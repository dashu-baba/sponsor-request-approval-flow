import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'

import { RequestDetailPage } from '@/features/requests/RequestDetailPage'
import { ApiError } from '@/lib/api/api-error'
import { Roles } from '@/lib/roles'
import type { RequestDetail, WorkflowHistoryEntry } from '@/lib/schemas/requests'
import type { Role } from '@/lib/roles'

let currentRole: Role = Roles.Manager
let currentUserId = 'manager-1'

const getRequestMock = vi.fn<(id: number) => Promise<RequestDetail>>()
const getRequestHistoryMock = vi.fn<(id: number) => Promise<WorkflowHistoryEntry[]>>()
const approveRequestMock = vi.fn()
const rejectRequestMock = vi.fn()
const listAttachmentsMock = vi.fn().mockResolvedValue([])

const requestId = 1

vi.mock('@/lib/api/requests-api', () => ({
  getRequest: (id: number) => getRequestMock(id),
  getRequestHistory: (id: number) => getRequestHistoryMock(id),
  approveRequest: (...args: unknown[]) => approveRequestMock(...args),
  rejectRequest: (...args: unknown[]) => rejectRequestMock(...args),
  listAttachments: (...args: unknown[]) => listAttachmentsMock(...args),
}))

vi.mock('@/features/auth/use-auth', () => ({
  useCurrentUser: () => ({
    id: currentUserId,
    email: 'user@demo.local',
    displayName: 'Test User',
    department: 'Engineering',
    role: currentRole,
  }),
}))

const requestFixture: RequestDetail = {
  id: requestId,
  title: 'TechConf 2025 Sponsorship',
  requestorName: 'Sarah Chen',
  requestorId: 'user-1',
  department: 'Engineering',
  sponsorshipTypeId: 1,
  sponsorshipTypeName: 'Conference',
  eventName: 'TechConf Asia',
  eventDate: '2025-08-15T00:00:00Z',
  requestedAmount: 5000,
  purpose: 'Support developer community outreach.',
  expectedBenefit: 'Brand visibility',
  remarks: null,
  status: 'PendingManagerApproval',
  createdAt: '2025-06-01T09:00:00Z',
  updatedAt: null,
}

const historyFixture: WorkflowHistoryEntry[] = [
  {
    id: 1,
    actorId: 'user-1',
    actorName: 'Sarah Chen',
    fromStatus: 'Draft',
    toStatus: 'PendingManagerApproval',
    remarks: null,
    occurredAt: '2025-06-01T09:00:00Z',
  },
]

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
}

function renderDetailPage() {
  const queryClient = createTestQueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/requests/${requestId}`]}>
        <Routes>
          <Route path="/requests/:id" element={<RequestDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('RequestDetailPage', () => {
  afterEach(() => {
    getRequestMock.mockReset()
    getRequestHistoryMock.mockReset()
    approveRequestMock.mockReset()
    rejectRequestMock.mockReset()
    listAttachmentsMock.mockReset()
    listAttachmentsMock.mockResolvedValue([])
  })

  it('shows conflict banner when approve returns 409', async () => {
    getRequestMock.mockResolvedValue(requestFixture)
    getRequestHistoryMock.mockResolvedValue(historyFixture)
    approveRequestMock.mockRejectedValueOnce(
      new ApiError(409, 'Request was already updated by another user.'),
    )
    const user = userEvent.setup()

    renderDetailPage()

    await screen.findByRole('heading', { name: /techconf 2025 sponsorship/i })
    await user.click(screen.getByRole('button', { name: /^approve$/i }))
    await user.click(screen.getByRole('button', { name: /confirm approval/i }))

    expect(await screen.findByText(/this request was already actioned/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /refresh/i })).toBeInTheDocument()
  })

  it('shows forbidden banner when reject returns 403', async () => {
    getRequestMock.mockResolvedValue(requestFixture)
    getRequestHistoryMock.mockResolvedValue(historyFixture)
    rejectRequestMock.mockRejectedValueOnce(new ApiError(403, 'Forbidden'))
    const user = userEvent.setup()

    renderDetailPage()

    await screen.findByRole('heading', { name: /techconf 2025 sponsorship/i })
    await user.click(screen.getByRole('button', { name: /^reject$/i }))
    await user.type(screen.getByLabelText(/remarks/i), 'Not aligned with policy.')
    await user.click(screen.getByRole('button', { name: /confirm rejection/i }))

    expect(await screen.findByText(/action not permitted/i)).toBeInTheDocument()
  })

  it('hides attachment upload for requestor when request is not draft', async () => {
    currentRole = Roles.Requestor
    currentUserId = 'requestor-1'

    getRequestMock.mockResolvedValue({
      ...requestFixture,
      requestorId: 'requestor-1',
      status: 'PendingManagerApproval',
    })
    getRequestHistoryMock.mockResolvedValue(historyFixture)

    renderDetailPage()

    await screen.findByRole('heading', { name: /techconf 2025 sponsorship/i })
    expect(screen.queryByText(/drag and drop files here/i)).not.toBeInTheDocument()

    currentRole = Roles.Manager
    currentUserId = 'manager-1'
  })

  it('shows retry when history fails to load', async () => {
    getRequestMock.mockResolvedValue(requestFixture)
    getRequestHistoryMock.mockRejectedValueOnce(new Error('History unavailable'))

    renderDetailPage()

    expect(await screen.findByText(/unable to load workflow history/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument()
  })
})
