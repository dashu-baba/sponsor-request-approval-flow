import { apiJson } from '@/lib/api/auth-api'
import { auditEventsSchema } from '@/features/admin/schemas'
import type { AuditEvent, PagedResult } from '@/features/admin/types'

export interface ListAuditEventsParams {
  page: number
  pageSize: number
  action?: string
  category?: string
  actorId?: string
  from?: string
  to?: string
  resourceType?: string
  resourceId?: string
  requestId?: string
}

export async function listAuditEvents(
  params: ListAuditEventsParams,
): Promise<PagedResult<AuditEvent>> {
  const searchParams = new URLSearchParams({
    page: String(params.page),
    pageSize: String(params.pageSize),
  })

  if (params.action) searchParams.set('action', params.action)
  if (params.category) searchParams.set('category', params.category)
  if (params.actorId) searchParams.set('actorId', params.actorId)
  if (params.from) searchParams.set('from', params.from)
  if (params.to) searchParams.set('to', params.to)
  if (params.resourceType) searchParams.set('resourceType', params.resourceType)
  if (params.resourceId) searchParams.set('resourceId', params.resourceId)
  if (params.requestId) searchParams.set('requestId', params.requestId)

  return apiJson(`/audit?${searchParams.toString()}`, { method: 'GET' }, auditEventsSchema)
}
