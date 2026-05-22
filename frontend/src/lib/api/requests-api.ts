import { apiJson } from '@/lib/api/auth-api'
import {
  attachmentSchema,
  pagedRequestsSchema,
  requestDetailSchema,
  requestSummarySchema,
  workflowHistoryEntrySchema,
  type PagedRequests,
  type RequestDetail,
  type RequestSummary,
  type WorkflowHistoryEntry,
  type Attachment,
} from '@/lib/schemas/requests'
import { z } from 'zod'

export interface ListRequestsParams {
  page?: number
  pageSize?: number
}

export async function getRequestSummary(): Promise<RequestSummary> {
  return apiJson('/requests/summary', { method: 'GET' }, requestSummarySchema)
}

export async function listRequests(params: ListRequestsParams = {}): Promise<PagedRequests> {
  const search = new URLSearchParams()
  if (params.page) search.set('page', String(params.page))
  if (params.pageSize) search.set('pageSize', String(params.pageSize))

  const query = search.toString()
  const path = query ? `/requests?${query}` : '/requests'

  return apiJson(path, { method: 'GET' }, pagedRequestsSchema)
}

export async function getRequest(id: string): Promise<RequestDetail> {
  return apiJson(`/requests/${id}`, { method: 'GET' }, requestDetailSchema)
}

export async function getRequestHistory(id: string): Promise<WorkflowHistoryEntry[]> {
  return apiJson(`/requests/${id}/history`, { method: 'GET' }, z.array(workflowHistoryEntrySchema))
}

export async function listAttachments(requestId: string): Promise<Attachment[]> {
  return apiJson(`/requests/${requestId}/attachments`, { method: 'GET' }, z.array(attachmentSchema))
}

export async function approveRequest(id: string, remarks?: string): Promise<RequestDetail> {
  return apiJson(
    `/requests/${id}/approve`,
    {
      method: 'POST',
      body: JSON.stringify({ remarks: remarks?.trim() || null }),
    },
    requestDetailSchema,
  )
}

export async function rejectRequest(id: string, remarks: string): Promise<RequestDetail> {
  return apiJson(
    `/requests/${id}/reject`,
    {
      method: 'POST',
      body: JSON.stringify({ remarks: remarks.trim() }),
    },
    requestDetailSchema,
  )
}
