import { lazy, Suspense } from 'react'
import { LoadingState } from '@/components/states/query-states'
import { ApproverDashboard } from '@/features/approvals/ApproverDashboard'
import { useCurrentUser } from '@/features/auth/use-auth'
import { Roles } from '@/lib/roles'
import { RequestorDashboard } from '@/features/requests/RequestorDashboard'

const AdminRequestsPage = lazy(() =>
  import('@/features/admin/AdminRequestsPage').then((module) => ({
    default: module.AdminRequestsPage,
  })),
)

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

  return <RequestorDashboard />
}
