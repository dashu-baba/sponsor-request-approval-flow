import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Loader2 } from 'lucide-react'
import { toast } from 'sonner'

import { Button } from '@/components/ui/button'
import { Modal } from '@/components/ui/modal'
import { ApiError } from '@/lib/api/api-error'
import { submitRequest } from '@/lib/api/requests-api'
import { queryKeys } from '@/lib/query-client'

interface SubmitRequestModalProps {
  open: boolean
  onClose: () => void
  requestId: number
  requestTitle: string
  onSuccess?: () => void
}

export function SubmitRequestModal({
  open,
  onClose,
  requestId,
  requestTitle,
  onSuccess,
}: SubmitRequestModalProps) {
  const queryClient = useQueryClient()

  const mutation = useMutation({
    mutationFn: () => submitRequest(requestId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.requests.all })
      toast.success('Request submitted for approval.')
      onClose()
      onSuccess?.()
    },
    onError: (error) => {
      const message =
        error instanceof ApiError
          ? error.message
          : error instanceof Error
            ? error.message
            : 'Unable to submit request'
      toast.error(message)
    },
  })

  function handleClose() {
    if (mutation.isPending) return
    onClose()
  }

  return (
    <Modal
      open={open}
      onClose={handleClose}
      title="Submit request for approval?"
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
            Keep as draft
          </Button>
          <Button
            type="button"
            variant="success"
            disabled={mutation.isPending}
            onClick={() => mutation.mutate()}
          >
            {mutation.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
            ) : null}
            Submit for approval
          </Button>
        </>
      }
    >
      <p className="text-[13px] leading-relaxed text-text-secondary">
        Once submitted, this request will be sent to your manager for approval. You can still cancel
        it while it is pending manager review.
      </p>
    </Modal>
  )
}
