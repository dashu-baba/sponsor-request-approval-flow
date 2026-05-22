export type RequestStatus =
  | 'Draft'
  | 'PendingManagerApproval'
  | 'PendingFinanceReview'
  | 'Approved'
  | 'Rejected'
  | 'Cancelled'

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface RequestListItem {
  id: string
  title: string
  status: RequestStatus
  eventName: string
  eventDate: string
  requestedAmount: number
  sponsorshipTypeName: string
  createdAt: string
}

export interface RequestDetail extends RequestListItem {
  requestorName: string
  requestorId: string
  department: string
  sponsorshipTypeId: string
  purpose: string
  expectedBenefit: string | null
  remarks: string | null
  updatedAt: string | null
}

export interface WorkflowHistoryItem {
  id: string
  actorId: string
  actorName: string
  fromStatus: RequestStatus
  toStatus: RequestStatus
  remarks: string | null
  occurredAt: string
}

export interface SponsorshipType {
  id: string
  name: string
  description: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

export interface SponsorshipTypeMutation {
  name: string
  description: string | null
}
