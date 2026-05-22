import { z } from 'zod'

import { Roles } from '@/lib/roles'

export const roleSchema = z.enum([
  Roles.Requestor,
  Roles.Manager,
  Roles.FinanceAdmin,
  Roles.SystemAdmin,
])

export const loginResponseSchema = z.object({
  accessToken: z.string().min(1),
  accessTokenExpiresAt: z.string().datetime({ offset: true }),
  tokenType: z.string().min(1),
})

export const userProfileSchema = z.object({
  id: z.string().min(1),
  email: z.string().email(),
  displayName: z.string().min(1),
  department: z.string().nullable(),
  role: roleSchema,
})

export type LoginResponse = z.infer<typeof loginResponseSchema>
export type UserProfile = z.infer<typeof userProfileSchema>
