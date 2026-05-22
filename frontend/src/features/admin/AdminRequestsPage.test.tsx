import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { AdminRequestDetailPage } from '@/features/admin/AdminRequestDetailPage'
import { AdminRequestsPage } from '@/features/admin/AdminRequestsPage'
import * as adminApi from '@/features/admin/api/admin-api'

vi.mock('@/features/admin/api/admin-api')

function renderWithProviders(route: string, element: ReactNode) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[route]}>
        <Routes>
          <Route path="/dashboard" element={element} />
          <Route path="/dashboard/requests/:id" element={element} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

const sampleRequests = [
  {
    id: 'request-1',
    title: 'Tech Summit Booth',
    status: 'PendingManagerApproval' as const,
    eventName: 'Tech Summit',
    eventDate: '2026-08-15',
    requestedAmount: 12500,
    sponsorshipTypeName: 'Event Sponsorship',
    createdAt: '2026-05-21T08:30:00Z',
  },
  {
    id: 'request-2',
    title: 'Community Workshop',
    status: 'Approved' as const,
    eventName: 'Community Outreach',
    eventDate: '2026-09-02',
    requestedAmount: 4500,
    sponsorshipTypeName: 'Community',
    createdAt: '2026-05-20T12:00:00Z',
  },
]

describe('AdminRequestsPage', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    vi.mocked(adminApi.listSponsorshipTypes).mockResolvedValue([
      {
        id: 'type-1',
        name: 'Event Sponsorship',
        description: 'Events',
        isActive: true,
        createdAt: '2026-05-21T08:30:00Z',
        updatedAt: null,
      },
      {
        id: 'type-2',
        name: 'Community',
        description: 'Community',
        isActive: true,
        createdAt: '2026-05-21T08:30:00Z',
        updatedAt: null,
      },
    ])
    vi.mocked(adminApi.listAdminRequests).mockImplementation(async (params) => {
      if (params.status === 'Approved') {
        return { items: [sampleRequests[1]], page: 1, pageSize: params.pageSize, totalCount: 1 }
      }

      if (params.status === 'PendingManagerApproval') {
        return { items: [sampleRequests[0]], page: 1, pageSize: params.pageSize, totalCount: 1 }
      }

      if (params.status === 'PendingFinanceReview') {
        return { items: [], page: 1, pageSize: params.pageSize, totalCount: 0 }
      }

      if (params.status === 'Rejected') {
        return { items: [], page: 1, pageSize: params.pageSize, totalCount: 0 }
      }

      if (params.page === 2) {
        return {
          items: [
            {
              id: 'request-3',
              title: 'Finance Expo',
              status: 'PendingFinanceReview',
              eventName: 'Finance Conference',
              eventDate: '2026-10-11',
              requestedAmount: 8000,
              sponsorshipTypeName: 'Conference',
              createdAt: '2026-05-19T09:00:00Z',
            },
          ],
          page: 2,
          pageSize: params.pageSize,
          totalCount: 15,
        }
      }

      return {
        items: sampleRequests,
        page: params.page,
        pageSize: params.pageSize,
        totalCount: 15,
      }
    })
  })

  it('filters submitted requests and paginates without showing drafts', async () => {
    renderWithProviders('/dashboard', <AdminRequestsPage />)

    expect(await screen.findByRole('heading', { name: /^dashboard$/i })).toBeVisible()
    expect(screen.getByText('Tech Summit Booth')).toBeVisible()
    expect(screen.getByText('Community Workshop')).toBeVisible()
    expect(screen.queryByText('Draft')).not.toBeInTheDocument()
    expect(adminApi.listAdminRequests).toHaveBeenCalledWith({
      page: 1,
      pageSize: 10,
      status: undefined,
    })

    await userEvent.click(screen.getByRole('button', { name: /next page/i }))

    await waitFor(() => expect(screen.getByText('Finance Expo')).toBeVisible())
    expect(adminApi.listAdminRequests).toHaveBeenCalledWith({
      page: 2,
      pageSize: 10,
      status: undefined,
    })

    await userEvent.selectOptions(screen.getByLabelText(/status/i), 'Approved')

    await waitFor(() => expect(screen.getByText('Community Workshop')).toBeVisible())
    expect(adminApi.listAdminRequests).toHaveBeenCalledWith({
      page: 1,
      pageSize: 10,
      status: 'Approved',
    })
  })

  it('supports search, type filter, and grid view toggle', async () => {
    renderWithProviders('/dashboard', <AdminRequestsPage />)

    expect(await screen.findByText('Tech Summit Booth')).toBeVisible()

    fireEvent.change(screen.getByLabelText(/search requests/i), {
      target: { value: 'community' },
    })

    await waitFor(() => {
      expect(screen.queryByText('Tech Summit Booth')).not.toBeInTheDocument()
      expect(screen.getByText('Community Workshop')).toBeVisible()
    })

    await userEvent.clear(screen.getByLabelText(/search requests/i))
    await userEvent.selectOptions(screen.getByLabelText(/sponsorship type/i), 'Event Sponsorship')

    await waitFor(() => {
      expect(screen.getByText('Tech Summit Booth')).toBeVisible()
      expect(screen.queryByText('Community Workshop')).not.toBeInTheDocument()
    })

    await userEvent.click(screen.getByRole('button', { name: /grid view/i }))

    expect(screen.getByRole('button', { name: /grid view/i })).toHaveAttribute(
      'aria-pressed',
      'true',
    )
  })
})

describe('AdminRequestDetailPage', () => {
  beforeEach(() => {
    vi.resetAllMocks()
  })

  it('shows read-only request details and history without approval controls', async () => {
    vi.mocked(adminApi.getRequestDetail).mockResolvedValue({
      id: 'request-1',
      title: 'Tech Summit Booth',
      requestorName: 'Rina Ahmed',
      requestorId: 'seed-requestor',
      department: 'Marketing',
      sponsorshipTypeId: 'type-1',
      sponsorshipTypeName: 'Event Sponsorship',
      eventName: 'Tech Summit',
      eventDate: '2026-08-15',
      requestedAmount: 12500,
      purpose: 'Sponsor a booth at the annual technology summit.',
      expectedBenefit: 'Qualified enterprise leads',
      remarks: null,
      status: 'PendingFinanceReview',
      createdAt: '2026-05-21T08:30:00Z',
      updatedAt: null,
    })
    vi.mocked(adminApi.getRequestHistory).mockResolvedValue([
      {
        id: 'history-1',
        actorId: 'seed-requestor',
        actorName: 'Rina Ahmed',
        fromStatus: 'Draft',
        toStatus: 'PendingManagerApproval',
        remarks: null,
        occurredAt: '2026-05-21T08:35:00Z',
      },
      {
        id: 'history-2',
        actorId: 'seed-manager',
        actorName: 'Manager User',
        fromStatus: 'PendingManagerApproval',
        toStatus: 'PendingFinanceReview',
        remarks: 'Looks aligned.',
        occurredAt: '2026-05-21T09:20:00Z',
      },
    ])

    renderWithProviders('/dashboard/requests/request-1', <AdminRequestDetailPage />)

    expect(await screen.findByRole('heading', { name: /tech summit booth/i })).toBeVisible()
    expect(screen.getByText('Rina Ahmed')).toBeVisible()
    expect(screen.getByText('Draft → Pending manager approval')).toBeVisible()
    expect(screen.getByText('Pending manager approval → Pending finance review')).toBeVisible()
    expect(screen.getByText('Looks aligned.')).toBeVisible()
    expect(screen.queryByRole('button', { name: /approve/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /reject/i })).not.toBeInTheDocument()
  })
})
