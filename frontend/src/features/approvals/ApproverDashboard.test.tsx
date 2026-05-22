import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { ApproverDashboard } from '@/features/approvals/ApproverDashboard'
import { ApiError } from '@/lib/api/api-error'
import { Roles, type Role } from '@/lib/roles'
import type { PagedRequests, RequestSummary } from '@/lib/schemas/requests'

const getRequestSummaryMock = vi.fn<() => Promise<RequestSummary>>()
const listRequestsMock =
  vi.fn<(params?: { page?: number; pageSize?: number }) => Promise<PagedRequests>>()
const approveRequestMock = vi.fn()
const rejectRequestMock = vi.fn()
const toastWarningMock = vi.fn()
const toastErrorMock = vi.fn()

let currentRole: Role = Roles.Manager

vi.mock('@/lib/api/requests-api', () => ({
  getRequestSummary: () => getRequestSummaryMock(),
  listRequests: (params?: { page?: number; pageSize?: number }) => listRequestsMock(params),
  approveRequest: (...args: unknown[]) => approveRequestMock(...args),
  rejectRequest: (...args: unknown[]) => rejectRequestMock(...args),
}))

vi.mock('@/features/auth/use-auth', () => ({
  useCurrentUser: () => ({
    id: 'reviewer-1',
    email: 'reviewer@demo.local',
    displayName: 'James Okafor',
    department: 'Engineering',
    role: currentRole,
  }),
}))

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: (...args: unknown[]) => toastErrorMock(...args),
    warning: (...args: unknown[]) => toastWarningMock(...args),
  },
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
  const view = render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ApproverDashboard />
      </MemoryRouter>
    </QueryClientProvider>,
  )

  return { queryClient, ...view }
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

const managerQueueFixture: PagedRequests = {
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

const financeQueueFixture: PagedRequests = {
  page: 1,
  pageSize: 20,
  totalCount: 1,
  items: [
    {
      id: '44444444-4444-4444-4444-444444444444',
      title: 'Annual Gala Sponsorship',
      requestorName: 'Alex Rivera',
      department: 'Marketing',
      status: 'PendingFinanceReview',
      eventName: 'Company Gala',
      eventDate: '2025-10-01T00:00:00Z',
      requestedAmount: 12000,
      sponsorshipTypeName: 'Event',
      createdAt: '2025-06-10T09:00:00Z',
    },
  ],
}

describe('ApproverDashboard', () => {
  beforeEach(() => {
    currentRole = Roles.Manager
  })

  afterEach(() => {
    getRequestSummaryMock.mockReset()
    listRequestsMock.mockReset()
    approveRequestMock.mockReset()
    rejectRequestMock.mockReset()
    toastWarningMock.mockReset()
    toastErrorMock.mockReset()
  })

  it('renders the approval queue without draft rows', async () => {
    getRequestSummaryMock.mockResolvedValueOnce(summaryFixture)
    listRequestsMock.mockResolvedValueOnce(managerQueueFixture)

    renderDashboard()

    expect(await screen.findByText('TechConf 2025 Sponsorship')).toBeInTheDocument()
    expect(screen.getByText('Women in Tech Forum')).toBeInTheDocument()
    expect(screen.queryByText('Draft should not render')).not.toBeInTheDocument()
    expect(screen.getAllByText('Sarah Chen').length).toBeGreaterThan(0)
    expect(screen.getByText('Pending review')).toBeInTheDocument()
  })

  it('renders finance queue with actionable pending finance rows', async () => {
    currentRole = Roles.FinanceAdmin
    getRequestSummaryMock.mockResolvedValueOnce(summaryFixture)
    listRequestsMock.mockResolvedValueOnce(financeQueueFixture)

    renderDashboard()

    expect(await screen.findByText('Annual Gala Sponsorship')).toBeInTheDocument()
    expect(screen.getByText('Alex Rivera')).toBeInTheDocument()
    expect(screen.getAllByRole('button', { name: /^approve$/i }).length).toBeGreaterThan(0)
    expect(screen.getAllByRole('button', { name: /^reject$/i }).length).toBeGreaterThan(0)
  })

  it('requires remarks when rejecting via modal', async () => {
    getRequestSummaryMock.mockResolvedValueOnce(summaryFixture)
    listRequestsMock.mockResolvedValueOnce(managerQueueFixture)
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
    listRequestsMock.mockResolvedValue(managerQueueFixture)
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

  it('submits approve from the dashboard modal', async () => {
    getRequestSummaryMock.mockResolvedValue(summaryFixture)
    listRequestsMock.mockResolvedValue(managerQueueFixture)
    approveRequestMock.mockResolvedValueOnce({ id: '11111111-1111-1111-1111-111111111111' })
    const user = userEvent.setup()

    renderDashboard()

    await screen.findByText('TechConf 2025 Sponsorship')
    const approveButtons = screen.getAllByRole('button', { name: /^approve$/i })
    await user.click(approveButtons[0])
    await user.click(screen.getByRole('button', { name: /confirm approval/i }))

    await waitFor(() => {
      expect(approveRequestMock).toHaveBeenCalledWith('11111111-1111-1111-1111-111111111111', '')
    })
  })

  it('shows warning toast and refetches when approve returns 409', async () => {
    getRequestSummaryMock.mockResolvedValue(summaryFixture)
    listRequestsMock.mockResolvedValue(managerQueueFixture)
    approveRequestMock.mockRejectedValueOnce(
      new ApiError(409, 'Request was already updated by another user.'),
    )
    const user = userEvent.setup()

    renderDashboard()

    await screen.findByText('TechConf 2025 Sponsorship')
    const approveButtons = screen.getAllByRole('button', { name: /^approve$/i })
    await user.click(approveButtons[0])
    await user.click(screen.getByRole('button', { name: /confirm approval/i }))

    await waitFor(() => {
      expect(toastWarningMock).toHaveBeenCalledWith(
        'This request was already updated. Refreshing list…',
      )
      expect(listRequestsMock.mock.calls.length).toBeGreaterThan(1)
    })
  })

  it('shows error toast and refetches when reject returns 403', async () => {
    getRequestSummaryMock.mockResolvedValue(summaryFixture)
    listRequestsMock.mockResolvedValue(managerQueueFixture)
    rejectRequestMock.mockRejectedValueOnce(new ApiError(403, 'Forbidden'))
    const user = userEvent.setup()

    renderDashboard()

    await screen.findByText('TechConf 2025 Sponsorship')
    const rejectButtons = screen.getAllByRole('button', { name: /^reject$/i })
    await user.click(rejectButtons[0])
    await user.type(screen.getByLabelText(/remarks/i), 'Not permitted.')
    await user.click(screen.getByRole('button', { name: /confirm rejection/i }))

    await waitFor(() => {
      expect(toastErrorMock).toHaveBeenCalledWith(
        'You no longer have permission to action this request.',
      )
      expect(listRequestsMock.mock.calls.length).toBeGreaterThan(1)
    })
  })
})
