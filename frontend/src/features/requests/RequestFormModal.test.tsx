import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { RequestFormModal } from '@/features/requests/RequestFormModal'
import type { RequestDetail } from '@/lib/schemas/requests'

const listSponsorshipTypesMock = vi.fn()
const createRequestMock = vi.fn()
const updateDraftRequestMock = vi.fn()
const submitRequestMock = vi.fn()
const getRequestMock = vi.fn()
const toastSuccessMock = vi.fn()

const typeId = '11111111-1111-4111-8111-111111111111'

vi.mock('@/lib/api/sponsorship-types-api', () => ({
  listSponsorshipTypes: () => listSponsorshipTypesMock(),
}))

vi.mock('@/lib/api/requests-api', () => ({
  createRequest: (...args: unknown[]) => createRequestMock(...args),
  updateDraftRequest: (...args: unknown[]) => updateDraftRequestMock(...args),
  submitRequest: (...args: unknown[]) => submitRequestMock(...args),
  getRequest: (...args: unknown[]) => getRequestMock(...args),
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
  },
}))

const typesFixture = [
  {
    id: typeId,
    name: 'Conference',
    description: null,
    isActive: true,
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: null,
  },
]

function renderModal(props: { requestId?: string } = {}) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <RequestFormModal open onClose={vi.fn()} {...props} />
    </QueryClientProvider>,
  )
}

async function fillValidForm(user: ReturnType<typeof userEvent.setup>) {
  await screen.findByLabelText(/sponsorship type/i)
  await user.type(screen.getByLabelText(/request title/i), 'Test request')
  await user.selectOptions(screen.getByLabelText(/sponsorship type/i), typeId)
  await user.type(screen.getByLabelText(/department/i), 'Engineering')
  await user.type(screen.getByLabelText(/event \/ organisation/i), 'Org')
  fireEvent.change(screen.getByLabelText(/event date/i), { target: { value: '2099-12-31' } })
  fireEvent.change(screen.getByLabelText(/requested amount/i), { target: { value: '100' } })
  await user.type(screen.getByLabelText(/purpose/i), 'Purpose text for sponsorship')
}

describe('RequestFormModal', () => {
  beforeEach(() => {
    listSponsorshipTypesMock.mockResolvedValue(typesFixture)
    createRequestMock.mockResolvedValue({
      id: '22222222-2222-2222-2222-222222222222',
      status: 'Draft',
    })
    submitRequestMock.mockResolvedValue({ id: '22222222-2222-2222-2222-222222222222' })
  })

  it('shows validation errors for empty required fields', async () => {
    renderModal()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /submit request/i }))

    expect(await screen.findByText(/title is required/i)).toBeVisible()
    expect(createRequestMock).not.toHaveBeenCalled()
  })

  it('creates and submits a request', async () => {
    renderModal()
    const user = userEvent.setup()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /submit request/i }))

    await waitFor(() => expect(createRequestMock).toHaveBeenCalled())
    await waitFor(() => expect(submitRequestMock).toHaveBeenCalled())
    expect(toastSuccessMock).toHaveBeenCalledWith('Request submitted for approval.')
  })

  it('blocks edit UI when loaded request is not a draft', async () => {
    const submitted: RequestDetail = {
      id: '33333333-3333-3333-3333-333333333333',
      title: 'Submitted',
      requestorName: 'Sarah Chen',
      requestorId: 'requestor-1',
      department: 'Engineering',
      sponsorshipTypeId: typeId,
      sponsorshipTypeName: 'Conference',
      eventName: 'Org',
      eventDate: '2099-12-31T00:00:00Z',
      requestedAmount: 100,
      purpose: 'Purpose',
      expectedBenefit: null,
      remarks: null,
      status: 'PendingManagerApproval',
      createdAt: '2025-06-01T00:00:00Z',
      updatedAt: null,
    }
    getRequestMock.mockResolvedValue(submitted)

    renderModal({ requestId: submitted.id })

    expect(await screen.findByText(/no longer a draft and cannot be edited/i)).toBeVisible()
    expect(screen.queryByRole('button', { name: /save draft/i })).not.toBeInTheDocument()
  })
})
