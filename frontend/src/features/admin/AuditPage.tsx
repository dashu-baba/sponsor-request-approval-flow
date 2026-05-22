import { useQuery } from '@tanstack/react-query'
import { ShieldCheck } from 'lucide-react'
import { useState } from 'react'

import { PageHeader } from '@/components/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/states/query-states'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { getErrorMessage } from '@/features/admin/format'
import type { AuditEvent } from '@/features/admin/types'
import { listAuditEvents } from '@/lib/api/audit-api'
import { queryKeys } from '@/lib/query-client'

const pageSize = 20

const categoryOptions = ['', 'Request', 'Attachment', 'SponsorshipType', 'User', 'Auth']

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  }).format(new Date(value))
}

export function AuditPage() {
  const [page, setPage] = useState(1)
  const [action, setAction] = useState('')
  const [category, setCategory] = useState('')
  const [requestId, setRequestId] = useState('')

  const query = useQuery({
    queryKey: queryKeys.audit.list(page, action, category, requestId),
    queryFn: () =>
      listAuditEvents({
        page,
        pageSize,
        action: action || undefined,
        category: category || undefined,
        requestId: requestId || undefined,
      }),
  })

  const totalPages = query.data ? Math.max(1, Math.ceil(query.data.totalCount / pageSize)) : 1

  return (
    <div className="space-y-6">
      <PageHeader
        title="Audit trail"
        description="SystemAdmin operations log. Separate from request workflow history."
        icon={ShieldCheck}
      />

      <Card>
        <CardContent className="space-y-4 pt-6">
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <Label htmlFor="audit-action">Action</Label>
              <Input
                id="audit-action"
                placeholder="e.g. request.created"
                value={action}
                onChange={(event) => {
                  setAction(event.target.value)
                  setPage(1)
                }}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="audit-category">Category</Label>
              <select
                id="audit-category"
                className="border-input bg-background ring-offset-background focus-visible:ring-ring flex h-10 w-full rounded-md border px-3 py-2 text-sm focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none"
                value={category}
                onChange={(event) => {
                  setCategory(event.target.value)
                  setPage(1)
                }}
              >
                {categoryOptions.map((option) => (
                  <option key={option || 'all'} value={option}>
                    {option || 'All categories'}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="audit-request-id">Request ID</Label>
              <Input
                id="audit-request-id"
                placeholder="Filter by requestId metadata"
                value={requestId}
                onChange={(event) => {
                  setRequestId(event.target.value)
                  setPage(1)
                }}
              />
            </div>
          </div>

          {query.isLoading ? (
            <LoadingState title="Loading audit events" metricCount={0} tableRows={6} />
          ) : null}
          {query.isError ? (
            <ErrorState
              title="Could not load audit events"
              message={getErrorMessage(query.error)}
            />
          ) : null}

          {query.isSuccess && query.data.items.length === 0 ? (
            <EmptyState title="No audit events" description="Try adjusting your filters." />
          ) : null}

          {query.isSuccess && query.data.items.length > 0 ? (
            <>
              <div className="overflow-x-auto">
                <table className="w-full min-w-[960px] text-left text-sm">
                  <thead>
                    <tr className="border-b text-muted-foreground">
                      <th className="px-3 py-2 font-medium">When</th>
                      <th className="px-3 py-2 font-medium">Actor</th>
                      <th className="px-3 py-2 font-medium">Action</th>
                      <th className="px-3 py-2 font-medium">Category</th>
                      <th className="px-3 py-2 font-medium">Resource</th>
                      <th className="px-3 py-2 font-medium">Summary</th>
                    </tr>
                  </thead>
                  <tbody>
                    {query.data.items.map((event: AuditEvent) => (
                      <tr key={event.id} className="border-b last:border-0">
                        <td className="px-3 py-3 whitespace-nowrap">
                          {formatDateTime(event.occurredAt)}
                        </td>
                        <td className="px-3 py-3">{event.actorDisplayName}</td>
                        <td className="px-3 py-3 font-mono text-xs">{event.action}</td>
                        <td className="px-3 py-3">{event.category}</td>
                        <td className="px-3 py-3">
                          {event.resourceType} #{event.resourceId}
                        </td>
                        <td className="px-3 py-3 text-muted-foreground">{event.summary ?? '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="flex items-center justify-between">
                <p className="text-muted-foreground text-sm">
                  Page {query.data.page} of {totalPages} · {query.data.totalCount} events
                </p>
                <div className="flex gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={page <= 1}
                    onClick={() => setPage((current) => Math.max(1, current - 1))}
                  >
                    Previous
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={page >= totalPages}
                    onClick={() => setPage((current) => current + 1)}
                  >
                    Next
                  </Button>
                </div>
              </div>
            </>
          ) : null}
        </CardContent>
      </Card>
    </div>
  )
}
