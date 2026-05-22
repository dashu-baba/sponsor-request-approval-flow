import { ChevronLeft, ChevronRight } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { RequestStatusBadge } from '@/features/requests/RequestStatusBadge'
import { formatCurrency, formatDate, formatRequestId } from '@/lib/format'
import type { RequestStatus } from '@/lib/schemas/requests'

export type DashboardRequestRow = {
  id: number
  title: string
  eventName: string
  department: string
  requestorName: string
  sponsorshipTypeName: string
  requestedAmount: number
  eventDate: string
  status: RequestStatus
}

const emptyCell = '—'

export function PaginationFooter({
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

export function RequestDashboardTable({
  items,
  getDetailPath,
  renderActions,
  showRequestorColumn = true,
}: {
  items: DashboardRequestRow[]
  getDetailPath: (id: number) => string
  renderActions: (request: DashboardRequestRow) => ReactNode
  showRequestorColumn?: boolean
}) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full border-collapse text-left">
        <thead>
          <tr className="border-b border-border bg-page text-[11px] font-semibold tracking-wide text-text-hint uppercase">
            <th className="px-4 py-3">ID</th>
            <th className="px-4 py-3">Title</th>
            {showRequestorColumn ? <th className="px-4 py-3">Requestor</th> : null}
            <th className="px-4 py-3">Department</th>
            <th className="px-4 py-3">Type</th>
            <th className="px-4 py-3">Amount</th>
            <th className="px-4 py-3">Event Date</th>
            <th className="px-4 py-3">Status</th>
            <th className="px-4 py-3">Actions</th>
          </tr>
        </thead>
        <tbody>
          {items.map((request) => (
            <tr
              key={request.id}
              className="border-b border-border transition-colors last:border-b-0 hover:bg-[#FAFAFE]"
            >
              <td className="px-4 py-3.5 font-mono text-[11.5px] text-text-hint">
                {formatRequestId(request.id)}
              </td>
              <td className="px-4 py-3.5">
                <Link
                  to={getDetailPath(request.id)}
                  className="block font-medium text-text-primary hover:text-brand"
                >
                  {request.title}
                </Link>
                <p className="mt-0.5 text-[11.5px] text-text-hint">{request.eventName}</p>
              </td>
              {showRequestorColumn ? (
                <td className="px-4 py-3.5 text-[13px]">
                  {request.requestorName || emptyCell}
                </td>
              ) : null}
              <td className="px-4 py-3.5 text-[13px]">{request.department || emptyCell}</td>
              <td className="px-4 py-3.5 text-[13px]">{request.sponsorshipTypeName}</td>
              <td className="px-4 py-3.5 font-mono text-[13px] font-medium">
                {formatCurrency(request.requestedAmount)}
              </td>
              <td className="px-4 py-3.5 text-[13px]">{formatDate(request.eventDate)}</td>
              <td className="px-4 py-3.5">
                <RequestStatusBadge status={request.status} />
              </td>
              <td className="px-4 py-3.5">{renderActions(request)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export function RequestDashboardGrid({
  items,
  getDetailPath,
  renderActions,
  showRequestorColumn = true,
}: {
  items: DashboardRequestRow[]
  getDetailPath: (id: number) => string
  renderActions: (request: DashboardRequestRow) => ReactNode
  showRequestorColumn?: boolean
}) {
  return (
    <div className="grid gap-4 p-4 sm:grid-cols-2 xl:grid-cols-3">
      {items.map((request) => (
        <Card
          key={request.id}
          className="transition-all hover:-translate-y-px hover:border-brand-mid hover:shadow-[0_4px_20px_rgba(74,63,200,0.1)]"
        >
          <CardContent className="flex h-full flex-col gap-3.5 p-5">
            <div className="flex items-start justify-between gap-2">
              <div className="min-w-0">
                <Link
                  to={getDetailPath(request.id)}
                  className="text-sm font-medium text-text-primary hover:text-brand"
                >
                  {request.title}
                </Link>
                <p className="mt-0.5 text-xs text-text-secondary">
                  {formatRequestId(request.id)}
                  {request.department ? ` · ${request.department}` : ''}
                </p>
              </div>
              <RequestStatusBadge status={request.status} />
            </div>
            <div className="space-y-1.5 text-xs">
              {showRequestorColumn ? (
                <div className="flex justify-between gap-2">
                  <span className="text-text-hint">Requestor</span>
                  <span className="font-medium text-text-primary">
                    {request.requestorName || emptyCell}
                  </span>
                </div>
              ) : null}
              <div className="flex justify-between gap-2">
                <span className="text-text-hint">Department</span>
                <span className="font-medium text-text-primary">
                  {request.department || emptyCell}
                </span>
              </div>
              <div className="flex justify-between gap-2">
                <span className="text-text-hint">Type</span>
                <span className="font-medium text-text-primary">{request.sponsorshipTypeName}</span>
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
              {renderActions(request)}
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  )
}

export function RequestDashboardResults({
  items,
  viewMode,
  getDetailPath,
  renderActions,
  showRequestorColumn = true,
  page,
  totalPages,
  totalCount,
  onPageChange,
}: {
  items: DashboardRequestRow[]
  viewMode: 'list' | 'grid'
  getDetailPath: (id: number) => string
  renderActions: (request: DashboardRequestRow) => ReactNode
  showRequestorColumn?: boolean
  page: number
  totalPages: number
  totalCount: number
  onPageChange: (page: number) => void
}) {
  return (
    <Card className="overflow-hidden">
      {viewMode === 'list' ? (
        <RequestDashboardTable
          items={items}
          getDetailPath={getDetailPath}
          renderActions={renderActions}
          showRequestorColumn={showRequestorColumn}
        />
      ) : (
        <RequestDashboardGrid
          items={items}
          getDetailPath={getDetailPath}
          renderActions={renderActions}
          showRequestorColumn={showRequestorColumn}
        />
      )}
      <PaginationFooter
        page={page}
        totalPages={totalPages}
        totalCount={totalCount}
        onPageChange={onPageChange}
      />
    </Card>
  )
}
