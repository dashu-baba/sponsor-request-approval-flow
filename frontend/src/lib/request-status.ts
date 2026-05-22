import type { BadgeProps } from '@/components/ui/badge'
import type { RequestStatus } from '@/lib/schemas/requests'
import { Roles, type Role } from '@/lib/roles'

export const requestStatusLabels: Record<RequestStatus, string> = {
  Draft: 'Draft',
  PendingManagerApproval: 'Pending Manager',
  PendingFinanceReview: 'Pending Finance',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
}

export function getStatusBadgeVariant(status: RequestStatus): NonNullable<BadgeProps['variant']> {
  switch (status) {
    case 'Draft':
      return 'draft'
    case 'PendingManagerApproval':
      return 'pendingManager'
    case 'PendingFinanceReview':
      return 'pendingFinance'
    case 'Approved':
      return 'approved'
    case 'Rejected':
      return 'rejected'
    case 'Cancelled':
      return 'cancelled'
    default:
      return 'default'
  }
}

export function formatStatusTransition(fromStatus: RequestStatus, toStatus: RequestStatus): string {
  return `${requestStatusLabels[fromStatus]} → ${requestStatusLabels[toStatus]}`
}

export function canApproveRequest(status: RequestStatus, role: Role): boolean {
  if (role === Roles.Manager) return status === 'PendingManagerApproval'
  if (role === Roles.FinanceAdmin) return status === 'PendingFinanceReview'
  return false
}

export function canEditRequest(status: RequestStatus): boolean {
  return status === 'Draft'
}

export function canCancelRequest(status: RequestStatus): boolean {
  return status === 'Draft' || status === 'PendingManagerApproval'
}

export function canUploadAttachments(status: RequestStatus): boolean {
  return status === 'Draft'
}

export function getPendingReviewCount(
  role: Role,
  summary: {
    pendingManagerApproval: number
    pendingFinanceReview: number
  },
): number {
  if (role === Roles.Manager) return summary.pendingManagerApproval
  if (role === Roles.FinanceAdmin) return summary.pendingFinanceReview
  return summary.pendingManagerApproval + summary.pendingFinanceReview
}
