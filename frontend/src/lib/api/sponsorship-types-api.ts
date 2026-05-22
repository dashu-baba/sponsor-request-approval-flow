import { z } from 'zod'

import { apiJson } from '@/lib/api/auth-api'
import { entityIdSchema } from '@/lib/schemas/requests'

export const sponsorshipTypeSchema = z.object({
  id: entityIdSchema,
  name: z.string(),
  description: z.string().nullable(),
  isActive: z.boolean(),
  createdAt: z.string(),
  updatedAt: z.string().nullable(),
})

export type SponsorshipType = z.infer<typeof sponsorshipTypeSchema>

const sponsorshipTypesSchema = z.array(sponsorshipTypeSchema)

export async function listSponsorshipTypes(): Promise<SponsorshipType[]> {
  return apiJson('/sponsorship-types', { method: 'GET' }, sponsorshipTypesSchema)
}
