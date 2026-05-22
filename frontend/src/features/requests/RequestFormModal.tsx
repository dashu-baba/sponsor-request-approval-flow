import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Loader2 } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { toast } from 'sonner'

import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Modal } from '@/components/ui/modal'
import { Textarea } from '@/components/ui/textarea'
import { useCurrentUser } from '@/features/auth/use-auth'
import {
  mapRequestMutationFieldErrors,
  requestMutationSchema,
  type RequestMutationFormValues,
  type RequestMutationValues,
} from '@/features/requests/schemas'
import { ApiError } from '@/lib/api/api-error'
import {
  createRequest,
  getRequest,
  submitRequest,
  updateDraftRequest,
  type RequestMutationPayload,
} from '@/lib/api/requests-api'
import { listSponsorshipTypes, type SponsorshipType } from '@/lib/api/sponsorship-types-api'
import { queryKeys } from '@/lib/query-client'
import type { RequestDetail } from '@/lib/schemas/requests'

function toDateInputValue(isoDate: string): string {
  return isoDate.slice(0, 10)
}

function toPayload(values: RequestMutationValues): RequestMutationPayload {
  return {
    title: values.title,
    department: values.department,
    sponsorshipTypeId: values.sponsorshipTypeId,
    eventName: values.eventName,
    eventDate: values.eventDate,
    requestedAmount: values.requestedAmount,
    purpose: values.purpose,
    expectedBenefit: values.expectedBenefit,
    remarks: values.remarks ?? null,
  }
}

function detailToFormValues(request: RequestDetail): RequestMutationFormValues {
  return {
    title: request.title,
    department: request.department,
    sponsorshipTypeId: request.sponsorshipTypeId,
    eventName: request.eventName,
    eventDate: toDateInputValue(request.eventDate),
    requestedAmount: request.requestedAmount,
    purpose: request.purpose,
    expectedBenefit: request.expectedBenefit ?? '',
    remarks: request.remarks ?? '',
  }
}

