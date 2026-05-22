import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi, beforeEach } from 'vitest'

import { AuditPage } from '@/features/admin/AuditPage'
import * as auditApi from '@/lib/api/audit-api'

vi.mock('@/lib/api/audit-api')

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <AuditPage />
    </QueryClientProvider>,
  )
}

describe('AuditPage', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    vi.mocked(auditApi.listAuditEvents).mockResolvedValue({
      items: [
        {
          id: 1,
          occurredAt: '2026-05-22T12:00:00.000Z',
          actorId: 'admin-1',
          actorDisplayName: 'System Admin',
          action: 'request.created',
          category: 'Request',
          resourceType: 'SponsorshipRequest',
          resourceId: '42',
          summary: 'Created draft request',
          metadata: { requestId: '42' },
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    })
  })

  it('renders audit rows and empty state', async () => {
    renderPage()

    expect(await screen.findByText('request.created')).toBeVisible()
    expect(screen.getByText('System Admin')).toBeVisible()
    expect(screen.getByText('Created draft request')).toBeVisible()
  })

  it('refetches when filters change', async () => {
    const user = userEvent.setup()
    renderPage()
    await screen.findByText('request.created')

    await user.type(screen.getByLabelText('Action'), 'user.created')

    await waitFor(() => {
      expect(vi.mocked(auditApi.listAuditEvents).mock.calls.at(-1)?.[0]).toMatchObject({
        action: 'user.created',
        page: 1,
      })
    })
  })

  it('shows empty state when no events match', async () => {
    vi.mocked(auditApi.listAuditEvents).mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    })

    renderPage()

    expect(await screen.findByText('No audit events')).toBeVisible()
  })
})
