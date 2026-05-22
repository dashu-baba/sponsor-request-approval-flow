import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

import { RequestStatusBadge } from '@/features/requests/RequestStatusBadge'
import { formatCurrency, formatDate, formatRequestId } from '@/lib/format'
import type { RequestStatus } from '@/lib/schemas/requests'

/** Minimum shape every dashboard's list item satisfies. */
export interface RequestTableRow {
  id: number
  title: string
  requestorName: string
  department: string
  sponsorshipTypeName: string
  requestedAmount: number
  eventDate: string
  eventName: string
  status: RequestStatus
}

interface RequestsTableProps<T extends RequestTableRow> {
  requests: T[]
  /** Hide the Requestor column (the requestor's own dashboard). Defaults to shown. */
  showRequestor?: boolean
  /** Where the title links to — differs per role (e.g. `/requests/:id` vs `/dashboard/requests/:id`). */
  detailHref: (request: T) => string
  /** Per-row actions cell (approve/reject, edit/cancel, view…) — supplied by each dashboard. */
  renderActions: (request: T) => ReactNode
}

/**
 * Shared requests table used by the requestor, approver, and admin dashboards.
 * The only per-dashboard differences are the Requestor column, the detail link,
 * and the actions cell — everything else (columns, order, status colors,
 * currency/id formatting) is identical by construction.
 */
export function RequestsTable<T extends RequestTableRow>({
  requests,
  showRequestor = true,
  detailHref,
  renderActions,
}: RequestsTableProps<T>) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full border-collapse text-left">
        <thead>
          <tr className="border-b border-border bg-page text-[11px] font-semibold tracking-wide text-text-hint uppercase">
            <th className="px-4 py-3">ID</th>
            <th className="px-4 py-3">Title</th>
            {showRequestor ? <th className="px-4 py-3">Requestor</th> : null}
            <th className="px-4 py-3">Department</th>
            <th className="px-4 py-3">Type</th>
            <th className="px-4 py-3">Amount</th>
            <th className="px-4 py-3">Event Date</th>
            <th className="px-4 py-3">Status</th>
            <th className="px-4 py-3">Actions</th>
          </tr>
        </thead>
        <tbody>
          {requests.map((request) => (
            <tr
              key={request.id}
              className="border-b border-border transition-colors last:border-b-0 hover:bg-[#FAFAFE]"
            >
              <td className="px-4 py-3.5 font-mono text-[11.5px] text-text-hint">
                {formatRequestId(request.id)}
              </td>
              <td className="px-4 py-3.5">
                <Link
                  to={detailHref(request)}
                  className="block font-medium text-text-primary hover:text-brand"
                >
                  {request.title}
                </Link>
                <p className="mt-0.5 text-[11.5px] text-text-hint">{request.eventName}</p>
              </td>
              {showRequestor ? (
                <td className="px-4 py-3.5 text-[13px]">{request.requestorName}</td>
              ) : null}
              <td className="px-4 py-3.5 text-[13px]">{request.department}</td>
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
