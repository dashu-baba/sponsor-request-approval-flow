import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Download, FileText, Loader2, Upload } from 'lucide-react'
import { useRef, useState } from 'react'
import { toast } from 'sonner'

import { Button } from '@/components/ui/button'
import { apiFetch } from '@/lib/api/auth-api'
import { ApiError, parseProblemResponse } from '@/lib/api/api-error'
import { listAttachments, uploadAttachment } from '@/lib/api/requests-api'
import {
  ATTACHMENT_ACCEPT,
  ATTACHMENT_HINT,
  ATTACHMENT_MAX_SIZE_BYTES,
} from '@/features/requests/schemas'
import { formatDate, formatFileSize } from '@/lib/format'
import { queryKeys } from '@/lib/query-client'
import type { Attachment } from '@/lib/schemas/requests'
import { cn } from '@/lib/utils'

interface RequestAttachmentsSectionProps {
  requestId: number
  allowUpload?: boolean
}

async function downloadAttachment(
  requestId: number,
  attachment: Attachment,
): Promise<void> {
  const response = await apiFetch(`/requests/${requestId}/attachments/${attachment.id}`)

  if (!response.ok) {
    throw await parseProblemResponse(response)
  }

  const blob = await response.blob()
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = attachment.fileName
  link.click()
  URL.revokeObjectURL(url)
}

export function RequestAttachmentsSection({
  requestId,
  allowUpload = false,
}: RequestAttachmentsSectionProps) {
  const queryClient = useQueryClient()
  const inputRef = useRef<HTMLInputElement>(null)
  const [isDragging, setIsDragging] = useState(false)

  const query = useQuery({
    queryKey: queryKeys.requests.attachments(requestId),
    queryFn: () => listAttachments(requestId),
  })

  const uploadMutation = useMutation({
    mutationFn: (file: File) => uploadAttachment(requestId, file),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.requests.attachments(requestId) })
      toast.success('File uploaded.')
    },
    onError: (error) => {
      const message =
        error instanceof ApiError
          ? error.message
          : error instanceof Error
            ? error.message
            : 'Upload failed'
      toast.error(message)
    },
  })

  async function handleFiles(files: FileList | File[]) {
    const fileArray = [...files]
    for (const file of fileArray) {
      if (file.size > ATTACHMENT_MAX_SIZE_BYTES) {
        toast.error(`${file.name} exceeds the 10 MB limit.`)
        continue
      }
      try {
        await uploadMutation.mutateAsync(file)
      } catch {
        // onError handler surfaces the failure to the user
      }
    }
  }

  async function handleDownload(attachment: Attachment) {
    try {
      await downloadAttachment(requestId, attachment)
    } catch (error) {
      const message =
        error instanceof ApiError
          ? error.message
          : error instanceof Error
            ? error.message
            : 'Download failed'
      toast.error(message)
    }
  }

  if (query.isLoading) {
    return (
      <div className="flex items-center gap-2 text-[13px] text-text-secondary">
        <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
        Loading attachments…
      </div>
    )
  }

  if (query.isError) {
    const message =
      query.error instanceof ApiError
        ? query.error.message
        : query.error instanceof Error
          ? query.error.message
          : 'Unable to load attachments'

    return (
      <div className="space-y-2">
        <p className="text-[13px] text-danger">{message}</p>
        <Button type="button" variant="outline" size="sm" onClick={() => void query.refetch()}>
          Retry
        </Button>
      </div>
    )
  }

  const attachments = query.data ?? []

  return (
    <div className="space-y-4">
      {allowUpload ? (
        <div
          className={cn(
            'rounded-[14px] border border-dashed border-border bg-page px-4 py-6 text-center transition-colors',
            isDragging && 'border-brand bg-brand-light',
            uploadMutation.isPending && 'pointer-events-none opacity-70',
          )}
          onDragOver={(event) => {
            event.preventDefault()
            setIsDragging(true)
          }}
          onDragLeave={() => setIsDragging(false)}
          onDrop={(event) => {
            event.preventDefault()
            setIsDragging(false)
            if (event.dataTransfer.files.length > 0) {
              void handleFiles(event.dataTransfer.files)
            }
          }}
        >
          <Upload className="mx-auto h-5 w-5 text-text-hint" aria-hidden="true" />
          <p className="mt-2 text-[13px] font-medium text-text-primary">
            Drag and drop files here, or{' '}
            <button
              type="button"
              className="text-brand underline-offset-2 hover:underline"
              onClick={() => inputRef.current?.click()}
            >
              browse
            </button>
          </p>
          <p className="mt-1 text-[12px] text-text-hint">{ATTACHMENT_HINT}</p>
          <input
            ref={inputRef}
            type="file"
            className="sr-only"
            multiple
            accept={ATTACHMENT_ACCEPT}
            onChange={(event) => {
              if (event.target.files) {
                void handleFiles(event.target.files)
                event.target.value = ''
              }
            }}
          />
        </div>
      ) : null}

      {attachments.length === 0 ? (
        <p className="text-[13px] text-text-secondary">No supporting documents.</p>
      ) : (
        <ul className="divide-y divide-border rounded-[14px] border border-border bg-surface">
          {attachments.map((attachment) => (
            <li key={attachment.id} className="flex items-center justify-between gap-3 px-4 py-3">
              <div className="flex min-w-0 items-start gap-3">
                <span className="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-[8px] bg-brand-light text-brand">
                  <FileText className="h-4 w-4" aria-hidden="true" />
                </span>
                <div className="min-w-0">
                  <p className="truncate text-[13px] font-medium text-text-primary">
                    {attachment.fileName}
                  </p>
                  <p className="text-[12px] text-text-hint">
                    {formatFileSize(attachment.sizeBytes)} · {formatDate(attachment.createdAt)}
                  </p>
                </div>
              </div>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => void handleDownload(attachment)}
              >
                <Download className="h-3.5 w-3.5" aria-hidden="true" />
                Download
              </Button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
