import { z } from 'zod'

// PostgreSQL bigint exceeds Number.MAX_SAFE_INTEGER; IDs stay within JS safe range for this app.
export const entityIdSchema = z.number().int().positive()

export const requestStatusSchema = z.enum([
  'Draft',
  'PendingManagerApproval',
  'PendingFinanceReview',
  'Approved',
  'Rejected',
  'Cancelled',
])

export type RequestStatus = z.infer<typeof requestStatusSchema>

export const requestListItemSchema = z.object({
  id: entityIdSchema,
  title: z.string(),
  requestorName: z.string(),
  department: z.string(),
  status: requestStatusSchema,
  eventName: z.string(),
  eventDate: z.string(),
  requestedAmount: z.number(),
  sponsorshipTypeName: z.string(),
  createdAt: z.string(),
})

export type RequestListItem = z.infer<typeof requestListItemSchema>

export const pagedRequestsSchema = z.object({
  items: z.array(requestListItemSchema),
  page: z.number(),
  pageSize: z.number(),
  totalCount: z.number(),
})

export type PagedRequests = z.infer<typeof pagedRequestsSchema>

export const requestSummarySchema = z.object({
  total: z.number(),
  draft: z.number(),
  pendingManagerApproval: z.number(),
  pendingFinanceReview: z.number(),
  approved: z.number(),
  rejected: z.number(),
  cancelled: z.number(),
})

export type RequestSummary = z.infer<typeof requestSummarySchema>

export const requestDetailSchema = z.object({
  id: entityIdSchema,
  title: z.string(),
  requestorName: z.string(),
  requestorId: z.string(),
  department: z.string(),
  sponsorshipTypeId: entityIdSchema,
  sponsorshipTypeName: z.string(),
  eventName: z.string(),
  eventDate: z.string(),
  requestedAmount: z.number(),
  purpose: z.string(),
  expectedBenefit: z.string().nullable(),
  remarks: z.string().nullable(),
  status: requestStatusSchema,
  createdAt: z.string(),
  updatedAt: z.string().nullable(),
})

export type RequestDetail = z.infer<typeof requestDetailSchema>

export const workflowHistoryEntrySchema = z.object({
  id: entityIdSchema,
  actorId: z.string(),
  actorName: z.string(),
  fromStatus: requestStatusSchema,
  toStatus: requestStatusSchema,
  remarks: z.string().nullable(),
  occurredAt: z.string(),
})

export type WorkflowHistoryEntry = z.infer<typeof workflowHistoryEntrySchema>

export const attachmentSchema = z.object({
  id: entityIdSchema,
  fileName: z.string(),
  contentType: z.string(),
  sizeBytes: z.number(),
  createdAt: z.string(),
})

export type Attachment = z.infer<typeof attachmentSchema>
