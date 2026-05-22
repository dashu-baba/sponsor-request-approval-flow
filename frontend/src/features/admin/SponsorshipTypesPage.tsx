import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Check, Pencil, Plus, Trash2 } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'

import { PageHeader } from '@/components/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/states/query-states'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Modal } from '@/components/ui/modal'
import {
  createSponsorshipType,
  deleteSponsorshipType,
  listAdminRequests,
  listSponsorshipTypes,
  updateSponsorshipType,
} from '@/features/admin/api/admin-api'
import { getErrorMessage } from '@/features/admin/format'
import { sponsorshipTypeMutationSchema } from '@/features/admin/schemas'
import type { SponsorshipType, SponsorshipTypeMutation } from '@/features/admin/types'
import { queryKeys } from '@/lib/query-client'

type FormValues = z.input<typeof sponsorshipTypeMutationSchema>
type SubmitValues = z.output<typeof sponsorshipTypeMutationSchema>

const emptyValues: FormValues = {
  name: '',
  description: '',
}

export function SponsorshipTypesPage() {
  const queryClient = useQueryClient()
  const [editingType, setEditingType] = useState<SponsorshipType | null>(null)
  const [deletingType, setDeletingType] = useState<SponsorshipType | null>(null)
  const [formModalOpen, setFormModalOpen] = useState(false)
  const [mutationError, setMutationError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const form = useForm<FormValues, unknown, SubmitValues>({
    resolver: zodResolver(sponsorshipTypeMutationSchema),
    defaultValues: emptyValues,
  })

  const query = useQuery({
    queryKey: queryKeys.sponsorshipTypes.list,
    queryFn: listSponsorshipTypes,
  })

  const requestsQuery = useQuery({
    queryKey: ['admin-requests-for-type-counts'],
    queryFn: () => listAdminRequests({ page: 1, pageSize: 100 }),
  })

  const activeRequestCounts = useMemo(() => {
    const counts = new Map<string, number>()
    for (const request of requestsQuery.data?.items ?? []) {
      counts.set(request.sponsorshipTypeName, (counts.get(request.sponsorshipTypeName) ?? 0) + 1)
    }
    return counts
  }, [requestsQuery.data?.items])

  const createMutation = useMutation({
    mutationFn: (values: SponsorshipTypeMutation) => createSponsorshipType(values),
    onSuccess: () => {
      setMutationError(null)
      setSuccessMessage('Sponsorship type created.')
      closeFormModal()
      void queryClient.invalidateQueries({ queryKey: queryKeys.sponsorshipTypes.list })
    },
    onError: (error) => setMutationError(getErrorMessage(error)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, values }: { id: number; values: SponsorshipTypeMutation }) =>
      updateSponsorshipType(id, values),
    onSuccess: () => {
      setMutationError(null)
      setSuccessMessage('Sponsorship type updated.')
      closeFormModal()
      void queryClient.invalidateQueries({ queryKey: queryKeys.sponsorshipTypes.list })
    },
    onError: (error) => setMutationError(getErrorMessage(error)),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteSponsorshipType(id),
    onSuccess: (_data, id) => {
      setMutationError(null)
      const deletedName = deletingType?.name ?? 'Sponsorship type'
      const requestCount = deletingType ? (activeRequestCounts.get(deletingType.name) ?? 0) : 0
      setSuccessMessage(
        requestCount > 0
          ? `${deletedName} was deactivated because it is referenced by submitted requests.`
          : `${deletedName} was deleted.`,
      )
      setDeletingType(null)
      void queryClient.invalidateQueries({ queryKey: queryKeys.sponsorshipTypes.list })
      void queryClient.invalidateQueries({ queryKey: ['admin-requests-for-type-counts'] })
      if (editingType?.id === id) {
        closeFormModal()
      }
    },
    onError: (error) => setMutationError(getErrorMessage(error)),
  })

  useEffect(() => {
    if (editingType && formModalOpen) {
      form.reset({
        name: editingType.name,
        description: editingType.description ?? '',
      })
    }
  }, [editingType, form, formModalOpen])

  function openCreateModal() {
    setMutationError(null)
    setEditingType(null)
    form.reset(emptyValues)
    setFormModalOpen(true)
  }

  function openEditModal(type: SponsorshipType) {
    setMutationError(null)
    setEditingType(type)
    setFormModalOpen(true)
  }

  function closeFormModal() {
    setFormModalOpen(false)
    setEditingType(null)
    form.reset(emptyValues)
  }

  function submit(values: SubmitValues) {
    if (editingType) {
      updateMutation.mutate({ id: editingType.id, values })
      return
    }

    createMutation.mutate(values)
  }

  if (query.isLoading || requestsQuery.isLoading) {
    return <LoadingState title="Loading sponsorship types" metricCount={0} tableRows={5} />
  }

  if (query.isError || requestsQuery.isError) {
    return (
      <ErrorState
        message={getErrorMessage(query.error ?? requestsQuery.error)}
        onRetry={() => {
          void query.refetch()
          void requestsQuery.refetch()
        }}
      />
    )
  }

  const isMutating =
    createMutation.isPending || updateMutation.isPending || deleteMutation.isPending
  const deletingRequestCount = deletingType ? (activeRequestCounts.get(deletingType.name) ?? 0) : 0

  return (
    <div className="space-y-6">
      <PageHeader
        title="Sponsorship types"
        subtitle="Manage lookup values used when creating requests."
        actions={
          <Button type="button" onClick={openCreateModal}>
            <Plus className="h-4 w-4" aria-hidden="true" />
            Add type
          </Button>
        }
      />

      {mutationError ? (
        <Alert variant="destructive">
          <AlertTitle>Action failed</AlertTitle>
          <AlertDescription>{mutationError}</AlertDescription>
        </Alert>
      ) : null}

      {successMessage ? (
        <Alert variant="info">
          <AlertTitle>Success</AlertTitle>
          <AlertDescription>{successMessage}</AlertDescription>
        </Alert>
      ) : null}

      <Card>
        <CardContent className="p-0">
          {query.data?.length === 0 ? (
            <div className="p-8">
              <EmptyState title="No sponsorship types" />
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[720px] border-collapse text-left text-[13px]">
                <thead>
                  <tr className="border-b border-border bg-page text-text-secondary">
                    <th className="px-5 py-3 font-medium">Name</th>
                    <th className="px-5 py-3 font-medium">Description</th>
                    <th className="px-5 py-3 font-medium">Active requests</th>
                    <th className="px-5 py-3 font-medium">Status</th>
                    <th className="px-5 py-3 text-right font-medium">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {query.data?.map((type) => (
                    <tr key={type.id} className="border-b border-border last:border-0">
                      <td className="px-5 py-4 font-medium text-text-primary">{type.name}</td>
                      <td className="px-5 py-4 text-text-secondary">
                        {type.description ?? 'No description'}
                      </td>
                      <td className="px-5 py-4">{activeRequestCounts.get(type.name) ?? 0}</td>
                      <td className="px-5 py-4">
                        <Badge>{type.isActive ? 'Active' : 'Inactive'}</Badge>
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex justify-end gap-2">
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            onClick={() => openEditModal(type)}
                            aria-label={`Edit ${type.name}`}
                          >
                            <Pencil className="h-4 w-4" aria-hidden="true" />
                            Edit
                          </Button>
                          <Button
                            type="button"
                            variant="destructive"
                            size="sm"
                            onClick={() => {
                              setMutationError(null)
                              setDeletingType(type)
                            }}
                            aria-label={`Delete ${type.name}`}
                            disabled={!type.isActive || deleteMutation.isPending}
                          >
                            <Trash2 className="h-4 w-4" aria-hidden="true" />
                            Delete
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <Modal
        open={formModalOpen}
        onClose={closeFormModal}
        title={editingType ? 'Edit sponsorship type' : 'Add sponsorship type'}
        subtitle="Used in the request create/edit form"
        footer={
          <>
            <Button type="button" variant="outline" onClick={closeFormModal} disabled={isMutating}>
              Cancel
            </Button>
            <Button type="submit" form="sponsorship-type-form" disabled={isMutating}>
              <Check className="h-4 w-4" aria-hidden="true" />
              {editingType ? 'Save type' : 'Create type'}
            </Button>
          </>
        }
      >
        <form id="sponsorship-type-form" className="space-y-4" onSubmit={form.handleSubmit(submit)}>
          <div className="space-y-1.5">
            <Label htmlFor="sponsorship-type-name">Name</Label>
            <Input id="sponsorship-type-name" {...form.register('name')} />
            {form.formState.errors.name ? (
              <p className="text-xs text-danger">{form.formState.errors.name.message}</p>
            ) : null}
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="sponsorship-type-description">Description</Label>
            <Input id="sponsorship-type-description" {...form.register('description')} />
            {form.formState.errors.description ? (
              <p className="text-xs text-danger">{form.formState.errors.description.message}</p>
            ) : null}
          </div>
        </form>
      </Modal>

      <Modal
        open={Boolean(deletingType)}
        onClose={() => setDeletingType(null)}
        title="Delete sponsorship type?"
        subtitle={
          deletingType
            ? deletingRequestCount > 0
              ? `${deletingType.name} is referenced by ${deletingRequestCount} submitted request${deletingRequestCount === 1 ? '' : 's'}. It will be deactivated instead of removed.`
              : `${deletingType.name} will be removed from the lookup list.`
            : undefined
        }
        maxWidthClassName="max-w-md"
        footer={
          <>
            <Button
              type="button"
              variant="outline"
              onClick={() => setDeletingType(null)}
              disabled={deleteMutation.isPending}
            >
              Cancel
            </Button>
            <Button
              type="button"
              variant="destructive"
              disabled={deleteMutation.isPending || !deletingType}
              onClick={() => {
                if (deletingType) {
                  deleteMutation.mutate(deletingType.id)
                }
              }}
            >
              {deletingRequestCount > 0 ? 'Deactivate type' : 'Delete type'}
            </Button>
          </>
        }
      >
        {deletingRequestCount > 0 ? (
          <p className="text-[13px] leading-6 text-text-secondary">
            Existing requests keep their historical sponsorship type. The type will no longer be
            available for new requests.
          </p>
        ) : null}
      </Modal>
    </div>
  )
}
