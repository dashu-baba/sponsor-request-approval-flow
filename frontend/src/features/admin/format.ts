import type { RequestStatus } from '@/features/admin/types'

import { formatCurrency } from '@/lib/format'

const statusLabels: Record<RequestStatus, string> = {
  Draft: 'Draft',
  PendingManagerApproval: 'Pending manager approval',
  PendingFinanceReview: 'Pending finance review',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
}

export function formatStatus(status: RequestStatus): string {
  return statusLabels[status]
}

export function formatMoney(amount: number): string {
  return formatCurrency(amount)
}

export function formatDate(value: string): string {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(value))
}

export function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Unexpected error'
}
