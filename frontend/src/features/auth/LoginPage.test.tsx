import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'

import { LoginPage } from '@/features/auth/LoginPage'

const loginMock = vi.fn()
const navigateMock = vi.fn()

vi.mock('@/features/auth/use-auth', () => ({
  useAuth: () => ({
    login: loginMock,
  }),
}))

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom')
  return {
    ...actual,
    useNavigate: () => navigateMock,
  }
})

describe('LoginPage', () => {
  afterEach(() => {
    loginMock.mockReset()
    navigateMock.mockReset()
  })

  it('shows validation-required fields before submit', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    )

    expect(screen.getByLabelText(/email address/i)).toBeRequired()
    expect(screen.getByLabelText(/^password$/i)).toBeRequired()
  })

  it('shows an error when login fails', async () => {
    loginMock.mockRejectedValueOnce(new Error('Invalid email or password.'))
    const user = userEvent.setup()

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    )

    await user.type(screen.getByLabelText(/email address/i), 'requestor@demo.local')
    await user.type(screen.getByLabelText(/^password$/i), 'wrong-password')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    expect(await screen.findByText(/invalid email or password/i)).toBeInTheDocument()
  })

  it('navigates to dashboard after successful login', async () => {
    loginMock.mockResolvedValueOnce(undefined)
    const user = userEvent.setup()

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    )

    await user.type(screen.getByLabelText(/email address/i), 'requestor@demo.local')
    await user.type(screen.getByLabelText(/^password$/i), 'Password1!')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(navigateMock).toHaveBeenCalledWith('/dashboard', { replace: true })
    })
  })
})