function emptyFormValues(department: string): RequestMutationFormValues {
  const now = new Date()
  const eventDate = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`

  return {
    title: '',
    department,
    sponsorshipTypeId: '',
    eventName: '',
    eventDate,
    requestedAmount: 0,
    purpose: '',
    expectedBenefit: '',
    remarks: '',
  }
}

interface RequestFormModalProps {
  open: boolean
  onClose: () => void
  requestId?: string
  onSuccess?: () => void
}

interface RequestFormBodyProps {
  defaultValues: RequestMutationFormValues
  requestId?: string
  isEdit: boolean
  types: SponsorshipType[]
  displayName: string
  onClose: () => void
  onSuccess?: () => void
}

function RequestFormBody({
  defaultValues,
  requestId,
  isEdit,
  types,
  displayName,
  onClose,
  onSuccess,
}: RequestFormBodyProps) {
  const queryClient = useQueryClient()
  const [formError, setFormError] = useState<string | null>(null)

  const form = useForm<RequestMutationFormValues, unknown, RequestMutationValues>({
    resolver: zodResolver(requestMutationSchema),
    defaultValues,
  })

  const saveMutation = useMutation({
    mutationFn: async (values: RequestMutationValues) => {
      const payload = toPayload(values)
      if (isEdit && requestId) {
        return updateDraftRequest(requestId, payload)
      }
      return createRequest(payload)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.requests.all })
      toast.success(isEdit ? 'Draft updated.' : 'Draft saved.')
      onClose()
      onSuccess?.()
    },
    onError: (error) => handleMutationError(error),
  })

  const submitMutation = useMutation({
    mutationFn: async (values: RequestMutationValues) => {
      const payload = toPayload(values)
      const saved =
        isEdit && requestId
          ? await updateDraftRequest(requestId, payload)
          : await createRequest(payload)
      return submitRequest(saved.id)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.requests.all })
      toast.success('Request submitted for approval.')
      onClose()
      onSuccess?.()
    },
    onError: (error) => handleMutationError(error),
  })

  function handleMutationError(error: unknown) {
    if (error instanceof ApiError && error.fieldErrors) {
      const mapped = mapRequestMutationFieldErrors(error.fieldErrors)
      for (const [field, message] of Object.entries(mapped) as [
        keyof RequestMutationFormValues,
        string,
      ][]) {
        form.setError(field, { message })
      }
      if (Object.keys(mapped).length === 0) {
        setFormError(error.message)
      }
      return
    }

    const message =
      error instanceof ApiError
        ? error.message
        : error instanceof Error
          ? error.message
          : 'Unable to save request'
    setFormError(message)
  }

  const isBusy = saveMutation.isPending || submitMutation.isPending

  return (
    <>
      {formError ? (
        <Alert variant="destructive" className="mb-4">
          <AlertDescription>{formError}</AlertDescription>
        </Alert>
      ) : null}

      <form className="grid gap-3.5 sm:grid-cols-2" onSubmit={(event) => event.preventDefault()}>
        <div className="space-y-1.5 sm:col-span-2">
          <Label htmlFor="request-title">Request title</Label>
          <Input id="request-title" {...form.register('title')} />
          {form.formState.errors.title ? (
            <p className="text-xs text-danger">{form.formState.errors.title.message}</p>
          ) : null}
        </div>

        <div className="space-y-1.5 sm:col-span-2">
          <Label htmlFor="request-requestor">Requestor</Label>
          <Input
            id="request-requestor"
            value={displayName}
            readOnly
            aria-readonly="true"
            className="cursor-default bg-page text-text-secondary"
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="request-type">Sponsorship type</Label>
          <select
            id="request-type"
            className="flex h-9 w-full rounded-[8px] border border-border bg-surface px-2.5 text-[13px] text-text-primary"
            {...form.register('sponsorshipTypeId')}
          >
            <option value="">— select —</option>
            {types
              .filter((type) => type.isActive)
              .map((type) => (
                <option key={type.id} value={type.id}>
                  {type.name}
                </option>
              ))}
          </select>
          {form.formState.errors.sponsorshipTypeId ? (
            <p className="text-xs text-danger">{form.formState.errors.sponsorshipTypeId.message}</p>
          ) : null}
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="request-department">Department</Label>
          <Input id="request-department" {...form.register('department')} />
          {form.formState.errors.department ? (
            <p className="text-xs text-danger">{form.formState.errors.department.message}</p>
          ) : null}
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="request-event">Event / organisation</Label>
          <Input id="request-event" {...form.register('eventName')} />
          {form.formState.errors.eventName ? (
            <p className="text-xs text-danger">{form.formState.errors.eventName.message}</p>
          ) : null}
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="request-date">Event date</Label>
          <Input id="request-date" type="date" {...form.register('eventDate')} />
          {form.formState.errors.eventDate ? (
            <p className="text-xs text-danger">{form.formState.errors.eventDate.message}</p>
          ) : null}
        </div>

        <div className="space-y-1.5 sm:col-span-2">
          <Label htmlFor="request-amount">Requested amount (RM)</Label>
          <Input
            id="request-amount"
            type="number"
            min={0}
            step="0.01"
            {...form.register('requestedAmount')}
          />
          {form.formState.errors.requestedAmount ? (
            <p className="text-xs text-danger">{form.formState.errors.requestedAmount.message}</p>
          ) : null}
        </div>

        <div className="space-y-1.5 sm:col-span-2">
          <Label htmlFor="request-purpose">Purpose / justification</Label>
          <Textarea id="request-purpose" rows={3} {...form.register('purpose')} />
          {form.formState.errors.purpose ? (
            <p className="text-xs text-danger">{form.formState.errors.purpose.message}</p>
          ) : null}
        </div>

        <div className="space-y-1.5 sm:col-span-2">
          <Label htmlFor="request-benefit">Expected benefit (optional)</Label>
          <Textarea id="request-benefit" rows={2} {...form.register('expectedBenefit')} />
          {form.formState.errors.expectedBenefit ? (
            <p className="text-xs text-danger">{form.formState.errors.expectedBenefit.message}</p>
          ) : null}
        </div>
      </form>

      <div className="mt-6 flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onClose} disabled={isBusy}>
          Cancel
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={isBusy}
          onClick={() => {
            setFormError(null)
            void form.handleSubmit((values) => saveMutation.mutate(values))()
          }}
        >
          {saveMutation.isPending ? (
            <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
          ) : null}
          Save draft
        </Button>
        <Button
          type="button"
          disabled={isBusy}
          onClick={() => {
            setFormError(null)
            void form.handleSubmit((values) => submitMutation.mutate(values))()
          }}
        >
          {submitMutation.isPending ? (
            <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
          ) : null}
          Submit request
        </Button>
      </div>
    </>
  )
}

export function RequestFormModal({ open, onClose, requestId, onSuccess }: RequestFormModalProps) {
  const user = useCurrentUser()
  const isEdit = Boolean(requestId)

  const typesQuery = useQuery({
    queryKey: queryKeys.sponsorshipTypes.list,
    queryFn: listSponsorshipTypes,
    enabled: open,
  })

  const detailQuery = useQuery({
    queryKey: queryKeys.requests.detail(requestId ?? ''),
    queryFn: () => {
      if (!requestId) throw new Error('Request id is required')
      return getRequest(requestId)
    },
    enabled: open && isEdit,
  })

  const isLoading = isEdit && detailQuery.isLoading
  const loadedRequest = detailQuery.data
  const isDraftEditable = !isEdit || loadedRequest?.status === 'Draft'
  const defaultValues =
    isEdit && loadedRequest
      ? detailToFormValues(loadedRequest)
      : emptyFormValues(user.department ?? '')
  const bodyKey = isEdit ? `edit-${requestId}` : 'create'

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={isEdit ? 'Edit draft request' : 'New sponsorship request'}
      subtitle={
        isEdit
          ? 'Update your draft before submitting for approval.'
          : 'Fill in the details to create a new request.'
      }
      maxWidthClassName="max-w-2xl"
      footer={null}
    >
      {isLoading ? (
        <p className="text-[13px] text-text-secondary">Loading request…</p>
      ) : typesQuery.isError ? (
        <Alert variant="destructive">
          <AlertDescription>
            {typesQuery.error instanceof ApiError
              ? typesQuery.error.message
              : 'Unable to load sponsorship types.'}
          </AlertDescription>
        </Alert>
      ) : isEdit && loadedRequest && !isDraftEditable ? (
        <div className="space-y-4">
          <Alert variant="warning">
            <AlertDescription>
              This request is no longer a draft and cannot be edited. View the request detail for
              read-only information.
            </AlertDescription>
          </Alert>
          <div className="flex justify-end">
            <Button type="button" variant="outline" onClick={onClose}>
              Close
            </Button>
          </div>
        </div>
      ) : open && (!isEdit || loadedRequest) ? (
        <RequestFormBody
          key={bodyKey}
          defaultValues={defaultValues}
          requestId={requestId}
          isEdit={isEdit}
          types={typesQuery.data ?? []}
          displayName={user.displayName}
          onClose={onClose}
          onSuccess={onSuccess}
        />
      ) : null}
    </Modal>
  )
}
