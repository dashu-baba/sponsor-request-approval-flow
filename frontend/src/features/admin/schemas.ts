import { z } from 'zod'

import { entityIdSchema } from '@/lib/schemas/requests'

const requestStatusSchema = z.enum([
  'Draft',
  'PendingManagerApproval',
  'PendingFinanceReview',
  'Approved',
  'Rejected',
  'Cancelled',
])

export const requestListItemSchema = z.object({
  id: entityIdSchema,
  title: z.string().min(1),
  status: requestStatusSchema,
  eventName: z.string().min(1),
  eventDate: z.string().min(1),
  requestedAmount: z.number(),
  sponsorshipTypeName: z.string().min(1),
  createdAt: z.string().min(1),
})

export const requestDetailSchema = requestListItemSchema.extend({
  requestorName: z.string().min(1),
  requestorId: z.string().min(1),
  department: z.string().min(1),
  sponsorshipTypeId: entityIdSchema,
  purpose: z.string().min(1),
  expectedBenefit: z.string().nullable(),
  remarks: z.string().nullable(),
  updatedAt: z.string().nullable(),
})

export const workflowHistoryItemSchema = z.object({
  id: entityIdSchema,
  actorId: z.string().min(1),
  actorName: z.string().min(1),
  fromStatus: requestStatusSchema,
  toStatus: requestStatusSchema,
  remarks: z.string().nullable(),
  occurredAt: z.string().min(1),
})

export const adminRequestsSchema = z.object({
  items: z.array(requestListItemSchema),
  page: z.number(),
  pageSize: z.number(),
  totalCount: z.number(),
})

export const sponsorshipTypeSchema = z.object({
  id: entityIdSchema,
  name: z.string().min(1),
  description: z.string().nullable(),
  isActive: z.boolean(),
  createdAt: z.string().min(1),
  updatedAt: z.string().nullable(),
})

export const sponsorshipTypesSchema = z.array(sponsorshipTypeSchema)
export const workflowHistorySchema = z.array(workflowHistoryItemSchema)

export const sponsorshipTypeMutationSchema = z.object({
  name: z.string().trim().min(2, 'Name must be at least 2 characters.').max(120),
  description: z
    .string()
    .trim()
    .max(500)
    .transform((value) => (value.length > 0 ? value : null)),
})
