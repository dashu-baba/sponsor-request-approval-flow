import { useQuery } from '@tanstack/react-query'
import { ArrowLeft } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'

import { PageHeader } from '@/components/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/states/query-states'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { getRequestDetail, getRequestHistory } from '@/features/admin/api/admin-api'
import { formatDate, formatMoney, formatStatus, getErrorMessage } from '@/features/admin/format'

export function AdminRequestDetailPage() {
  const { id } = useParams<{ id: string }>()

  const detailQuery = useQuery({
    queryKey: ['admin-request-detail', id],
    queryFn: () => getRequestDetail(id ?? ''),
    enabled: Boolean(id),
  })
  const historyQuery = useQuery({
    queryKey: ['admin-request-history', id],
    queryFn: () => getRequestHistory(id ?? ''),
    enabled: Boolean(id),
  })

  if (detailQuery.isLoading || historyQuery.isLoading) {
    return <LoadingState title="Loading request history" metricCount={0} tableRows={6} />
  }

  if (detailQuery.isError || historyQuery.isError) {
    return (
      <ErrorState
        message={getErrorMessage(detailQuery.error ?? historyQuery.error)}
        onRetry={() => {
          void detailQuery.refetch()
          void historyQuery.refetch()
        }}
      />
    )
  }

  if (!detailQuery.data) {
    return <EmptyState title="Request not found" />
  }

  const request = detailQuery.data
  const history = historyQuery.data ?? []

  return (
    <div className="space-y-6">
      <Button asChild variant="ghost" size="sm" className="w-fit">
        <Link to="/dashboard">
          <ArrowLeft className="h-4 w-4" aria-hidden="true" />
          Back
        </Link>
      </Button>

      <PageHeader
        title={request.title}
        subtitle={`${request.eventName} · ${formatDate(request.eventDate)}`}
      />

      <Alert variant="info">
        <AlertTitle>Read-only audit view</AlertTitle>
        <AlertDescription>
          System admins can inspect workflow history, but approval actions stay with manager and
          finance roles.
        </AlertDescription>
      </Alert>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_420px]">
        <Card>
          <CardHeader>
            <CardTitle>Request details</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4 p-5 pt-0 sm:grid-cols-2">
            <Detail label="Status">
              <Badge>{formatStatus(request.status)}</Badge>
            </Detail>
            <Detail label="Requested amount">{formatMoney(request.requestedAmount)}</Detail>
            <Detail label="Requestor">{request.requestorName}</Detail>
            <Detail label="Department">{request.department}</Detail>
            <Detail label="Sponsorship type">{request.sponsorshipTypeName}</Detail>
            <Detail label="Created">{formatDate(request.createdAt)}</Detail>
            <Detail label="Purpose" wide>
              {request.purpose}
            </Detail>
            <Detail label="Expected benefit" wide>
              {request.expectedBenefit ?? 'Not provided'}
            </Detail>
            <Detail label="Remarks" wide>
              {request.remarks ?? 'No remarks recorded'}
            </Detail>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Workflow history</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4 p-5 pt-0">
            {history.length === 0 ? (
              <p className="text-[13px] text-text-secondary">No workflow transitions recorded.</p>
            ) : (
              history.map((item) => (
                <div key={item.id} className="border-l-2 border-brand-light pl-4">
                  <div className="text-[13px] font-medium text-text-primary">
                    {formatStatus(item.fromStatus)} → {formatStatus(item.toStatus)}
                  </div>
                  <div className="mt-1 text-xs text-text-secondary">
                    {item.actorName} · {formatDate(item.occurredAt)}
                  </div>
                  {item.remarks ? (
                    <p className="mt-2 rounded-[8px] bg-page p-3 text-[13px] text-text-secondary">
                      {item.remarks}
                    </p>
                  ) : null}
                </div>
              ))
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

function Detail({
  label,
  children,
  wide = false,
}: {
  label: string
  children: ReactNode
  wide?: boolean
}) {
  return (
    <div className={wide ? 'sm:col-span-2' : undefined}>
      <div className="mb-1 text-xs font-medium text-text-hint">{label}</div>
      <div className="text-[13px] text-text-primary">{children}</div>
    </div>
  )
}
