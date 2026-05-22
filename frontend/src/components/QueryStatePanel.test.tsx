import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { QueryStatePanel } from '@/components/QueryStatePanel'

function renderPanel(props?: { forceEmpty?: boolean; forceError?: boolean }) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <QueryStatePanel {...props} />
    </QueryClientProvider>,
  )
}

vi.mock('@/lib/api/auth-api', () => ({
  apiFetch: vi.fn(async () => ({
    ok: true,
    text: async () => 'Healthy',
  })),
}))

describe('QueryStatePanel', () => {
  it('renders loading state initially', () => {
    renderPanel()
    expect(screen.getByText(/please wait while we fetch your data/i)).toBeInTheDocument()
  })

  it('renders error state with retry', async () => {
    renderPanel({ forceError: true })
    expect(await screen.findByRole('button', { name: /retry/i })).toBeInTheDocument()
  })

  it('renders empty state', async () => {
    renderPanel({ forceEmpty: true })
    expect(await screen.findByText(/no data yet/i)).toBeInTheDocument()
  })

  it('renders success state after loading', async () => {
    renderPanel()
    expect(await screen.findByText(/api connected/i)).toBeInTheDocument()
  })

  it('shows success toast trigger', async () => {
    const user = userEvent.setup()
    renderPanel()
    const button = await screen.findByRole('button', { name: /show success toast/i })
    await user.click(button)
    expect(button).toBeInTheDocument()
  })
})
