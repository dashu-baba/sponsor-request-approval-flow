import { useQuery } from '@tanstack/react-query'
import {
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Clock3,
  FileText,
  LayoutGrid,
  LayoutList,
  Pencil,
  Plus,
  Search,
  XCircle,
} from 'lucide-react'
import { useMemo, useState, type ReactNode } from 'react'
import { Link, useSearchParams } from 'react-router-dom'

import { PageHeader } from '@/components/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/states/query-states'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { CancelRequestModal } from '@/features/requests/CancelRequestModal'
import { RequestFormModal } from '@/features/requests/RequestFormModal'
import { RequestStatusBadge } from '@/features/requests/RequestStatusBadge'
import { getDashboardHeading, type DashboardStatusFilter } from '@/features/auth/role-nav'
import { ApiError } from '@/lib/api/api-error'
import { getRequestSummary, listRequests } from '@/lib/api/requests-api'
import { listSponsorshipTypes } from '@/lib/api/sponsorship-types-api'
import { formatCurrency, formatDate, formatRequestId } from '@/lib/format'
import { queryKeys } from '@/lib/query-client'
import { canCancelRequest, canEditRequest } from '@/lib/request-status'
import { Roles } from '@/lib/roles'
import type { RequestListItem, RequestStatus } from '@/lib/schemas/requests'
import { cn } from '@/lib/utils'

const PAGE_SIZE = 20

type ViewMode = 'list' | 'grid'

type StatusFilterValue = '' | RequestStatus

function parseUrlStatusFilter(value: string | null): DashboardStatusFilter {
  if (!value) return 'overview'
  if (value === 'all') return 'all'
  if (
    value === 'Draft' ||
    value === 'PendingManagerApproval' ||
    value === 'Approved' ||
    value === 'Rejected'
  ) {
    return value
  }
  return 'overview'
}

function urlFilterToStatusValue(filter: DashboardStatusFilter): StatusFilterValue {
  if (filter === 'overview' || filter === 'all') return ''
  return filter
}

const statusFilterOptions: { value: StatusFilterValue; label: string }[] = [
  { value: '', label: 'All statuses' },
  { value: 'Draft', label: 'Draft' },
  { value: 'PendingManagerApproval', label: 'Pending approval' },
  { value: 'PendingFinanceReview', label: 'Pending finance review' },
  { value: 'Approved', label: 'Approved' },
  { value: 'Rejected', label: 'Rejected' },
  { value: 'Cancelled', label: 'Cancelled' },
]

function MetricCard({
  icon,
  iconClassName,
  value,
  label,
  valueClassName,
  badge,
}: {
  icon: ReactNode
  iconClassName: string
  value: number
  label: string
  valueClassName?: string
  badge?: string
}) {
  return (
    <Card className="transition-shadow hover:shadow-[0_2px_12px_rgba(74,63,200,0.08)]">
      <CardContent className="flex flex-col gap-2.5 p-5">
        <div className="flex items-center justify-between">
          <span
            className={cn('flex h-9 w-9 items-center justify-center rounded-[9px]', iconClassName)}
          >
            {icon}
          </span>
          {badge ? (
            <span className="rounded-full bg-warning-bg px-2 py-0.5 text-[11px] font-medium text-warning">
              {badge}
            </span>
          ) : null}
        </div>
        <div
          className={cn('text-[28px] font-semibold leading-none tracking-tight', valueClassName)}
        >
          {value}
        </div>
        <p className="text-xs text-text-secondary">{label}</p>
      </CardContent>
    </Card>
  )
}

function PaginationFooter({
  page,
  totalPages,
  totalCount,
  onPageChange,
}: {
  page: number
  totalPages: number
  totalCount: number
  onPageChange: (page: number) => void
}) {
  return (
    <div className="flex items-center justify-between border-t border-border px-4 py-3.5 text-xs text-text-secondary">
      <span>
        Showing page {page} of {totalPages} ({totalCount} total)
      </span>
      <div className="flex gap-1">
        <Button
          type="button"
          variant="outline"
          size="icon"
          disabled={page <= 1}
          onClick={() => onPageChange(Math.max(1, page - 1))}
          aria-label="Previous page"
        >
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <Button
          type="button"
          variant="outline"
          size="icon"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
          aria-label="Next page"
        >
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>
    </div>
  )
}

interface RowActionsProps {
  request: RequestListItem
  onEdit: (request: RequestListItem) => void
  onCancel: (request: RequestListItem) => void
}

