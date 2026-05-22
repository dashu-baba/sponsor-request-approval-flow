import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Loader2 } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'

import { Button } from '@/components/ui/button'
import { Modal } from '@/components/ui/modal'
import { Textarea } from '@/components/ui/textarea'
import { ApiError } from '@/lib/api/api-error'
import { cancelRequest } from '@/lib/api/requests-api'
import { queryKeys } from '@/lib/query-client'

interface CancelRequestModalProps {
  open: boolean
  onClose: () => void
  requestId: string
  requestTitle: string
  onSuccess?: () => void
}

export function CancelRequestModal({
  open,
  onClose,
  requestId,
  requestTitle,
  onSuccess,
}: CancelRequestModalProps) {
  const queryClient = useQueryClient()
  const [remarks, setRemarks] = useState('')

  const mutation = useMutation({
    mutationFn: () => cancelRequest(requestId, remarks),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.requests.all })
      toast.success('Request cancelled.')
      setRemarks('')
      onClose()
      onSuccess?.()
    },
    onError: (error) => {
      const message =
        error instanceof ApiError
          ? error.message
          : error instanceof Error
            ? error.message
            : 'Unable to cancel request'
      toast.error(message)
    },
  })

  function handleClose() {
    if (mutation.isPending) return
    setRemarks('')
    onClose()
  }

  return (
    <Modal
      open={open}
      onClose={handleClose}
      title="Cancel request?"
      subtitle={requestTitle}
      maxWidthClassName="max-w-md"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={handleClose}
            disabled={mutation.isPending}
          >
            Keep request
          </Button>
          <Button
            type="button"
            variant="destructive"
            disabled={mutation.isPending}
            onClick={() => mutation.mutate()}
          >
            {mutation.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
            ) : null}
            Cancel request
          </Button>
        </>
      }
    >
      <p className="text-[13px] leading-relaxed text-text-secondary">
        Once cancelled, this request will no longer proceed through approval. You can add an
        optional note below.
      </p>
      <div className="mt-4">
        <Textarea
          value={remarks}
          onChange={(event) => setRemarks(event.target.value)}
          placeholder="Optional remarks…"
          rows={3}
          aria-label="Cancellation remarks"
        />
      </div>
    </Modal>
  )
}
