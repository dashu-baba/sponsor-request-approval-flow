import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { Loader2 } from 'lucide-react'
import { useEffect, type FocusEvent } from 'react'
import { useForm, type UseFormReturn } from 'react-hook-form'

import { PageHeader } from '@/components/PageHeader'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  changePasswordSchema,
  profileUpdateSchema,
  type ChangePasswordValues,
  type ProfileUpdateInput,
  type ProfileUpdateValues,
} from '@/features/account/schemas'
import { useAuth, useCurrentUser } from '@/features/auth/use-auth'
import { changePassword, updateProfile } from '@/lib/api/account-api'
import { ApiError } from '@/lib/api/api-error'
import { getRoleLabel } from '@/lib/roles'

function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.detail ?? error.message
  }

  return error instanceof Error ? error.message : 'Something went wrong. Please try again.'
}

function registerPasswordField(
  form: UseFormReturn<ChangePasswordValues>,
  name: 'newPassword' | 'confirmPassword',
) {
  const registration = form.register(name)

  return {
    ...registration,
    onBlur: (event: FocusEvent<HTMLInputElement>) => {
      registration.onBlur(event)

      if (name === 'confirmPassword' || form.getValues('confirmPassword')) {
        void form.trigger('confirmPassword')
      }
    },
  }
}

export function ProfilePage() {
  const user = useCurrentUser()
  const { refreshProfile } = useAuth()

  const profileForm = useForm<ProfileUpdateValues, unknown, ProfileUpdateInput>({
    resolver: zodResolver(profileUpdateSchema),
    defaultValues: {
      displayName: '',
      department: '',
    },
  })

  const passwordForm = useForm<ChangePasswordValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: {
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    },
  })

  useEffect(() => {
    profileForm.reset({
      displayName: user.displayName,
      department: user.department ?? '',
    })
  }, [profileForm, user])

  const profileMutation = useMutation({
    mutationFn: updateProfile,
    onSuccess: async () => {
      await refreshProfile()
    },
  })

  const passwordMutation = useMutation({
    mutationFn: changePassword,
    onSuccess: async () => {
      passwordForm.reset()
      await refreshProfile()
    },
  })

  return (
    <div className="space-y-6">
      <PageHeader title="Profile" subtitle="Account settings and personal details." />

      <div className="grid max-w-3xl gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Profile details</CardTitle>
            <CardDescription>
              Update how your name and department appear across SponTrack.
            </CardDescription>
          </CardHeader>
          <CardContent>
            {profileMutation.isSuccess ? (
              <Alert variant="success" className="mb-4">
                <AlertTitle>Profile updated</AlertTitle>
                <AlertDescription>Your profile details were saved successfully.</AlertDescription>
              </Alert>
            ) : null}

            {profileMutation.isError ? (
              <Alert variant="destructive" className="mb-4">
                <AlertTitle>Unable to save profile</AlertTitle>
                <AlertDescription>{getErrorMessage(profileMutation.error)}</AlertDescription>
              </Alert>
            ) : null}

            <form
              className="space-y-4"
              onSubmit={(event) => {
                profileMutation.reset()
                void profileForm.handleSubmit((values) => profileMutation.mutate(values))(event)
              }}
              noValidate
            >
              <div className="space-y-2">
                <Label htmlFor="email">Email</Label>
                <Input id="email" value={user.email} readOnly disabled />
              </div>

              <div className="space-y-2">
                <Label htmlFor="role">Role</Label>
                <Input id="role" value={getRoleLabel(user.role)} readOnly disabled />
              </div>

              <div className="space-y-2">
                <Label htmlFor="displayName">Display name</Label>
                <Input
                  id="displayName"
                  autoComplete="name"
                  disabled={profileMutation.isPending}
                  {...profileForm.register('displayName')}
                />
                {profileForm.formState.errors.displayName ? (
                  <p className="text-sm text-danger">
                    {profileForm.formState.errors.displayName.message}
                  </p>
                ) : null}
              </div>

              <div className="space-y-2">
                <Label htmlFor="department">Department</Label>
                <Input
                  id="department"
                  autoComplete="organization"
                  disabled={profileMutation.isPending}
                  {...profileForm.register('department')}
                />
                {profileForm.formState.errors.department ? (
                  <p className="text-sm text-danger">
                    {profileForm.formState.errors.department.message}
                  </p>
                ) : null}
              </div>

              <Button type="submit" disabled={profileMutation.isPending}>
                {profileMutation.isPending ? (
                  <>
                    <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
                    Saving…
                  </>
                ) : (
                  'Save profile'
                )}
              </Button>
            </form>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Change password</CardTitle>
            <CardDescription>
              Changing your password signs out all other devices. This browser stays signed in.
            </CardDescription>
          </CardHeader>
          <CardContent>
            {passwordMutation.isSuccess ? (
              <Alert variant="success" className="mb-4">
                <AlertTitle>Password updated</AlertTitle>
                <AlertDescription>Your password was changed successfully.</AlertDescription>
              </Alert>
            ) : null}

            {passwordMutation.isError ? (
              <Alert variant="destructive" className="mb-4">
                <AlertTitle>Unable to change password</AlertTitle>
                <AlertDescription>{getErrorMessage(passwordMutation.error)}</AlertDescription>
              </Alert>
            ) : null}

            <form
              className="space-y-4"
              onSubmit={(event) => {
                passwordMutation.reset()
                void passwordForm.handleSubmit((values) =>
                  passwordMutation.mutate({
                    currentPassword: values.currentPassword,
                    newPassword: values.newPassword,
                  }),
                )(event)
              }}
              noValidate
            >
              <div className="space-y-2">
                <Label htmlFor="currentPassword">Current password</Label>
                <Input
                  id="currentPassword"
                  type="password"
                  autoComplete="current-password"
                  disabled={passwordMutation.isPending}
                  {...passwordForm.register('currentPassword')}
                />
                {passwordForm.formState.errors.currentPassword ? (
                  <p className="text-sm text-danger">
                    {passwordForm.formState.errors.currentPassword.message}
                  </p>
                ) : null}
              </div>

              <div className="space-y-2">
                <Label htmlFor="newPassword">New password</Label>
                <Input
                  id="newPassword"
                  type="password"
                  autoComplete="new-password"
                  disabled={passwordMutation.isPending}
                  {...registerPasswordField(passwordForm, 'newPassword')}
                />
                {passwordForm.formState.errors.newPassword ? (
                  <p className="text-sm text-danger">
                    {passwordForm.formState.errors.newPassword.message}
                  </p>
                ) : null}
              </div>

              <div className="space-y-2">
                <Label htmlFor="confirmPassword">Confirm new password</Label>
                <Input
                  id="confirmPassword"
                  type="password"
                  autoComplete="new-password"
                  disabled={passwordMutation.isPending}
                  {...registerPasswordField(passwordForm, 'confirmPassword')}
                />
                {passwordForm.formState.errors.confirmPassword ? (
                  <p className="text-sm text-danger">
                    {passwordForm.formState.errors.confirmPassword.message}
                  </p>
                ) : null}
              </div>

              <Button type="submit" disabled={passwordMutation.isPending}>
                {passwordMutation.isPending ? (
                  <>
                    <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
                    Updating…
                  </>
                ) : (
                  'Change password'
                )}
              </Button>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