function RequestRowActions({ request, onEdit, onCancel }: RowActionsProps) {
  const showEdit = canEditRequest(request.status)
  const showCancel = canCancelRequest(request.status)

  return (
    <div className="flex flex-wrap items-center gap-1.5">
      {showEdit ? (
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={(event) => {
            event.preventDefault()
            event.stopPropagation()
            onEdit(request)
          }}
        >
          <Pencil className="h-3.5 w-3.5" aria-hidden="true" />
          Edit
        </Button>
      ) : null}
      {showCancel ? (
        <Button
          type="button"
          variant="destructive"
          size="sm"
          onClick={(event) => {
            event.preventDefault()
            event.stopPropagation()
            onCancel(request)
          }}
        >
          Cancel
        </Button>
      ) : null}
      <Button type="button" variant="outline" size="sm" asChild>
        <Link to={`/requests/${request.id}`} onClick={(event) => event.stopPropagation()}>
          View
        </Link>
      </Button>
    </div>
  )
}

export function RequestorDashboard() {
  const [searchParams, setSearchParams] = useSearchParams()
  const urlStatusFilter = parseUrlStatusFilter(searchParams.get('status'))
  const statusFilter = urlFilterToStatusValue(urlStatusFilter)
  const heading = getDashboardHeading(Roles.Requestor, urlStatusFilter)

  const [page, setPage] = useState(1)
  const [viewMode, setViewMode] = useState<ViewMode>('list')
  const [search, setSearch] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editingRequestId, setEditingRequestId] = useState<number | undefined>()
  const [cancellingRequest, setCancellingRequest] = useState<RequestListItem | null>(null)

  function updateStatusFilter(value: StatusFilterValue) {
    setPage(1)
    const next = new URLSearchParams(searchParams)
    if (!value) {
      next.set('status', 'all')
    } else {
      next.set('status', value)
    }
    setSearchParams(next)
  }

  const summaryQuery = useQuery({
    queryKey: queryKeys.requests.summary,
    queryFn: getRequestSummary,
  })

  const listQuery = useQuery({
    queryKey: queryKeys.requests.list(page, PAGE_SIZE),
    queryFn: () => listRequests({ page, pageSize: PAGE_SIZE }),
  })

  const sponsorshipTypesQuery = useQuery({
    queryKey: queryKeys.sponsorshipTypes.list,
    queryFn: listSponsorshipTypes,
  })

  const sponsorshipTypeNames = useMemo(
    () =>
      (sponsorshipTypesQuery.data ?? [])
        .filter((type) => type.isActive)
        .map((type) => type.name)
        .sort(),
    [sponsorshipTypesQuery.data],
  )

  const filteredItems = useMemo(() => {
    const items = listQuery.data?.items ?? []
    const query = search.trim().toLowerCase()
    return items.filter((item) => {
      if (statusFilter && item.status !== statusFilter) return false
      if (typeFilter && item.sponsorshipTypeName !== typeFilter) return false
      if (!query) return true

      return (
        item.title.toLowerCase().includes(query) ||
        item.eventName.toLowerCase().includes(query) ||
        item.department.toLowerCase().includes(query) ||
        String(item.id).toLowerCase().includes(query) ||
        formatRequestId(item.id).toLowerCase().includes(query)
      )
    })
  }, [listQuery.data?.items, search, statusFilter, typeFilter])

  const isLoading = summaryQuery.isLoading || listQuery.isLoading
  const isError = summaryQuery.isError || listQuery.isError
  const error =
    summaryQuery.error instanceof ApiError
      ? summaryQuery.error
      : listQuery.error instanceof ApiError
        ? listQuery.error
        : (summaryQuery.error ?? listQuery.error)

  if (isLoading) {
    return <LoadingState title="Loading dashboard" description="Fetching your requests…" />
  }

  if (isError) {
    const message =
      error instanceof ApiError
        ? error.message
        : error instanceof Error
          ? error.message
          : 'Unable to load dashboard'

    return (
      <ErrorState
        message={message}
        onRetry={() => {
          void summaryQuery.refetch()
          void listQuery.refetch()
        }}
      />
    )
  }

  const summary = summaryQuery.data
  const paged = listQuery.data
  const pendingCount = (summary?.pendingManagerApproval ?? 0) + (summary?.pendingFinanceReview ?? 0)
  const totalPages = paged ? Math.max(1, Math.ceil(paged.totalCount / PAGE_SIZE)) : 1

  function openCreateModal() {
    setEditingRequestId(undefined)
    setFormOpen(true)
  }

  function openEditModal(request: RequestListItem) {
    setEditingRequestId(request.id)
    setFormOpen(true)
  }

  function closeFormModal() {
    setFormOpen(false)
    setEditingRequestId(undefined)
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={heading.title}
        subtitle={heading.subtitle}
        actions={
          <Button type="button" onClick={openCreateModal}>
            <Plus className="h-4 w-4" aria-hidden="true" />
            New request
          </Button>
        }
      />

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <MetricCard
          icon={<FileText className="h-[18px] w-[18px] text-brand" aria-hidden="true" />}
          iconClassName="bg-brand-light"
          value={summary?.total ?? 0}
          label="Total requests"
          valueClassName="text-brand"
        />
        <MetricCard
          icon={<Pencil className="h-[18px] w-[18px] text-text-secondary" aria-hidden="true" />}
          iconClassName="bg-gray-bg"
          value={summary?.draft ?? 0}
          label="Drafts"
          valueClassName="text-text-primary"
        />
        <MetricCard
          icon={<Clock3 className="h-[18px] w-[18px] text-warning" aria-hidden="true" />}
          iconClassName="bg-warning-bg"
          value={pendingCount}
          label="Pending approval"
          valueClassName="text-warning"
          badge={pendingCount > 0 ? `${pendingCount} active` : undefined}
        />
        <MetricCard
          icon={<CheckCircle2 className="h-[18px] w-[18px] text-success" aria-hidden="true" />}
          iconClassName="bg-success-bg"
          value={summary?.approved ?? 0}
          label="Approved"
          valueClassName="text-success"
        />
        <MetricCard
          icon={<XCircle className="h-[18px] w-[18px] text-danger" aria-hidden="true" />}
          iconClassName="bg-danger-bg"
          value={summary?.rejected ?? 0}
          label="Rejected"
          valueClassName="text-danger"
        />
      </div>

      <div className="space-y-3">
        <div className="relative">
          <Search
            className="pointer-events-none absolute top-1/2 left-3 h-3.5 w-3.5 -translate-y-1/2 text-text-hint"
            aria-hidden="true"
          />
          <Input
            type="search"
            value={search}
            onChange={(event) => {
              setSearch(event.target.value)
              setPage(1)
            }}
            placeholder="Search requests by title, organisation, or ID…"
            className="bg-page pl-9"
            aria-label="Search requests"
          />
        </div>

        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2">
            <select
              value={statusFilter}
              onChange={(event) => updateStatusFilter(event.target.value as StatusFilterValue)}
              className="h-9 rounded-[8px] border border-border bg-surface px-2.5 text-xs text-text-primary"
              aria-label="Filter by status"
            >
              {statusFilterOptions.map((option) => (
                <option key={option.label} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
            <select
              value={typeFilter}
              onChange={(event) => {
                setTypeFilter(event.target.value)
                setPage(1)
              }}
              className="h-9 rounded-[8px] border border-border bg-surface px-2.5 text-xs text-text-primary"
              aria-label="Filter by sponsorship type"
            >
              <option value="">All types</option>
              {sponsorshipTypeNames.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
            <span className="text-xs text-text-hint">
              {filteredItems.length !== (paged?.items.length ?? 0)
                ? `${filteredItems.length} of ${paged?.items.length ?? 0} on this page — search and filters apply to the current page only`
                : `${paged?.totalCount ?? 0} requests — search and filters apply to the current page only`}
            </span>
          </div>

          <div
            className="flex overflow-hidden rounded-[8px] border border-border bg-surface"
            role="group"
            aria-label="View toggle"
          >
            <button
              type="button"
              className={cn(
                'flex items-center px-2.5 py-2 text-text-hint transition-colors',
                viewMode === 'list' && 'bg-brand-light text-brand',
              )}
              onClick={() => setViewMode('list')}
              aria-label="List view"
              aria-pressed={viewMode === 'list'}
            >
              <LayoutList className="h-4 w-4" aria-hidden="true" />
            </button>
            <button
              type="button"
              className={cn(
                'flex items-center px-2.5 py-2 text-text-hint transition-colors',
                viewMode === 'grid' && 'bg-brand-light text-brand',
              )}
              onClick={() => setViewMode('grid')}
              aria-label="Grid view"
              aria-pressed={viewMode === 'grid'}
            >
              <LayoutGrid className="h-4 w-4" aria-hidden="true" />
            </button>
          </div>
        </div>
      </div>

      {filteredItems.length === 0 ? (
        <EmptyState
          title="No requests found"
          description="Try adjusting your filters or create a new request."
          action={
            <Button type="button" onClick={openCreateModal}>
              <Plus className="h-4 w-4" aria-hidden="true" />
              New request
            </Button>
          }
        />
      ) : viewMode === 'list' ? (
        <Card className="overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-left">
              <thead>
                <tr className="border-b border-border bg-page text-[11px] font-semibold tracking-wide text-text-hint uppercase">
                  <th className="px-4 py-3">ID</th>
                  <th className="px-4 py-3">Title</th>
                  <th className="px-4 py-3">Department</th>
                  <th className="px-4 py-3">Type</th>
                  <th className="px-4 py-3">Amount</th>
                  <th className="px-4 py-3">Event Date</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredItems.map((request) => (
                  <tr
                    key={request.id}
                    className="border-b border-border transition-colors last:border-b-0 hover:bg-[#FAFAFE]"
                  >
                    <td className="px-4 py-3.5 font-mono text-[11.5px] text-text-hint">
                      {formatRequestId(request.id)}
                    </td>
                    <td className="px-4 py-3.5">
                      <Link
                        to={`/requests/${request.id}`}
                        className="block font-medium text-text-primary hover:text-brand"
                      >
                        {request.title}
                      </Link>
                      <p className="mt-0.5 text-[11.5px] text-text-hint">{request.eventName}</p>
                    </td>
                    <td className="px-4 py-3.5 text-[13px]">{request.department}</td>
                    <td className="px-4 py-3.5 text-[13px]">{request.sponsorshipTypeName}</td>
                    <td className="px-4 py-3.5 font-mono text-[13px] font-medium">
                      {formatCurrency(request.requestedAmount)}
                    </td>
                    <td className="px-4 py-3.5 text-[13px]">{formatDate(request.eventDate)}</td>
                    <td className="px-4 py-3.5">
                      <RequestStatusBadge status={request.status} />
                    </td>
                    <td className="px-4 py-3.5">
                      <RequestRowActions
                        request={request}
                        onEdit={openEditModal}
                        onCancel={setCancellingRequest}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <PaginationFooter
            page={page}
            totalPages={totalPages}
            totalCount={paged?.totalCount ?? 0}
            onPageChange={setPage}
          />
        </Card>
      ) : (
        <Card className="overflow-hidden">
          <div className="grid gap-4 p-4 sm:grid-cols-2 xl:grid-cols-3">
            {filteredItems.map((request) => (
              <Card
                key={request.id}
                className="transition-all hover:-translate-y-px hover:border-brand-mid hover:shadow-[0_4px_20px_rgba(74,63,200,0.1)]"
              >
                <CardContent className="flex h-full flex-col gap-3.5 p-5">
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0">
                      <Link
                        to={`/requests/${request.id}`}
                        className="text-sm font-medium text-text-primary hover:text-brand"
                      >
                        {request.title}
                      </Link>
                      <p className="mt-0.5 text-xs text-text-secondary">
                        {formatRequestId(request.id)} · {request.department}
                      </p>
                    </div>
                    <RequestStatusBadge status={request.status} />
                  </div>
                  <div className="space-y-1.5 text-xs">
                    <div className="flex justify-between gap-2">
                      <span className="text-text-hint">Type</span>
                      <span className="font-medium text-text-primary">
                        {request.sponsorshipTypeName}
                      </span>
                    </div>
                    <div className="flex justify-between gap-2">
                      <span className="text-text-hint">Organisation</span>
                      <span className="max-w-[160px] truncate text-right font-medium text-text-primary">
                        {request.eventName}
                      </span>
                    </div>
                    <div className="flex justify-between gap-2">
                      <span className="text-text-hint">Event date</span>
                      <span className="font-medium text-text-primary">
                        {formatDate(request.eventDate)}
                      </span>
                    </div>
                  </div>
                  <div className="mt-auto flex items-center justify-between border-t border-border pt-3">
                    <span className="font-mono text-[15px] font-semibold text-text-primary">
                      {formatCurrency(request.requestedAmount)}
                    </span>
                    <RequestRowActions
                      request={request}
                      onEdit={openEditModal}
                      onCancel={setCancellingRequest}
                    />
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
          <PaginationFooter
            page={page}
            totalPages={totalPages}
            totalCount={paged?.totalCount ?? 0}
            onPageChange={setPage}
          />
        </Card>
      )}

      <RequestFormModal
        open={formOpen}
        onClose={closeFormModal}
        requestId={editingRequestId}
        onSuccess={() => {
          void summaryQuery.refetch()
          void listQuery.refetch()
        }}
      />

      {cancellingRequest ? (
        <CancelRequestModal
          open
          onClose={() => setCancellingRequest(null)}
          requestId={cancellingRequest.id}
          requestTitle={cancellingRequest.title}
          onSuccess={() => {
            void summaryQuery.refetch()
            void listQuery.refetch()
          }}
        />
      ) : null}
    </div>
  )
}
