import { useQuery } from '@tanstack/react-query'
import { Eye } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useState } from 'react'

import { PageHeader } from '@/components/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/states/query-states'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { listAdminRequests } from '@/features/admin/api/admin-api'
import { formatDate, formatMoney, formatStatus, getErrorMessage } from '@/features/admin/format'
import type { RequestStatus } from '@/features/admin/types'

const pageSize = 10
const statusOptions: Exclude<RequestStatus, 'Draft'>[] = [
  'PendingManagerApproval',
  'PendingFinanceReview',
  'Approved',
  'Rejected',
  'Cancelled',
]

export function AdminRequestsPage() {
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState<Exclude<RequestStatus, 'Draft'> | ''>('')

  const query = useQuery({
    queryKey: ['admin-requests', page, status],
    queryFn: () =>
      listAdminRequests({
        page,
        pageSize,
        status: status || undefined,
      }),
  })

  const totalPages = query.data ? Math.max(1, Math.ceil(query.data.totalCount / pageSize)) : 1

  if (query.isLoading) {
    return <LoadingState title="Loading submitted requests" metricCount={0} tableRows={8} />
  }

  if (query.isError) {
    return (
      <ErrorState
        message={getErrorMessage(query.error)}
        onRetry={() => {
          void query.refetch()
        }}
      />
    )
  }

  const data = query.data
  if (!data) {
    return <EmptyState title="No submitted requests" />
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="All submitted requests"
        subtitle="Submitted requests only. Private drafts remain visible only to their requestor."
      />

      <Alert variant="info">
        <AlertTitle>No draft leakage</AlertTitle>
        <AlertDescription>
          System admins can inspect submitted workflows, but cannot approve or reject requests.
        </AlertDescription>
      </Alert>

      <Card>
        <CardContent className="space-y-5 p-5">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
            <label className="flex max-w-xs flex-col gap-1 text-[13px] font-medium text-text-primary">
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
                    {formatStatus(option)}
                  </option>
                ))}
              </select>
            </label>

            <div className="text-[13px] text-text-secondary">
              {data.totalCount} request{data.totalCount === 1 ? '' : 's'}
            </div>
          </div>

          {data.items.length === 0 ? (
            <EmptyState
              title="No submitted requests"
              description="Requests appear here after requestors submit them."
            />
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[780px] border-collapse text-left text-[13px]">
                <thead>
                  <tr className="border-b border-border text-text-secondary">
                    <th className="py-3 pr-4 font-medium">Request</th>
                    <th className="py-3 pr-4 font-medium">Status</th>
                    <th className="py-3 pr-4 font-medium">Event</th>
                    <th className="py-3 pr-4 font-medium">Type</th>
                    <th className="py-3 pr-4 text-right font-medium">Amount</th>
                    <th className="py-3 pl-4 text-right font-medium">History</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((request) => (
                    <tr key={request.id} className="border-b border-border last:border-0">
                      <td className="py-3 pr-4">
                        <div className="font-medium text-text-primary">{request.title}</div>
                        <div className="text-xs text-text-secondary">
                          Created {formatDate(request.createdAt)}
                        </div>
                      </td>
                      <td className="py-3 pr-4">
                        <Badge>{formatStatus(request.status)}</Badge>
                      </td>
                      <td className="py-3 pr-4">
                        <div>{request.eventName}</div>
                        <div className="text-xs text-text-secondary">
                          {formatDate(request.eventDate)}
                        </div>
                      </td>
                      <td className="py-3 pr-4">{request.sponsorshipTypeName}</td>
                      <td className="py-3 pr-4 text-right font-medium">
                        {formatMoney(request.requestedAmount)}
                      </td>
                      <td className="py-3 pl-4 text-right">
                        <Button asChild variant="outline" size="sm">
                          <Link to={`/admin/requests/${request.id}`}>
                            <Eye className="h-4 w-4" aria-hidden="true" />
                            View
                          </Link>
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
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
