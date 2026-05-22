import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { RequestFormModal } from '@/features/requests/RequestFormModal'
const listSponsorshipTypesMock = vi.fn()
const createRequestMock = vi.fn()

vi.mock('@/lib/api/sponsorship-types-api', () => ({
  listSponsorshipTypes: () => listSponsorshipTypesMock(),
}))

vi.mock('@/lib/api/requests-api', () => ({
  createRequest: (...args: unknown[]) => createRequestMock(...args),
  updateDraftRequest: vi.fn(),
  submitRequest: vi.fn(),
  getRequest: vi.fn(),
}))

vi.mock('@/features/auth/use-auth', () => ({
  useCurrentUser: () => ({
    id: 'requestor-1',
    email: 'requestor@demo.local',
    displayName: 'Sarah Chen',
    department: 'Engineering',
    role: 'Requestor',
  }),
}))

vi.mock('sonner', () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}))

function renderModal() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <RequestFormModal open onClose={vi.fn()} />
    </QueryClientProvider>,
  )
}

describe('RequestFormModal', () => {
  it('shows validation errors for empty required fields', async () => {
    listSponsorshipTypesMock.mockResolvedValue([
      {
        id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1',
        name: 'Conference',
        description: null,
        isActive: true,
        createdAt: '2025-01-01T00:00:00Z',
        updatedAt: null,
      },
    ])

    renderModal()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /submit request/i }))

    expect(await screen.findByText(/title is required/i)).toBeVisible()
    expect(createRequestMock).not.toHaveBeenCalled()
  })
})
