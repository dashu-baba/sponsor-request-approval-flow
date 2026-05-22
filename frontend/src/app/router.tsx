import { createElement, lazy, Suspense, type ReactNode } from 'react'
import { createBrowserRouter, Navigate } from 'react-router-dom'

import { AppShell } from '@/app/layout/AppShell'
import { LoadingState } from '@/components/states/query-states'
import { ProfilePage } from '@/features/account/ProfilePage'
import { AdminRequestLegacyRedirect } from '@/features/admin/AdminRequestLegacyRedirect'
import { LoginPage } from '@/features/auth/LoginPage'
import { adminOnly } from '@/features/auth/route-policy'
import { GuestRoute, ProtectedRoute, RoleRedirect } from '@/features/auth/ProtectedRoute'
import { DashboardPage } from '@/features/dashboard/DashboardPage'
import { UiStatesDemoPage } from '@/features/dev/UiStatesDemoPage'
import { RequestDetailPage } from '@/features/requests/RequestDetailPage'

const adminRequestDetailPage = lazy(() =>
  import('@/features/admin/AdminRequestDetailPage').then((module) => ({
    default: module.AdminRequestDetailPage,
  })),
)
const sponsorshipTypesPage = lazy(() =>
  import('@/features/admin/SponsorshipTypesPage').then((module) => ({
    default: module.SponsorshipTypesPage,
  })),
)
const usersPage = lazy(() =>
  import('@/features/admin/UsersPage').then((module) => ({
    default: module.UsersPage,
  })),
)
const auditPage = lazy(() =>
  import('@/features/admin/AuditPage').then((module) => ({
    default: module.AuditPage,
  })),
)

function lazyPage(page: ReactNode) {
  return (
    <Suspense fallback={<LoadingState title="Loading page" metricCount={0} tableRows={5} />}>
      {page}
    </Suspense>
  )
}

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <GuestRoute />,
    children: [{ index: true, element: <LoginPage /> }],
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppShell />,
        children: [
          { index: true, element: <Navigate to="/dashboard" replace /> },
          { path: 'dashboard', element: <DashboardPage /> },
          { path: 'requests/:id', element: <RequestDetailPage /> },
          {
            path: 'dashboard/requests/:id',
            element: lazyPage(createElement(adminRequestDetailPage)),
          },
          { path: 'dev/ui-states', element: <UiStatesDemoPage /> },
          { path: 'profile', element: <ProfilePage /> },
          { path: 'admin/requests', element: <Navigate to="/dashboard" replace /> },
          { path: 'admin/requests/:id', element: <AdminRequestLegacyRedirect /> },
          {
            element: <ProtectedRoute allowedRoles={[...adminOnly]} />,
            children: [
              {
                path: 'admin/sponsorship-types',
                element: lazyPage(createElement(sponsorshipTypesPage)),
              },
              {
                path: 'admin/users',
                element: lazyPage(createElement(usersPage)),
              },
              {
                path: 'admin/audit',
                element: lazyPage(createElement(auditPage)),
              },
            ],
          },
        ],
      },
    ],
  },
  { path: '*', element: <RoleRedirect /> },
])
