import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { RequestorDashboard } from '@/features/requests/RequestorDashboard'
import type { PagedRequests, RequestSummary } from '@/lib/schemas/requests'

const getRequestSummaryMock = vi.fn<() => Promise<RequestSummary>>()
const listRequestsMock =
  vi.fn<(params?: { page?: number; pageSize?: number }) => Promise<PagedRequests>>()
const cancelRequestMock = vi.fn()
const toastSuccessMock = vi.fn()

vi.mock('@/lib/api/requests-api', () => ({
  getRequestSummary: () => getRequestSummaryMock(),
  listRequests: (params?: { page?: number; pageSize?: number }) => listRequestsMock(params),
  createRequest: vi.fn(),
  updateDraftRequest: vi.fn(),
  submitRequest: vi.fn(),
  cancelRequest: (...args: unknown[]) => cancelRequestMock(...args),
}))

vi.mock('@/lib/api/sponsorship-types-api', () => ({
  listSponsorshipTypes: vi.fn().mockResolvedValue([]),
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
  toast: {
    success: (...args: unknown[]) => toastSuccessMock(...args),
    error: vi.fn(),
    warning: vi.fn(),
  },
}))

const summaryFixture: RequestSummary = {
  total: 3,
  draft: 1,
  pendingManagerApproval: 1,
  pendingFinanceReview: 0,
  approved: 1,
  rejected: 0,
  cancelled: 0,
}

const listFixture: PagedRequests = {
  page: 1,
  pageSize: 20,
  totalCount: 3,
  items: [
    {
      id: 1,
      title: 'Draft request',
      requestorName: 'Sarah Chen',
      department: 'Engineering',
      status: 'Draft',
      eventName: 'TechConf',
      eventDate: '2025-08-15T00:00:00Z',
      requestedAmount: 1000,
      sponsorshipTypeName: 'Conference',
      createdAt: '2025-06-01T00:00:00Z',
    },
    {
      id: 2,
      title: 'Pending request',
      requestorName: 'Sarah Chen',
      department: 'Engineering',
      status: 'PendingManagerApproval',
      eventName: 'Summit',
      eventDate: '2025-09-01T00:00:00Z',
      requestedAmount: 2000,
      sponsorshipTypeName: 'Summit',
      createdAt: '2025-06-02T00:00:00Z',
    },
    {
      id: 3,
      title: 'Approved request',
      requestorName: 'Sarah Chen',
      department: 'Engineering',
      status: 'Approved',
      eventName: 'Expo',
      eventDate: '2025-10-01T00:00:00Z',
      requestedAmount: 3000,
      sponsorshipTypeName: 'Exhibition',
      createdAt: '2025-06-03T00:00:00Z',
    },
  ],
}

function renderDashboard(initialEntry = '/dashboard') {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <RequestorDashboard />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('RequestorDashboard', () => {
  beforeEach(() => {
    getRequestSummaryMock.mockResolvedValue(summaryFixture)
    listRequestsMock.mockResolvedValue(listFixture)
  })

  it('renders metrics and request rows', async () => {
    renderDashboard()

    expect(await screen.findByText('Total requests')).toBeVisible()
    expect(screen.getByText('Draft request')).toBeVisible()
    expect(screen.getByText('Pending request')).toBeVisible()
  })

  it('shows edit only for draft and cancel for draft or pending manager', async () => {
    renderDashboard()

    await screen.findByText('Draft request')

    const editButtons = screen.getAllByRole('button', { name: /edit/i })
    const cancelButtons = screen
      .getAllByRole('button', { name: /^cancel$/i })
      .filter((button) => button.textContent === 'Cancel')

    expect(editButtons).toHaveLength(1)
    expect(cancelButtons).toHaveLength(2)
    expect(screen.queryByRole('button', { name: /edit/i, hidden: false })).toBeTruthy()
  })

  it('opens create modal from header action', async () => {
    renderDashboard()
    const user = userEvent.setup()

    await screen.findByRole('button', { name: /new request/i })
    await user.click(screen.getByRole('button', { name: /new request/i }))

    expect(await screen.findByRole('dialog', { name: /new sponsorship request/i })).toBeVisible()
  })

  it('cancels a pending request via confirmation modal', async () => {
    cancelRequestMock.mockResolvedValue({ id: listFixture.items[1].id, status: 'Cancelled' })

    renderDashboard()
    const user = userEvent.setup()

    await screen.findByText('Pending request')

    const cancelButtons = screen
      .getAllByRole('button', { name: /^cancel$/i })
      .filter((button) => button.textContent === 'Cancel')
    await user.click(cancelButtons[0])

    expect(await screen.findByRole('dialog', { name: /cancel request/i })).toBeVisible()
    await user.click(screen.getByRole('button', { name: /cancel request$/i }))

    await waitFor(() => expect(cancelRequestMock).toHaveBeenCalled())
    expect(toastSuccessMock).toHaveBeenCalledWith('Request cancelled.')
  })

  it('shows error state when summary fails', async () => {
    getRequestSummaryMock.mockRejectedValue(new Error('Network error'))
    listRequestsMock.mockRejectedValue(new Error('Network error'))

    renderDashboard()

    expect(await screen.findByText('Something went wrong')).toBeVisible()
    expect(screen.getByRole('button', { name: /retry/i })).toBeVisible()
  })
})
