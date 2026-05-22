import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'

import { ApproveRejectModal, type ApprovalAction } from '@/features/approvals/ApproveRejectModal'
import { ApiError } from '@/lib/api/api-error'

const approveRequestMock = vi.fn()
const rejectRequestMock = vi.fn()
const toastWarningMock = vi.fn()
const toastErrorMock = vi.fn()

vi.mock('@/lib/api/requests-api', () => ({
  approveRequest: (...args: unknown[]) => approveRequestMock(...args),
  rejectRequest: (...args: unknown[]) => rejectRequestMock(...args),
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

function renderModal(
  action: ApprovalAction,
  callbacks: {
    onConflict409?: () => void
    onForbidden403?: () => void
    onSuccess?: () => void
  } = {},
) {
  const queryClient = createTestQueryClient()
  const onOpenChange = vi.fn()

  render(
    <QueryClientProvider client={queryClient}>
      <ApproveRejectModal
        open
        onOpenChange={onOpenChange}
        action={action}
        requestId="11111111-1111-1111-1111-111111111111"
        requestTitle="TechConf 2025 Sponsorship"
        onConflict409={callbacks.onConflict409}
        onForbidden403={callbacks.onForbidden403}
        onSuccess={callbacks.onSuccess}
      />
    </QueryClientProvider>,
  )

  return { onOpenChange, queryClient }
}

describe('ApproveRejectModal', () => {
  afterEach(() => {
    approveRequestMock.mockReset()
    rejectRequestMock.mockReset()
    toastWarningMock.mockReset()
    toastErrorMock.mockReset()
  })

  it('shows validation when reject remarks are empty', async () => {
    const user = userEvent.setup()
    renderModal('reject')

    await user.click(screen.getByRole('button', { name: /confirm rejection/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Remarks are required when rejecting a request.',
    )
    expect(rejectRequestMock).not.toHaveBeenCalled()
  })

  it('calls onConflict409 when approve returns 409', async () => {
    approveRequestMock.mockRejectedValueOnce(
      new ApiError(409, 'Request was already updated by another user.'),
    )
    const onConflict409 = vi.fn()
    const user = userEvent.setup()

    const { onOpenChange } = renderModal('approve', { onConflict409 })

    await user.click(screen.getByRole('button', { name: /confirm approval/i }))

    await waitFor(() => {
      expect(onConflict409).toHaveBeenCalledTimes(1)
      expect(onOpenChange).toHaveBeenCalledWith(false)
    })
  })

  it('calls onForbidden403 when reject returns 403', async () => {
    rejectRequestMock.mockRejectedValueOnce(new ApiError(403, 'Forbidden'))
    const onForbidden403 = vi.fn()
    const user = userEvent.setup()

    renderModal('reject', { onForbidden403 })

    await user.type(screen.getByLabelText(/remarks/i), 'Not aligned with policy.')
    await user.click(screen.getByRole('button', { name: /confirm rejection/i }))

    await waitFor(() => {
      expect(onForbidden403).toHaveBeenCalledTimes(1)
    })
  })

  it('shows fallback warning toast when approve returns 409 without callback', async () => {
    approveRequestMock.mockRejectedValueOnce(
      new ApiError(409, 'Request was already updated by another user.'),
    )
    const user = userEvent.setup()

    const { onOpenChange } = renderModal('approve')

    await user.click(screen.getByRole('button', { name: /confirm approval/i }))

    await waitFor(() => {
      expect(onOpenChange).toHaveBeenCalledWith(false)
      expect(toastWarningMock).toHaveBeenCalledWith('Request was already updated by another user.')
    })
  })

  it('invalidates request queries after successful approve', async () => {
    approveRequestMock.mockResolvedValueOnce({ id: '11111111-1111-1111-1111-111111111111' })
    const onSuccess = vi.fn()
    const user = userEvent.setup()

    const { queryClient } = renderModal('approve', { onSuccess })
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries')

    await user.click(screen.getByRole('button', { name: /confirm approval/i }))

    await waitFor(() => {
      expect(approveRequestMock).toHaveBeenCalledWith('11111111-1111-1111-1111-111111111111', '')
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['requests'] })
      expect(onSuccess).toHaveBeenCalledTimes(1)
    })
  })
})
