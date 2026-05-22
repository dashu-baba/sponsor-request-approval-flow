import { z } from 'zod'

import { Roles } from '@/lib/roles'
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
  requestorName: z.string().min(1),
  department: z.string().min(1),
  status: requestStatusSchema,
  eventName: z.string().min(1),
  eventDate: z.string().min(1),
  requestedAmount: z.number(),
  sponsorshipTypeName: z.string().min(1),
  createdAt: z.string().min(1),
})

export const requestDetailSchema = requestListItemSchema.extend({
  requestorId: z.string().min(1),
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

export const workflowHistorySchema = z.array(workflowHistoryItemSchema)

export const auditEventSchema = z.object({
  id: entityIdSchema,
  occurredAt: z.string().min(1),
  actorId: z.string().min(1),
  actorDisplayName: z.string().min(1),
  action: z.string().min(1),
  category: z.string().min(1),
  resourceType: z.string().min(1),
  resourceId: z.string().min(1),
  summary: z.string().nullable(),
  metadata: z.record(z.string(), z.unknown()).nullable(),
})

export const auditEventsSchema = z.object({
  items: z.array(auditEventSchema),
  page: z.number(),
  pageSize: z.number(),
  totalCount: z.number(),
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

export const sponsorshipTypeMutationSchema = z.object({
  name: z.string().trim().min(2, 'Name must be at least 2 characters.').max(120),
  description: z
    .string()
    .trim()
    .max(500)
    .transform((value) => (value.length > 0 ? value : null)),
})

const roleSchema = z.enum([Roles.Requestor, Roles.Manager, Roles.FinanceAdmin, Roles.SystemAdmin])

export const userSummarySchema = z.object({
  id: z.string(),
  email: z.string(),
  displayName: z.string(),
  department: z.string().nullable(),
  role: roleSchema,
})

export const usersSchema = z.array(userSummarySchema)

export const createUserSchema = z.object({
  email: z.string().trim().min(1, 'Email is required').email('Enter a valid email address'),
  displayName: z.string().trim().min(1, 'Display name is required').max(120),
  department: z
    .string()
    .trim()
    .max(120)
    .optional()
    .transform((value) => (value === '' ? undefined : value)),
  role: roleSchema,
  initialPassword: z
    .string()
    .min(8, 'Password must be at least 8 characters')
    .regex(/[a-z]/, 'Password must include a lowercase letter')
    .regex(/[A-Z]/, 'Password must include an uppercase letter')
    .regex(/\d/, 'Password must include a digit'),
})
