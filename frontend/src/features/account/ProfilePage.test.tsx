import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi, beforeEach } from 'vitest'

import { ProfilePage } from '@/features/account/ProfilePage'
import { Roles } from '@/lib/roles'
import type { UserProfile } from '@/lib/schemas/auth'

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <ProfilePage />
    </QueryClientProvider>,
  )
}

const mocks = vi.hoisted(() => ({
  refreshProfile: vi.fn(),
  updateProfile: vi.fn(),
  changePassword: vi.fn(),
  user: null as UserProfile | null,
}))

vi.mock('@/features/auth/use-auth', () => ({
  useAuth: () => ({
    refreshProfile: mocks.refreshProfile,
  }),
  useCurrentUser: () => mocks.user,
}))

vi.mock('@/lib/api/account-api', () => ({
  updateProfile: (...args: unknown[]) => mocks.updateProfile(...args),
  changePassword: (...args: unknown[]) => mocks.changePassword(...args),
}))

const baseUser: UserProfile = {
  id: 'user-1',
  email: 'requestor@test.local',
  displayName: 'Original Name',
  department: 'Marketing',
  role: Roles.Requestor,
}

describe('ProfilePage', () => {
  beforeEach(() => {
    mocks.user = baseUser
    mocks.refreshProfile.mockResolvedValue({
      ...baseUser,
      displayName: 'Updated Name',
    })
    mocks.updateProfile.mockResolvedValue({
      ...baseUser,
      displayName: 'Updated Name',
    })
    mocks.changePassword.mockResolvedValue({
      accessToken: 'new-token',
      accessTokenExpiresAt: new Date().toISOString(),
      tokenType: 'Bearer',
    })
  })

  it('shows validation errors when profile display name is cleared', async () => {
    const user = userEvent.setup()
    renderPage()

    const displayNameInput = screen.getByLabelText(/display name/i)
    await user.clear(displayNameInput)
    await user.click(screen.getByRole('button', { name: /save profile/i }))

    expect(await screen.findByText(/display name is required/i)).toBeInTheDocument()
    expect(mocks.updateProfile).not.toHaveBeenCalled()
  })

  it('submits profile updates and refreshes auth context', async () => {
    const user = userEvent.setup()
    renderPage()

    const displayNameInput = screen.getByLabelText(/display name/i)
    await user.clear(displayNameInput)
    await user.type(displayNameInput, 'Updated Name')
    await user.click(screen.getByRole('button', { name: /save profile/i }))

    await waitFor(() => {
      expect(mocks.updateProfile).toHaveBeenCalledWith(
        expect.objectContaining({
          displayName: 'Updated Name',
          department: 'Marketing',
        }),
        expect.anything(),
      )
    })

    expect(mocks.refreshProfile).toHaveBeenCalled()
    expect(await screen.findByText(/profile updated/i)).toBeInTheDocument()
  })

  it('requires matching confirmation before changing password', async () => {
    const user = userEvent.setup()
    renderPage()

    await user.type(screen.getByLabelText(/current password/i), 'Password1!')
    await user.type(screen.getByLabelText(/^new password$/i), 'Password2!')
    await user.type(screen.getByLabelText(/confirm new password/i), 'Mismatch2!')
    await user.click(screen.getByRole('button', { name: /change password/i }))

    expect(await screen.findByText(/passwords do not match/i)).toBeInTheDocument()
    expect(mocks.changePassword).not.toHaveBeenCalled()
  })

  it('shows password mismatch on confirm blur', async () => {
    const user = userEvent.setup()
    renderPage()

    await user.type(screen.getByLabelText(/^new password$/i), 'Password2!')
    await user.type(screen.getByLabelText(/confirm new password/i), 'Mismatch2!')
    await user.tab()

    expect(await screen.findByText(/passwords do not match/i)).toBeInTheDocument()
    expect(mocks.changePassword).not.toHaveBeenCalled()
  })

  it('changes password on valid submission', async () => {
    const user = userEvent.setup()
    renderPage()

    await user.type(screen.getByLabelText(/current password/i), 'Password1!')
    await user.type(screen.getByLabelText(/^new password$/i), 'Password2!')
    await user.type(screen.getByLabelText(/confirm new password/i), 'Password2!')
    await user.click(screen.getByRole('button', { name: /change password/i }))

    await waitFor(() => {
      expect(mocks.changePassword).toHaveBeenCalledWith(
        expect.objectContaining({
          currentPassword: 'Password1!',
          newPassword: 'Password2!',
        }),
        expect.anything(),
      )
    })

    expect(mocks.refreshProfile).toHaveBeenCalled()
    expect(await screen.findByText(/password updated/i)).toBeInTheDocument()
  })
})
