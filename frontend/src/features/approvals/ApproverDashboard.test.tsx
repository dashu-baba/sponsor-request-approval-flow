import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'

import { ApproverDashboard } from '@/features/approvals/ApproverDashboard'
import { Roles } from '@/lib/roles'
import type { PagedRequests, RequestSummary } from '@/lib/schemas/requests'

const getRequestSummaryMock = vi.fn<() => Promise<RequestSummary>>()
const listRequestsMock =
  vi.fn<(params?: { page?: number; pageSize?: number }) => Promise<PagedRequests>>()
const approveRequestMock = vi.fn()
const rejectRequestMock = vi.fn()

vi.mock('@/lib/api/requests-api', () => ({
  getRequestSummary: () => getRequestSummaryMock(),
  listRequests: (params?: { page?: number; pageSize?: number }) => listRequestsMock(params),
  approveRequest: (...args: unknown[]) => approveRequestMock(...args),
  rejectRequest: (...args: unknown[]) => rejectRequestMock(...args),
}))

vi.mock('@/features/auth/use-auth', () => ({
  useCurrentUser: () => ({
    id: 'manager-1',
    email: 'manager@demo.local',
    displayName: 'James Okafor',
    department: 'Engineering',
    role: Roles.Manager,
  }),
}))

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
}

function renderDashboard() {
  const queryClient = createTestQueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ApproverDashboard />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

const summaryFixture: RequestSummary = {
  total: 4,
  draft: 2,
  pendingManagerApproval: 1,
  pendingFinanceReview: 1,
  approved: 1,
  rejected: 1,
  cancelled: 0,
}

const queueFixture: PagedRequests = {
  page: 1,
  pageSize: 20,
  totalCount: 3,
  items: [
    {
      id: '11111111-1111-1111-1111-111111111111',
      title: 'TechConf 2025 Sponsorship',
      requestorName: 'Sarah Chen',
      department: 'Engineering',
      status: 'PendingManagerApproval',
      eventName: 'TechConf Asia',
      eventDate: '2025-08-15T00:00:00Z',
      requestedAmount: 5000,
      sponsorshipTypeName: 'Conference',
      createdAt: '2025-06-01T09:00:00Z',
    },
    {
      id: '22222222-2222-2222-2222-222222222222',
      title: 'Draft should not render',
      requestorName: 'Hidden User',
      department: 'Finance',
      status: 'Draft',
      eventName: 'Secret Event',
      eventDate: '2025-09-01T00:00:00Z',
      requestedAmount: 1000,
      sponsorshipTypeName: 'Summit',
      createdAt: '2025-06-02T09:00:00Z',
    },
    {
      id: '33333333-3333-3333-3333-333333333333',
      title: 'Women in Tech Forum',
      requestorName: 'Sarah Chen',
      department: 'HR',
      status: 'Approved',
      eventName: 'WiT Malaysia',
      eventDate: '2025-07-30T00:00:00Z',
      requestedAmount: 3500,
      sponsorshipTypeName: 'Community',
      createdAt: '2025-05-10T09:00:00Z',
    },
  ],
}

describe('ApproverDashboard', () => {
  afterEach(() => {
    getRequestSummaryMock.mockReset()
    listRequestsMock.mockReset()
    approveRequestMock.mockReset()
    rejectRequestMock.mockReset()
  })

  it('renders the approval queue without draft rows', async () => {
    getRequestSummaryMock.mockResolvedValueOnce(summaryFixture)
    listRequestsMock.mockResolvedValueOnce(queueFixture)

    renderDashboard()

    expect(await screen.findByText('TechConf 2025 Sponsorship')).toBeInTheDocument()
    expect(screen.getByText('Women in Tech Forum')).toBeInTheDocument()
    expect(screen.queryByText('Draft should not render')).not.toBeInTheDocument()
    expect(screen.getAllByText('Sarah Chen').length).toBeGreaterThan(0)
    expect(screen.getByText('Pending review')).toBeInTheDocument()
  })

  it('requires remarks when rejecting via modal', async () => {
    getRequestSummaryMock.mockResolvedValueOnce(summaryFixture)
    listRequestsMock.mockResolvedValueOnce(queueFixture)
    const user = userEvent.setup()

    renderDashboard()

    await screen.findByText('TechConf 2025 Sponsorship')
    const rejectButtons = screen.getAllByRole('button', { name: /^reject$/i })
    await user.click(rejectButtons[0])

    expect(await screen.findByRole('dialog')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /confirm rejection/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Remarks are required when rejecting a request.',
    )
    expect(rejectRequestMock).not.toHaveBeenCalled()
  })

  it('submits reject when remarks are provided', async () => {
    getRequestSummaryMock.mockResolvedValue(summaryFixture)
    listRequestsMock.mockResolvedValue(queueFixture)
    rejectRequestMock.mockResolvedValueOnce({})
    const user = userEvent.setup()

    renderDashboard()

    await screen.findByText('TechConf 2025 Sponsorship')
    const rejectButtons = screen.getAllByRole('button', { name: /^reject$/i })
    await user.click(rejectButtons[0])

    await user.type(screen.getByLabelText(/remarks/i), 'Budget constraints for this quarter.')
    await user.click(screen.getByRole('button', { name: /confirm rejection/i }))

    await waitFor(() => {
      expect(rejectRequestMock).toHaveBeenCalledWith(
        '11111111-1111-1111-1111-111111111111',
        'Budget constraints for this quarter.',
      )
    })
  })
})
