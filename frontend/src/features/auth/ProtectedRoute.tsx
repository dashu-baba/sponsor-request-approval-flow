import { Navigate, Outlet, useLocation } from 'react-router-dom'

import { LoadingState } from '@/components/states/query-states'
import { useAuth } from '@/features/auth/use-auth'
import type { Role } from '@/lib/roles'

interface ProtectedRouteProps {
  allowedRoles?: Role[]
}

export function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const { status, user } = useAuth()
  const location = useLocation()

  if (status === 'loading') {
    return (
      <div className="flex min-h-screen items-center justify-center bg-page p-8">
        <div className="w-full max-w-3xl">
          <LoadingState
            title="Checking session"
            description="Restoring your session…"
            metricCount={0}
            tableRows={0}
          />
        </div>
      </div>
    )
  }

  if (status !== 'authenticated' || !user) {
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
  }

  if (allowedRoles && !allowedRoles.includes(user.role)) {
    return <Navigate to="/dashboard" replace />
  }

  return <Outlet />
}

export function GuestRoute() {
  const { status } = useAuth()
  const location = useLocation()
  const from =
    typeof location.state === 'object' &&
    location.state !== null &&
    'from' in location.state &&
    typeof location.state.from === 'string'
      ? location.state.from
      : '/dashboard'

  if (status === 'loading') {
    return (
      <div className="flex min-h-screen items-center justify-center bg-page">
        <LoadingState
          title="Loading"
          description="Preparing sign-in…"
          metricCount={0}
          tableRows={0}
        />
      </div>
    )
  }

  if (status === 'authenticated') {
    return <Navigate to={from} replace />
  }

  return <Outlet />
}

export function RoleRedirect() {
  const { status, user } = useAuth()

  if (status === 'loading') {
    return null
  }

  if (status === 'authenticated' && user) {
    return <Navigate to="/dashboard" replace />
  }

  return <Navigate to="/login" replace />
}
