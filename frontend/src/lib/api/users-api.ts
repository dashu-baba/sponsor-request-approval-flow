import { apiJson } from '@/lib/api/auth-api'
import { userSummarySchema, usersSchema } from '@/features/admin/schemas'
import type { CreateUserValues, UserSummary } from '@/features/admin/types'

export async function listUsers(): Promise<UserSummary[]> {
  return apiJson('/users', { method: 'GET' }, usersSchema)
}

export async function createUser(values: CreateUserValues): Promise<UserSummary> {
  return apiJson(
    '/users',
    {
      method: 'POST',
      body: JSON.stringify(values),
      headers: { 'Content-Type': 'application/json' },
    },
    userSummarySchema,
  )
}
