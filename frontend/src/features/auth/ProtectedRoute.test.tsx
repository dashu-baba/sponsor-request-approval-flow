import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'

import { ProtectedRoute } from '@/features/auth/ProtectedRoute'
import { Roles } from '@/lib/roles'
import type { UserProfile } from '@/lib/schemas/auth'

const authState = vi.hoisted(() => ({
  status: 'unauthenticated' as 'loading' | 'authenticated' | 'unauthenticated',
  user: null as UserProfile | null,
}))

vi.mock('@/features/auth/use-auth', () => ({
  useAuth: () => authState,
}))

function DashboardStub() {
  return <div>Dashboard content</div>
}

describe('ProtectedRoute', () => {
  it('redirects unauthenticated users to login', () => {
    authState.status = 'unauthenticated'
    authState.user = null

    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Routes>
          <Route path="/login" element={<div>Login page</div>} />
          <Route element={<ProtectedRoute />}>
            <Route path="/dashboard" element={<DashboardStub />} />
          </Route>
        </Routes>
      </MemoryRouter>,
    )

    expect(screen.getByText('Login page')).toBeInTheDocument()
  })

  it('redirects users outside allowed roles', () => {
    authState.status = 'authenticated'
    authState.user = {
      id: '1',
      email: 'manager@demo.local',
      displayName: 'Manager User',
      department: 'Ops',
      role: Roles.Manager,
    }

    render(
      <MemoryRouter initialEntries={['/admin/sponsorship-types']}>
        <Routes>
          <Route element={<ProtectedRoute />}>
            <Route element={<ProtectedRoute allowedRoles={[Roles.SystemAdmin]} />}>
              <Route path="/admin/sponsorship-types" element={<div>Admin page</div>} />
            </Route>
            <Route path="/dashboard" element={<DashboardStub />} />
          </Route>
        </Routes>
      </MemoryRouter>,
    )

    expect(screen.getByText('Dashboard content')).toBeInTheDocument()
  })
})
