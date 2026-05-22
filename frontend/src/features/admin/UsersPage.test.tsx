import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi, beforeEach } from 'vitest'

import { UsersPage } from '@/features/admin/UsersPage'
import { ApiError } from '@/lib/api/api-error'
import * as usersApi from '@/lib/api/users-api'
import { Roles } from '@/lib/roles'

vi.mock('@/lib/api/users-api')

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <UsersPage />
    </QueryClientProvider>,
  )
}

describe('UsersPage', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    vi.mocked(usersApi.listUsers).mockResolvedValue([
      {
        id: 'seed-requestor',
        email: 'requestor@demo.local',
        displayName: 'Alex Requestor',
        department: 'Engineering',
        role: Roles.Requestor,
      },
    ])
  })

  it('renders users table rows and empty/error states', async () => {
    renderPage()

    expect(await screen.findByText('requestor@demo.local')).toBeVisible()
    expect(screen.getByText('Alex Requestor')).toBeVisible()
    expect(screen.getByText('Engineering')).toBeVisible()
    expect(screen.getByText('Requestor')).toBeVisible()
  })

  it('validates create modal fields and refetches on success', async () => {
    const user = userEvent.setup()
    vi.mocked(usersApi.createUser).mockResolvedValue({
      id: 'new-user',
      email: 'new.user@test.local',
      displayName: 'New User',
      department: 'Marketing',
      role: Roles.Manager,
    })

    renderPage()
    await screen.findByText('requestor@demo.local')

    await user.click(screen.getByRole('button', { name: 'Add user' }))
    await user.click(screen.getByRole('button', { name: 'Create user' }))

    expect(await screen.findByText('Email is required')).toBeVisible()
    expect(screen.getByText('Display name is required')).toBeVisible()
    expect(screen.getByText('Password must be at least 8 characters')).toBeVisible()

    await user.type(screen.getByLabelText('Email'), 'new.user@test.local')
    await user.type(screen.getByLabelText('Display name'), 'New User')
    await user.type(screen.getByLabelText('Department'), 'Marketing')
    await user.selectOptions(screen.getByLabelText('Role'), Roles.Manager)
    await user.type(screen.getByLabelText('Initial password'), 'Password1!')
    await user.click(screen.getByRole('button', { name: 'Create user' }))

    await waitFor(() => {
      expect(vi.mocked(usersApi.createUser).mock.calls[0]?.[0]).toEqual({
        email: 'new.user@test.local',
        displayName: 'New User',
        department: 'Marketing',
        role: Roles.Manager,
        initialPassword: 'Password1!',
      })
    })

    expect(await screen.findByText('New User was created.')).toBeVisible()
    expect(usersApi.listUsers).toHaveBeenCalledTimes(2)
  })

  it('surfaces duplicate email errors on the email field', async () => {
    const user = userEvent.setup()
    vi.mocked(usersApi.createUser).mockRejectedValue(
      new ApiError(409, 'Conflict', 'Conflict', 'A user with this email already exists.'),
    )

    renderPage()
    await screen.findByText('requestor@demo.local')

    await user.click(screen.getByRole('button', { name: 'Add user' }))
    await user.type(screen.getByLabelText('Email'), 'requestor@demo.local')
    await user.type(screen.getByLabelText('Display name'), 'Duplicate User')
    await user.type(screen.getByLabelText('Initial password'), 'Password1!')
    await user.click(screen.getByRole('button', { name: 'Create user' }))

    expect(await screen.findByText('A user with this email already exists.')).toBeVisible()
  })
})
