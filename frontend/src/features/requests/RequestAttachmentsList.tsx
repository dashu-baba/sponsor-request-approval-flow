import { useQuery } from '@tanstack/react-query'
import { Download, FileText, Loader2 } from 'lucide-react'
import { toast } from 'sonner'

import { Button } from '@/components/ui/button'
import { apiFetch } from '@/lib/api/auth-api'
import { ApiError, parseProblemResponse } from '@/lib/api/api-error'
import { listAttachments } from '@/lib/api/requests-api'
import { formatDate, formatFileSize } from '@/lib/format'
import { queryKeys } from '@/lib/query-client'
import type { Attachment } from '@/lib/schemas/requests'

interface RequestAttachmentsListProps {
  requestId: number
}

async function downloadAttachment(requestId: number, attachment: Attachment): Promise<void> {
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

export function RequestAttachmentsList({ requestId }: RequestAttachmentsListProps) {
  const query = useQuery({
    queryKey: queryKeys.requests.attachments(requestId),
    queryFn: () => listAttachments(requestId),
  })

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

  if (attachments.length === 0) {
    return <p className="text-[13px] text-text-secondary">No supporting documents.</p>
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

  return (
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
  )
}
