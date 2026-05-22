import { entityIdSchema } from '@/lib/schemas/requests'

export function parseRouteEntityId(id: string | undefined): number | null {
  const parsed = Number(id)
  const result = entityIdSchema.safeParse(parsed)
  return result.success ? result.data : null
}
