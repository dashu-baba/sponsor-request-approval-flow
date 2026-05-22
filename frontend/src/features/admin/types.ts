import type { z } from 'zod'

import type { createUserSchema, userSummarySchema } from '@/features/admin/schemas'

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
  id: number
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
  sponsorshipTypeId: number
  purpose: string
  expectedBenefit: string | null
  remarks: string | null
  updatedAt: string | null
}

export interface WorkflowHistoryItem {
  id: number
  actorId: string
  actorName: string
  fromStatus: RequestStatus
  toStatus: RequestStatus
  remarks: string | null
  occurredAt: string
}

export interface AuditEvent {
  id: number
  occurredAt: string
  actorId: string
  actorDisplayName: string
  action: string
  category: string
  resourceType: string
  resourceId: string
  summary: string | null
  metadata: Record<string, unknown> | null
}

export interface SponsorshipType {
  id: number
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

export type UserSummary = z.infer<typeof userSummarySchema>

export type CreateUserInput = z.input<typeof createUserSchema>

export type CreateUserValues = z.output<typeof createUserSchema>
