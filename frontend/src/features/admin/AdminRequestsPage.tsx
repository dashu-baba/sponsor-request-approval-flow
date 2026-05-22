import { useQueries, useQuery } from '@tanstack/react-query'
import {
  CheckCircle2,
  Clock3,
  Eye,
  FileText,
  LayoutGrid,
  LayoutList,
  Search,
  XCircle,
} from 'lucide-react'
import { useMemo, useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'

import { PageHeader } from '@/components/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/states/query-states'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { listAdminRequests, listSponsorshipTypes } from '@/features/admin/api/admin-api'
import { getErrorMessage } from '@/features/admin/format'
import type { RequestListItem, RequestStatus } from '@/features/admin/types'
import { RequestStatusBadge } from '@/features/requests/RequestStatusBadge'
import { RequestsTable } from '@/features/requests/RequestsTable'
import { formatCurrency, formatDate, formatRequestId, requestIdMatchesQuery } from '@/lib/format'
import { queryKeys } from '@/lib/query-client'
import { requestStatusLabels } from '@/lib/request-status'

const pageSize = 10
const clientFilterPageSize = 100

const statusOptions: Exclude<RequestStatus, 'Draft'>[] = [
  'PendingManagerApproval',
  'PendingFinanceReview',
  'Approved',
  'Rejected',
  'Cancelled',
]

type ViewMode = 'list' | 'grid'

function matchesSearch(request: RequestListItem, search: string): boolean {
  const query = search.trim().toLowerCase()
  if (!query) return true

  return (
    request.title.toLowerCase().includes(query) ||
    request.eventName.toLowerCase().includes(query) ||
    requestIdMatchesQuery(request.id, search)
  )
}

export function AdminRequestsPage() {
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState<Exclude<RequestStatus, 'Draft'> | ''>('')
  const [search, setSearch] = useState('')
  const [sponsorshipTypeName, setSponsorshipTypeName] = useState('')
  const [viewMode, setViewMode] = useState<ViewMode>('list')

  const trimmedSearch = search.trim()
  const needsClientFilter = trimmedSearch !== '' || sponsorshipTypeName !== ''

  const metricsQueries = useQueries({
    queries: [
      {
        queryKey: ['admin-metrics', 'total'],
        queryFn: () => listAdminRequests({ page: 1, pageSize: 1 }),
      },
      {
        queryKey: ['admin-metrics', 'pending-manager'],
        queryFn: () =>
          listAdminRequests({ page: 1, pageSize: 1, status: 'PendingManagerApproval' }),
      },
      {
        queryKey: ['admin-metrics', 'pending-finance'],
        queryFn: () => listAdminRequests({ page: 1, pageSize: 1, status: 'PendingFinanceReview' }),
      },
      {
        queryKey: ['admin-metrics', 'approved'],
        queryFn: () => listAdminRequests({ page: 1, pageSize: 1, status: 'Approved' }),
      },
      {
        queryKey: ['admin-metrics', 'rejected'],
        queryFn: () => listAdminRequests({ page: 1, pageSize: 1, status: 'Rejected' }),
      },
    ],
  })

  const typesQuery = useQuery({
    queryKey: queryKeys.sponsorshipTypes.list,
    queryFn: listSponsorshipTypes,
  })

  const listQuery = useQuery({
    queryKey: ['admin-requests', needsClientFilter ? 1 : page, status, needsClientFilter],
    queryFn: () =>
      listAdminRequests({
        page: needsClientFilter ? 1 : page,
        pageSize: needsClientFilter ? clientFilterPageSize : pageSize,
        status: status || undefined,
      }),
  })

  const filteredItems = useMemo(() => {
    const items = listQuery.data?.items ?? []
    return items.filter(
      (request) =>
        matchesSearch(request, trimmedSearch) &&
        (sponsorshipTypeName === '' || request.sponsorshipTypeName === sponsorshipTypeName),
    )
  }, [listQuery.data?.items, trimmedSearch, sponsorshipTypeName])

  const paginatedItems = useMemo(() => {
    if (!needsClientFilter) {
      return filteredItems
    }

    const start = (page - 1) * pageSize
    return filteredItems.slice(start, start + pageSize)
  }, [filteredItems, needsClientFilter, page])

  const totalCount = needsClientFilter
    ? filteredItems.length
    : (listQuery.data?.totalCount ?? filteredItems.length)
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

  const metricsLoading = metricsQueries.some((query) => query.isLoading)
  const metricsError = metricsQueries.find((query) => query.isError)?.error

  if (listQuery.isLoading || metricsLoading || typesQuery.isLoading) {
    return <LoadingState title="Loading submitted requests" metricCount={4} tableRows={8} />
  }

  if (listQuery.isError || typesQuery.isError) {
    return (
      <ErrorState
        message={getErrorMessage(listQuery.error ?? typesQuery.error)}
        onRetry={() => {
          void listQuery.refetch()
          void typesQuery.refetch()
          metricsQueries.forEach((query) => {
            void query.refetch()
          })
        }}
      />
    )
  }

  const totalRequests = metricsQueries[0]?.data?.totalCount ?? 0
  const pendingReview =
    (metricsQueries[1]?.data?.totalCount ?? 0) + (metricsQueries[2]?.data?.totalCount ?? 0)
  const approvedCount = metricsQueries[3]?.data?.totalCount ?? 0
  const rejectedCount = metricsQueries[4]?.data?.totalCount ?? 0

  return (
    <div className="space-y-6">
      <PageHeader
        title="Dashboard"
        subtitle="Welcome back. Here's an overview of all submitted sponsorship requests."
      />

      <Alert variant="info">
        <AlertTitle>Submitted requests only</AlertTitle>
        <AlertDescription>
          Private drafts are visible only to their requestor. System admins can inspect workflows,
          but cannot approve or reject requests.
        </AlertDescription>
      </Alert>

      {metricsError ? (
        <Alert variant="destructive">
          <AlertTitle>Metrics unavailable</AlertTitle>
          <AlertDescription>{getErrorMessage(metricsError)}</AlertDescription>
        </Alert>
      ) : null}

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard
          icon={<FileText className="h-[18px] w-[18px] text-brand" aria-hidden="true" />}
          iconClassName="bg-brand-light"
          label="Total requests"
          value={totalRequests}
          valueClassName="text-brand"
        />
        <MetricCard
          icon={<Clock3 className="h-[18px] w-[18px] text-warning" aria-hidden="true" />}
          iconClassName="bg-warning-bg"
          label="Pending review"
          value={pendingReview}
          valueClassName="text-warning"
        />
        <MetricCard
          icon={<CheckCircle2 className="h-[18px] w-[18px] text-success" aria-hidden="true" />}
          iconClassName="bg-success-bg"
          label="Approved"
          value={approvedCount}
          valueClassName="text-success"
        />
        <MetricCard
          icon={<XCircle className="h-[18px] w-[18px] text-danger" aria-hidden="true" />}
          iconClassName="bg-danger-bg"
          label="Rejected"
          value={rejectedCount}
          valueClassName="text-danger"
        />
      </div>

      <Card>
        <CardContent className="space-y-5 p-5">
          <div className="relative">
            <Search
              className="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-text-hint"
              aria-hidden="true"
            />
            <Input
              type="search"
              value={search}
              onChange={(event) => {
                setSearch(event.target.value)
                setPage(1)
              }}
              placeholder="Search requests by title, event, or ID…"
              className="pl-9"
              aria-label="Search requests"
            />
          </div>

          <div className="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
              <label className="flex min-w-[180px] flex-col gap-1 text-[13px] font-medium text-text-primary">
                Status
                <select
                  className="h-10 rounded-[8px] border border-border bg-surface px-3 text-[13px] focus-visible:border-brand-mid focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-brand/10"
                  value={status}
                  onChange={(event) => {
                    setStatus(event.target.value as Exclude<RequestStatus, 'Draft'> | '')
                    setPage(1)
                  }}
                >
                  <option value="">All submitted</option>
                  {statusOptions.map((option) => (
                    <option key={option} value={option}>
                      {requestStatusLabels[option]}
                    </option>
                  ))}
                </select>
              </label>

              <label className="flex min-w-[180px] flex-col gap-1 text-[13px] font-medium text-text-primary">
                Sponsorship type
                <select
                  className="h-10 rounded-[8px] border border-border bg-surface px-3 text-[13px] focus-visible:border-brand-mid focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-brand/10"
                  value={sponsorshipTypeName}
                  onChange={(event) => {
                    setSponsorshipTypeName(event.target.value)
                    setPage(1)
                  }}
                >
                  <option value="">All types</option>
                  {(typesQuery.data ?? []).map((type) => (
                    <option key={type.id} value={type.name}>
                      {type.name}
                    </option>
                  ))}
                </select>
              </label>

              <div className="pb-2 text-[13px] text-text-secondary">
                {totalCount} request{totalCount === 1 ? '' : 's'}
              </div>
            </div>

            <div
              className="flex overflow-hidden rounded-[8px] border border-border bg-surface"
              role="group"
              aria-label="View toggle"
            >
              <button
                type="button"
                className={`flex items-center px-3 py-2 transition-colors ${
                  viewMode === 'list'
                    ? 'bg-brand-light text-brand'
                    : 'text-text-hint hover:bg-page hover:text-text-secondary'
                }`}
                onClick={() => setViewMode('list')}
                aria-label="List view"
                aria-pressed={viewMode === 'list'}
              >
                <LayoutList className="h-4 w-4" aria-hidden="true" />
              </button>
              <button
                type="button"
                className={`flex items-center px-3 py-2 transition-colors ${
                  viewMode === 'grid'
                    ? 'bg-brand-light text-brand'
                    : 'text-text-hint hover:bg-page hover:text-text-secondary'
                }`}
                onClick={() => setViewMode('grid')}
                aria-label="Grid view"
                aria-pressed={viewMode === 'grid'}
              >
                <LayoutGrid className="h-4 w-4" aria-hidden="true" />
              </button>
            </div>
          </div>

          {paginatedItems.length === 0 ? (
            <EmptyState
              title="No submitted requests"
              description="Requests appear here after requestors submit them."
            />
          ) : viewMode === 'list' ? (
            <RequestListTable requests={paginatedItems} />
          ) : (
            <RequestGrid requests={paginatedItems} />
          )}

          <div className="flex items-center justify-between border-t border-border pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={() => setPage((current) => Math.max(1, current - 1))}
              disabled={page <= 1}
            >
              Previous page
            </Button>
            <span className="text-[13px] text-text-secondary">
              Page {page} of {totalPages}
            </span>
            <Button
              type="button"
              variant="outline"
              onClick={() => setPage((current) => current + 1)}
              disabled={page >= totalPages}
            >
              Next page
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}

function MetricCard({
  icon,
  iconClassName,
  label,
  value,
  valueClassName,
}: {
  icon: ReactNode
  iconClassName: string
  label: string
  value: number
  valueClassName: string
}) {
  return (
    <Card>
      <CardContent className="space-y-3 p-5">
        <div className={`flex h-9 w-9 items-center justify-center rounded-[9px] ${iconClassName}`}>
          {icon}
        </div>
        <div className={`text-[28px] font-semibold leading-none ${valueClassName}`}>{value}</div>
        <div className="text-xs text-text-secondary">{label}</div>
      </CardContent>
    </Card>
  )
}

function RequestListTable({ requests }: { requests: RequestListItem[] }) {
  return (
    <RequestsTable
      requests={requests}
      detailHref={(request) => `/dashboard/requests/${request.id}`}
      renderActions={(request) => (
        <Button asChild variant="outline" size="sm">
          <Link to={`/dashboard/requests/${request.id}`}>
            <Eye className="h-4 w-4" aria-hidden="true" />
            View
          </Link>
        </Button>
      )}
    />
  )
}

function RequestGrid({ requests }: { requests: RequestListItem[] }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
      {requests.map((request) => (
        <Card key={request.id} className="h-full">
          <CardContent className="flex h-full flex-col gap-4 p-5">
            <div className="flex items-start justify-between gap-3">
              <div>
                <div className="font-medium text-text-primary">{request.title}</div>
                <div className="mt-1 text-xs text-text-secondary">
                  {formatRequestId(request.id)} · {request.eventName}
                </div>
              </div>
              <RequestStatusBadge status={request.status} />
            </div>

            <div className="space-y-2 text-xs">
              <div className="flex justify-between gap-3">
                <span className="text-text-hint">Requestor</span>
                <span className="font-medium text-text-primary">
                  {request.requestorName} · {request.department}
                </span>
              </div>
              <div className="flex justify-between gap-3">
                <span className="text-text-hint">Type</span>
                <span className="font-medium text-text-primary">{request.sponsorshipTypeName}</span>
              </div>
              <div className="flex justify-between gap-3">
                <span className="text-text-hint">Event date</span>
                <span className="font-medium text-text-primary">
                  {formatDate(request.eventDate)}
                </span>
              </div>
              <div className="flex justify-between gap-3">
                <span className="text-text-hint">Created</span>
                <span className="font-medium text-text-primary">
                  {formatDate(request.createdAt)}
                </span>
              </div>
            </div>

            <div className="mt-auto flex items-center justify-between border-t border-border pt-4">
              <div className="font-semibold text-text-primary">
                {formatCurrency(request.requestedAmount)}
              </div>
              <Button asChild variant="outline" size="sm">
                <Link to={`/dashboard/requests/${request.id}`}>
                  <Eye className="h-4 w-4" aria-hidden="true" />
                  View
                </Link>
              </Button>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
