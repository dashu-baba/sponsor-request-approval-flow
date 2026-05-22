import { apiJson, apiFetch } from '@/lib/api/auth-api'
import { parseProblemResponse } from '@/lib/api/api-error'
import {
  adminRequestsSchema,
  requestDetailSchema,
  sponsorshipTypeSchema,
  sponsorshipTypesSchema,
  workflowHistorySchema,
} from '@/features/admin/schemas'
import type {
  PagedResult,
  RequestDetail,
  RequestListItem,
  RequestStatus,
  SponsorshipType,
  SponsorshipTypeMutation,
  WorkflowHistoryItem,
} from '@/features/admin/types'

export interface ListAdminRequestsParams {
  page: number
  pageSize: number
  status?: Exclude<RequestStatus, 'Draft'> | undefined
}

export async function listAdminRequests(
  params: ListAdminRequestsParams,
): Promise<PagedResult<RequestListItem>> {
  const searchParams = new URLSearchParams({
    page: String(params.page),
    pageSize: String(params.pageSize),
  })

  if (params.status) {
    searchParams.set('status', params.status)
  }

  return apiJson(`/requests?${searchParams.toString()}`, { method: 'GET' }, adminRequestsSchema)
}

export async function getRequestDetail(id: string): Promise<RequestDetail> {
  return apiJson(`/requests/${id}`, { method: 'GET' }, requestDetailSchema)
}

export async function getRequestHistory(id: string): Promise<WorkflowHistoryItem[]> {
  return apiJson(`/requests/${id}/history`, { method: 'GET' }, workflowHistorySchema)
}

export async function listSponsorshipTypes(): Promise<SponsorshipType[]> {
  return apiJson('/sponsorship-types', { method: 'GET' }, sponsorshipTypesSchema)
}

export async function createSponsorshipType(
  mutation: SponsorshipTypeMutation,
): Promise<SponsorshipType> {
  return apiJson(
    '/sponsorship-types',
    {
      method: 'POST',
      body: JSON.stringify(mutation),
      headers: { 'Content-Type': 'application/json' },
    },
    sponsorshipTypeSchema,
  )
}

export async function updateSponsorshipType(
  id: string,
  mutation: SponsorshipTypeMutation,
): Promise<SponsorshipType> {
  return apiJson(
    `/sponsorship-types/${id}`,
    {
      method: 'PUT',
      body: JSON.stringify(mutation),
      headers: { 'Content-Type': 'application/json' },
    },
    sponsorshipTypeSchema,
  )
}

export async function deleteSponsorshipType(id: string): Promise<void> {
  const response = await apiFetch(`/sponsorship-types/${id}`, { method: 'DELETE' })

  if (!response.ok) {
    throw await parseProblemResponse(response)
  }
}
