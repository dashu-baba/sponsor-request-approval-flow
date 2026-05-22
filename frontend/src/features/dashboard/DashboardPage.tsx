import { lazy, Suspense } from 'react'
import { useSearchParams } from 'react-router-dom'

import { PageHeader } from '@/components/PageHeader'
import { LoadingState } from '@/components/states/query-states'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { ApproverDashboard } from '@/features/approvals/ApproverDashboard'
import { useCurrentUser } from '@/features/auth/use-auth'
import {
  getDashboardHeading,
  getDashboardMetricCount,
  type DashboardStatusFilter,
} from '@/features/auth/role-nav'
import { Roles } from '@/lib/roles'

const AdminRequestsPage = lazy(() =>
  import('@/features/admin/AdminRequestsPage').then((module) => ({
    default: module.AdminRequestsPage,
  })),
)

function parseStatusFilter(value: string | null): DashboardStatusFilter {
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

function RequestorDashboardPlaceholder() {
  const user = useCurrentUser()
  const [searchParams] = useSearchParams()
  const statusFilter = parseStatusFilter(searchParams.get('status'))
  const heading = getDashboardHeading(user.role, statusFilter)
  const metricCount = getDashboardMetricCount(user.role)

  return (
    <div className="space-y-6">
      <PageHeader title={heading.title} subtitle={heading.subtitle} />

      <div
        className={`grid gap-4 sm:grid-cols-2 ${metricCount === 5 ? 'xl:grid-cols-5' : 'xl:grid-cols-4'}`}
      >
        {Array.from({ length: metricCount }).map((_, index) => (
          <Card key={index}>
            <CardContent className="space-y-2 p-5">
              <Skeleton className="h-4 w-24" />
              <div className="text-[28px] font-semibold text-text-primary">—</div>
              <p className="text-xs text-text-hint">Metrics arrive in T3.2</p>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card>
        <CardContent className="space-y-3 p-8 text-center">
          <h2 className="text-base font-semibold">Request list placeholder</h2>
          <p className="text-[13px] text-text-secondary">
            {statusFilter === 'overview' || statusFilter === 'all'
              ? 'Your dashboard request table will render here in T3.2.'
              : `Filtered by status: ${statusFilter}. Same dashboard view — sidebar sets ?status= for T3.2.`}
          </p>
        </CardContent>
      </Card>
    </div>
  )
}

export function DashboardPage() {
  const user = useCurrentUser()

  if (user.role === Roles.Manager || user.role === Roles.FinanceAdmin) {
    return <ApproverDashboard />
  }

  if (user.role === Roles.SystemAdmin) {
    return (
      <Suspense
        fallback={<LoadingState title="Loading submitted requests" metricCount={4} tableRows={8} />}
      >
        <AdminRequestsPage />
      </Suspense>
    )
  }

  return <RequestorDashboardPlaceholder />
}
