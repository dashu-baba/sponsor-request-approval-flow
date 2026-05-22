import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Check, Pencil, Trash2, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'

import { PageHeader } from '@/components/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/states/query-states'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  createSponsorshipType,
  deleteSponsorshipType,
  listSponsorshipTypes,
  updateSponsorshipType,
} from '@/features/admin/api/admin-api'
import { getErrorMessage } from '@/features/admin/format'
import { sponsorshipTypeMutationSchema } from '@/features/admin/schemas'
import type { SponsorshipType, SponsorshipTypeMutation } from '@/features/admin/types'

type FormValues = z.input<typeof sponsorshipTypeMutationSchema>
type SubmitValues = z.output<typeof sponsorshipTypeMutationSchema>

const emptyValues: FormValues = {
  name: '',
  description: '',
}

export function SponsorshipTypesPage() {
  const queryClient = useQueryClient()
  const [editingType, setEditingType] = useState<SponsorshipType | null>(null)
  const [mutationError, setMutationError] = useState<string | null>(null)

  const form = useForm<FormValues, unknown, SubmitValues>({
    resolver: zodResolver(sponsorshipTypeMutationSchema),
    defaultValues: emptyValues,
  })

  const query = useQuery({
    queryKey: ['sponsorship-types'],
    queryFn: listSponsorshipTypes,
  })

  const createMutation = useMutation({
    mutationFn: (values: SponsorshipTypeMutation) => createSponsorshipType(values),
    onSuccess: () => {
      setMutationError(null)
      form.reset(emptyValues)
      void queryClient.invalidateQueries({ queryKey: ['sponsorship-types'] })
    },
    onError: (error) => setMutationError(getErrorMessage(error)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, values }: { id: string; values: SponsorshipTypeMutation }) =>
      updateSponsorshipType(id, values),
    onSuccess: () => {
      setMutationError(null)
      setEditingType(null)
      form.reset(emptyValues)
      void queryClient.invalidateQueries({ queryKey: ['sponsorship-types'] })
    },
    onError: (error) => setMutationError(getErrorMessage(error)),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteSponsorshipType(id),
    onSuccess: () => {
      setMutationError(null)
      void queryClient.invalidateQueries({ queryKey: ['sponsorship-types'] })
    },
    onError: (error) => setMutationError(getErrorMessage(error)),
  })

  useEffect(() => {
    if (editingType) {
      form.reset({
        name: editingType.name,
        description: editingType.description ?? '',
      })
    }
  }, [editingType, form])

  if (query.isLoading) {
    return <LoadingState title="Loading sponsorship types" metricCount={0} tableRows={5} />
  }

  if (query.isError) {
    return (
      <ErrorState
        message={getErrorMessage(query.error)}
        onRetry={() => {
          void query.refetch()
        }}
      />
    )
  }

  const isMutating =
    createMutation.isPending || updateMutation.isPending || deleteMutation.isPending
  const submitLabel = editingType ? 'Save changes' : 'Create type'

  function submit(values: SubmitValues) {
    const parsed = values
    if (editingType) {
      updateMutation.mutate({ id: editingType.id, values: parsed })
      return
    }

    createMutation.mutate(parsed)
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Sponsorship types"
        subtitle="Manage sponsorship categories available to requestors."
      />

      {mutationError ? (
        <Alert variant="destructive">
          <AlertTitle>Action failed</AlertTitle>
          <AlertDescription>{mutationError}</AlertDescription>
        </Alert>
      ) : null}

      <div className="grid gap-6 xl:grid-cols-[360px_minmax(0,1fr)]">
        <Card>
          <CardHeader>
            <CardTitle>{editingType ? 'Edit type' : 'Create type'}</CardTitle>
          </CardHeader>
          <CardContent>
            <form className="space-y-4" onSubmit={form.handleSubmit(submit)}>
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

              <div className="flex gap-2">
                <Button type="submit" disabled={isMutating}>
                  <Check className="h-4 w-4" aria-hidden="true" />
                  {submitLabel}
                </Button>
                {editingType ? (
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => {
                      setEditingType(null)
                      form.reset(emptyValues)
                    }}
                  >
                    <X className="h-4 w-4" aria-hidden="true" />
                    Cancel
                  </Button>
                ) : null}
              </div>
            </form>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Existing types</CardTitle>
          </CardHeader>
          <CardContent>
            {query.data?.length === 0 ? (
              <EmptyState title="No sponsorship types" />
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full min-w-[620px] border-collapse text-left text-[13px]">
                  <thead>
                    <tr className="border-b border-border text-text-secondary">
                      <th className="py-3 pr-4 font-medium">Name</th>
                      <th className="py-3 pr-4 font-medium">Description</th>
                      <th className="py-3 pr-4 font-medium">Status</th>
                      <th className="py-3 pl-4 text-right font-medium">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {query.data?.map((type) => (
                      <tr key={type.id} className="border-b border-border last:border-0">
                        <td className="py-3 pr-4 font-medium text-text-primary">{type.name}</td>
                        <td className="py-3 pr-4 text-text-secondary">
                          {type.description ?? 'No description'}
                        </td>
                        <td className="py-3 pr-4">
                          <Badge>{type.isActive ? 'Active' : 'Inactive'}</Badge>
                        </td>
                        <td className="py-3 pl-4">
                          <div className="flex justify-end gap-2">
                            <Button
                              type="button"
                              variant="outline"
                              size="sm"
                              onClick={() => {
                                setMutationError(null)
                                setEditingType(type)
                              }}
                              aria-label={`Edit ${type.name}`}
                            >
                              <Pencil className="h-4 w-4" aria-hidden="true" />
                              Edit
                            </Button>
                            <Button
                              type="button"
                              variant="destructive"
                              size="sm"
                              onClick={() => deleteMutation.mutate(type.id)}
                              aria-label={`Delete ${type.name}`}
                              disabled={deleteMutation.isPending}
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
      </div>
    </div>
  )
}
