import { createBrowserRouter, Navigate } from 'react-router-dom'

import { AppShell } from '@/app/layout/AppShell'
import { SponsorshipTypesStubPage } from '@/features/admin/SponsorshipTypesStubPage'
import { ProfileStubPage } from '@/features/account/ProfileStubPage'
import { LoginPage } from '@/features/auth/LoginPage'
import { adminOnly } from '@/features/auth/route-policy'
import { GuestRoute, ProtectedRoute, RoleRedirect } from '@/features/auth/ProtectedRoute'
import { DashboardPage } from '@/features/dashboard/DashboardPage'

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
          { path: 'profile', element: <ProfileStubPage /> },
          {
            element: <ProtectedRoute allowedRoles={[...adminOnly]} />,
            children: [{ path: 'admin/sponsorship-types', element: <SponsorshipTypesStubPage /> }],
          },
        ],
      },
    ],
  },
  { path: '*', element: <RoleRedirect /> },
])
