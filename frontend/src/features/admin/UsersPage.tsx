import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Check, Plus } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'

import { PageHeader } from '@/components/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/states/query-states'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Modal } from '@/components/ui/modal'
import { getErrorMessage } from '@/features/admin/format'
import { createUserSchema } from '@/features/admin/schemas'
import type { CreateUserInput, CreateUserValues } from '@/features/admin/types'
import { ApiError } from '@/lib/api/api-error'
import { createUser, listUsers } from '@/lib/api/users-api'
import { queryKeys } from '@/lib/query-client'
import { getRoleLabel, Roles } from '@/lib/roles'

const emptyValues: CreateUserInput = {
  email: '',
  displayName: '',
  department: '',
  role: Roles.Requestor,
  initialPassword: '',
}

const roleOptions = Object.values(Roles)

export function UsersPage() {
  const queryClient = useQueryClient()
  const [formModalOpen, setFormModalOpen] = useState(false)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const form = useForm<CreateUserInput, unknown, CreateUserValues>({
    resolver: zodResolver(createUserSchema),
    defaultValues: emptyValues,
  })

  const query = useQuery({
    queryKey: queryKeys.users.list,
    queryFn: listUsers,
  })

  const createMutation = useMutation({
    mutationFn: createUser,
    onSuccess: (user) => {
      setSuccessMessage(`${user.displayName} was created.`)
      closeFormModal()
      void queryClient.invalidateQueries({ queryKey: queryKeys.users.list })
    },
    onError: (error) => {
      form.clearErrors()

      if (!(error instanceof ApiError)) {
        return
      }

      if (error.status === 409) {
        form.setError('email', {
          message: error.detail ?? 'A user with this email already exists.',
        })
        return
      }

      if (error.status === 400) {
        form.setError('initialPassword', {
          message: error.detail ?? 'Password does not meet the required policy.',
        })
      }
    },
  })

  function openCreateModal() {
    createMutation.reset()
    form.reset(emptyValues)
    setFormModalOpen(true)
  }

  function closeFormModal() {
    setFormModalOpen(false)
    createMutation.reset()
    form.reset(emptyValues)
  }

  function submit(values: CreateUserValues) {
    createMutation.mutate(values)
  }

  if (query.isLoading) {
    return <LoadingState title="Loading users" metricCount={0} tableRows={5} />
  }

  if (query.isError) {
    return (
      <ErrorState message={getErrorMessage(query.error)} onRetry={() => void query.refetch()} />
    )
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Users"
        subtitle="Onboard colleagues with a role and initial password."
        actions={
          <Button type="button" onClick={openCreateModal}>
            <Plus className="h-4 w-4" aria-hidden="true" />
            Add user
          </Button>
        }
      />

      {createMutation.isError && !(createMutation.error instanceof ApiError) ? (
        <Alert variant="destructive">
          <AlertTitle>Action failed</AlertTitle>
          <AlertDescription>{getErrorMessage(createMutation.error)}</AlertDescription>
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
              <EmptyState title="No users" description="Create the first user to get started." />
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[720px] border-collapse text-left text-[13px]">
                <thead>
                  <tr className="border-b border-border bg-page text-text-secondary">
                    <th className="px-5 py-3 font-medium">Email</th>
                    <th className="px-5 py-3 font-medium">Display name</th>
                    <th className="px-5 py-3 font-medium">Department</th>
                    <th className="px-5 py-3 font-medium">Role</th>
                  </tr>
                </thead>
                <tbody>
                  {query.data?.map((user) => (
                    <tr key={user.id} className="border-b border-border last:border-0">
                      <td className="px-5 py-4 font-medium text-text-primary">{user.email}</td>
                      <td className="px-5 py-4">{user.displayName}</td>
                      <td className="px-5 py-4 text-text-secondary">{user.department ?? '—'}</td>
                      <td className="px-5 py-4">
                        <Badge>{getRoleLabel(user.role)}</Badge>
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
        title="Create user"
        subtitle="Assign exactly one role and an initial password."
        footer={
          <>
            <Button
              type="button"
              variant="outline"
              onClick={closeFormModal}
              disabled={createMutation.isPending}
            >
              Cancel
            </Button>
            <Button type="submit" form="create-user-form" disabled={createMutation.isPending}>
              <Check className="h-4 w-4" aria-hidden="true" />
              Create user
            </Button>
          </>
        }
      >
        <form id="create-user-form" className="space-y-4" onSubmit={form.handleSubmit(submit)}>
          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              autoComplete="off"
              disabled={createMutation.isPending}
              {...form.register('email')}
            />
            {form.formState.errors.email ? (
              <p className="text-sm text-danger">{form.formState.errors.email.message}</p>
            ) : null}
          </div>

          <div className="space-y-2">
            <Label htmlFor="displayName">Display name</Label>
            <Input
              id="displayName"
              autoComplete="off"
              disabled={createMutation.isPending}
              {...form.register('displayName')}
            />
            {form.formState.errors.displayName ? (
              <p className="text-sm text-danger">{form.formState.errors.displayName.message}</p>
            ) : null}
          </div>

          <div className="space-y-2">
            <Label htmlFor="department">Department</Label>
            <Input
              id="department"
              autoComplete="off"
              disabled={createMutation.isPending}
              {...form.register('department')}
            />
            {form.formState.errors.department ? (
              <p className="text-sm text-danger">{form.formState.errors.department.message}</p>
            ) : null}
          </div>

          <div className="space-y-2">
            <Label htmlFor="role">Role</Label>
            <select
              id="role"
              className="flex h-10 w-full rounded-md border border-border bg-surface px-3 py-2 text-sm text-text-primary"
              disabled={createMutation.isPending}
              {...form.register('role')}
            >
              {roleOptions.map((role) => (
                <option key={role} value={role}>
                  {getRoleLabel(role)}
                </option>
              ))}
            </select>
            {form.formState.errors.role ? (
              <p className="text-sm text-danger">{form.formState.errors.role.message}</p>
            ) : null}
          </div>

          <div className="space-y-2">
            <Label htmlFor="initialPassword">Initial password</Label>
            <Input
              id="initialPassword"
              type="password"
              autoComplete="new-password"
              disabled={createMutation.isPending}
              {...form.register('initialPassword')}
            />
            {form.formState.errors.initialPassword ? (
              <p className="text-sm text-danger">{form.formState.errors.initialPassword.message}</p>
            ) : null}
          </div>
        </form>
      </Modal>
    </div>
  )
}
