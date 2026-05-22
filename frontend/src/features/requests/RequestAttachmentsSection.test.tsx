import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { RequestAttachmentsSection } from '@/features/requests/RequestAttachmentsSection'
import { ApiError } from '@/lib/api/api-error'

const listAttachmentsMock = vi.fn()
const uploadAttachmentMock = vi.fn()
const toastSuccessMock = vi.fn()
const toastErrorMock = vi.fn()

vi.mock('@/lib/api/requests-api', () => ({
  listAttachments: (...args: unknown[]) => listAttachmentsMock(...args),
  uploadAttachment: (...args: unknown[]) => uploadAttachmentMock(...args),
}))

vi.mock('sonner', () => ({
  toast: {
    success: (...args: unknown[]) => toastSuccessMock(...args),
    error: (...args: unknown[]) => toastErrorMock(...args),
  },
}))

function renderSection(allowUpload: boolean) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <RequestAttachmentsSection requestId={1} allowUpload={allowUpload} />
    </QueryClientProvider>,
  )
}

describe('RequestAttachmentsSection', () => {
  beforeEach(() => {
    listAttachmentsMock.mockResolvedValue([])
    uploadAttachmentMock.mockResolvedValue({
      id: 1,
      fileName: 'brief.pdf',
      contentType: 'application/pdf',
      sizeBytes: 1024,
      createdAt: '2025-06-01T00:00:00Z',
    })
  })

  it('shows upload dropzone when allowUpload is true', async () => {
    renderSection(true)

    expect(await screen.findByText(/drag and drop files here/i)).toBeVisible()
  })

  it('hides upload dropzone when allowUpload is false', async () => {
    renderSection(false)

    await screen.findByText(/no supporting documents/i)
    expect(screen.queryByText(/drag and drop files here/i)).not.toBeInTheDocument()
  })

  it('uploads a file successfully', async () => {
    renderSection(true)
    const user = userEvent.setup()

    await screen.findByText(/drag and drop files here/i)

    const input = document.querySelector('input[type="file"]') as HTMLInputElement
    const file = new File(['pdf'], 'brief.pdf', { type: 'application/pdf' })
    await user.upload(input, file)

    await waitFor(() => expect(uploadAttachmentMock).toHaveBeenCalled())
    expect(toastSuccessMock).toHaveBeenCalledWith('File uploaded.')
  })

  it('shows toast when upload fails', async () => {
    uploadAttachmentMock.mockRejectedValue(new ApiError(400, 'File type not allowed'))

    renderSection(true)
    const user = userEvent.setup()

    await screen.findByText(/drag and drop files here/i)

    const input = document.querySelector('input[type="file"]') as HTMLInputElement
    const file = new File(['bad'], 'brief.pdf', { type: 'application/pdf' })
    await user.upload(input, file)

    await waitFor(() => expect(toastErrorMock).toHaveBeenCalled())
  })
})
