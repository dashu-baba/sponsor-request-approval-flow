import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { toast } from 'sonner'

import {
  Dialog,
  DialogCloseButton,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { ApiError } from '@/lib/api/api-error'
import { approveRequest, rejectRequest } from '@/lib/api/requests-api'
import { queryKeys } from '@/lib/query-client'

export type ApprovalAction = 'approve' | 'reject'

interface ApproveRejectModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  action: ApprovalAction
  requestId: number
  requestTitle: string
  onSuccess?: () => void
  onConflict409?: () => void
  onForbidden403?: () => void
}

export function ApproveRejectModal({
  open,
  onOpenChange,
  action,
  requestId,
  requestTitle,
  onSuccess,
  onConflict409,
  onForbidden403,
}: ApproveRejectModalProps) {
  const queryClient = useQueryClient()
  const [remarks, setRemarks] = useState('')
  const [validationError, setValidationError] = useState<string | null>(null)

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) {
      setRemarks('')
      setValidationError(null)
    }
    onOpenChange(nextOpen)
  }

  const mutation = useMutation({
    mutationFn: async () => {
      if (action === 'approve') {
        return approveRequest(requestId, remarks)
      }
      return rejectRequest(requestId, remarks)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.requests.all })
      toast.success(action === 'approve' ? 'Request approved successfully.' : 'Request rejected.')
      onOpenChange(false)
      onSuccess?.()
    },
    onError: (error) => {
      if (error instanceof ApiError) {
        if (error.status === 409) {
          handleOpenChange(false)
          if (onConflict409) {
            onConflict409()
          } else {
            toast.warning(
              error.message ||
                'This request was already updated. Refresh the list to see the latest state.',
            )
          }
          return
        }
        if (error.status === 403) {
          handleOpenChange(false)
          if (onForbidden403) {
            onForbidden403()
          } else {
            toast.error(error.message || 'You no longer have permission to action this request.')
          }
          return
        }
      }

      const message =
        error instanceof ApiError
          ? error.message
          : error instanceof Error
            ? error.message
            : 'Action failed'
      toast.error(message)
    },
  })

  function handleSubmit() {
    if (action === 'reject' && !remarks.trim()) {
      setValidationError('Remarks are required when rejecting a request.')
      return
    }

    setValidationError(null)
    mutation.mutate()
  }

  const isApprove = action === 'approve'

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogCloseButton />
        <DialogHeader>
          <DialogTitle>{isApprove ? 'Approve request' : 'Reject request'}</DialogTitle>
          <DialogDescription>
            {isApprove
              ? `Approve "${requestTitle}" — optional remarks will be recorded in the audit trail.`
              : `Reject "${requestTitle}" — remarks are required when rejecting a request.`}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-2 px-6 pb-2">
          <Label htmlFor="approval-remarks">
            Remarks {isApprove ? '(optional)' : <span className="text-danger">*</span>}
          </Label>
          <Textarea
            id="approval-remarks"
            value={remarks}
            onChange={(event) => {
              setRemarks(event.target.value)
              if (validationError) setValidationError(null)
            }}
            placeholder={
              isApprove
                ? 'Add any notes for the audit trail…'
                : 'Explain why this request is being rejected…'
            }
            aria-invalid={validationError ? true : undefined}
          />
          {validationError ? (
            <p className="text-[12px] text-danger" role="alert">
              {validationError}
            </p>
          ) : null}
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => handleOpenChange(false)}>
            Cancel
          </Button>
          <Button
            type="button"
            variant={isApprove ? 'success' : 'destructive'}
            disabled={mutation.isPending}
            onClick={handleSubmit}
          >
            {mutation.isPending ? 'Saving…' : isApprove ? 'Confirm approval' : 'Confirm rejection'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
