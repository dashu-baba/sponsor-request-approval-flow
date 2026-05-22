import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi, beforeEach } from 'vitest'

import { SponsorshipTypesPage } from '@/features/admin/SponsorshipTypesPage'
import * as adminApi from '@/features/admin/api/admin-api'

vi.mock('@/features/admin/api/admin-api')

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <SponsorshipTypesPage />
    </QueryClientProvider>,
  )
}

describe('SponsorshipTypesPage', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    vi.mocked(adminApi.listSponsorshipTypes).mockResolvedValue([
      {
        id: 'type-1',
        name: 'Event Sponsorship',
        description: 'Trade shows and conferences',
        isActive: true,
        createdAt: '2026-05-21T08:30:00Z',
        updatedAt: null,
      },
    ])
    vi.mocked(adminApi.listAdminRequests).mockResolvedValue({
      items: [
        {
          id: 'request-1',
          title: 'Tech Summit Booth',
          status: 'PendingManagerApproval',
          eventName: 'Tech Summit',
          eventDate: '2026-08-15',
          requestedAmount: 12500,
          sponsorshipTypeName: 'Event Sponsorship',
          createdAt: '2026-05-21T08:30:00Z',
        },
      ],
      page: 1,
      pageSize: 100,
      totalCount: 1,
    })
  })

  it('creates, edits, and soft-deletes sponsorship types via modals', async () => {
    vi.mocked(adminApi.createSponsorshipType).mockResolvedValue({
      id: 'type-2',
      name: 'Community Grant',
      description: 'Local programs',
      isActive: true,
      createdAt: '2026-05-22T08:30:00Z',
      updatedAt: null,
    })
    vi.mocked(adminApi.updateSponsorshipType).mockResolvedValue({
      id: 'type-1',
      name: 'Event Sponsorship Plus',
      description: 'Premium events',
      isActive: true,
      createdAt: '2026-05-21T08:30:00Z',
      updatedAt: '2026-05-22T08:30:00Z',
    })
    vi.mocked(adminApi.deleteSponsorshipType).mockResolvedValue(undefined)

    renderPage()

    expect(await screen.findByText('Event Sponsorship')).toBeVisible()
    expect(screen.getByText('1')).toBeVisible()

    await userEvent.click(screen.getByRole('button', { name: /add type/i }))
    await userEvent.type(screen.getByLabelText(/^name$/i), 'Community Grant')
    await userEvent.type(screen.getByLabelText(/description/i), 'Local programs')
    await userEvent.click(screen.getByRole('button', { name: /create type/i }))

    await waitFor(() =>
      expect(adminApi.createSponsorshipType).toHaveBeenCalledWith({
        name: 'Community Grant',
        description: 'Local programs',
      }),
    )

    await userEvent.click(screen.getByRole('button', { name: /edit event sponsorship/i }))
    await userEvent.clear(screen.getByLabelText(/^name$/i))
    await userEvent.type(screen.getByLabelText(/^name$/i), 'Event Sponsorship Plus')
    await userEvent.clear(screen.getByLabelText(/description/i))
    await userEvent.type(screen.getByLabelText(/description/i), 'Premium events')
    await userEvent.click(screen.getByRole('button', { name: /save type/i }))

    await waitFor(() =>
      expect(adminApi.updateSponsorshipType).toHaveBeenCalledWith('type-1', {
        name: 'Event Sponsorship Plus',
        description: 'Premium events',
      }),
    )

    await userEvent.click(screen.getByRole('button', { name: /delete event sponsorship/i }))
    expect(screen.getByRole('dialog', { name: /delete sponsorship type/i })).toBeVisible()
    await userEvent.click(screen.getByRole('button', { name: /deactivate type/i }))

    await waitFor(() => expect(adminApi.deleteSponsorshipType).toHaveBeenCalledWith('type-1'))
    expect(
      await screen.findByText(/deactivated because it is referenced by submitted requests/i),
    ).toBeVisible()
  })

  it('shows validation errors clearly', async () => {
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: /add type/i }))
    await userEvent.click(screen.getByRole('button', { name: /create type/i }))
    expect(screen.getByText('Name must be at least 2 characters.')).toBeVisible()
  })
})
