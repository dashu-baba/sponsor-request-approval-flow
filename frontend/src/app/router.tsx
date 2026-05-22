import { createElement, lazy, Suspense, type ReactNode } from 'react'
import { createBrowserRouter, Navigate } from 'react-router-dom'

import { AppShell } from '@/app/layout/AppShell'
import { LoadingState } from '@/components/states/query-states'
import { ProfileStubPage } from '@/features/account/ProfileStubPage'
import { LoginPage } from '@/features/auth/LoginPage'
import { adminOnly } from '@/features/auth/route-policy'
import { GuestRoute, ProtectedRoute, RoleRedirect } from '@/features/auth/ProtectedRoute'
import { DashboardPage } from '@/features/dashboard/DashboardPage'
import { UiStatesDemoPage } from '@/features/dev/UiStatesDemoPage'

const adminRequestsPage = lazy(() =>
  import('@/features/admin/AdminRequestsPage').then((module) => ({
    default: module.AdminRequestsPage,
  })),
)
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
          { path: 'dev/ui-states', element: <UiStatesDemoPage /> },
          { path: 'profile', element: <ProfileStubPage /> },
          {
            element: <ProtectedRoute allowedRoles={[...adminOnly]} />,
            children: [
              { path: 'admin/requests', element: lazyPage(createElement(adminRequestsPage)) },
              {
                path: 'admin/requests/:id',
                element: lazyPage(createElement(adminRequestDetailPage)),
              },
              {
                path: 'admin/sponsorship-types',
                element: lazyPage(createElement(sponsorshipTypesPage)),
              },
            ],
          },
        ],
      },
    ],
  },
  { path: '*', element: <RoleRedirect /> },
])
