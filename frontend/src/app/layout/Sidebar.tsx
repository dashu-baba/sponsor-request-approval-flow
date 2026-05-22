import { useQuery } from '@tanstack/react-query'
import { LogOut } from 'lucide-react'
import { NavLink, useLocation } from 'react-router-dom'

import { Button } from '@/components/ui/button'
import { useAuth, useCurrentUser } from '@/features/auth/use-auth'
import { getNavSections, type DashboardStatusFilter } from '@/features/auth/role-nav'
import { getRequestSummary } from '@/lib/api/requests-api'
import { queryKeys } from '@/lib/query-client'
import { Roles } from '@/lib/roles'
import { cn } from '@/lib/utils'

function getRequestorNavBadgeCount(
  statusFilter: DashboardStatusFilter | undefined,
  summary: {
    total: number
    draft: number
    pendingManagerApproval: number
    pendingFinanceReview: number
    approved: number
    rejected: number
  },
): number | null {
  switch (statusFilter) {
    case 'all':
      return summary.total
    case 'Draft':
      return summary.draft
    case 'PendingManagerApproval':
      return summary.pendingManagerApproval + summary.pendingFinanceReview
    case 'Approved':
      return summary.approved
    case 'Rejected':
      return summary.rejected
    default:
      return null
  }
}

function parseStatusFilter(search: string): DashboardStatusFilter | null {
  const params = new URLSearchParams(search)
  const status = params.get('status')
  if (!status) return 'overview'
  if (status === 'all') return 'all'
  if (
    status === 'Draft' ||
    status === 'PendingManagerApproval' ||
    status === 'Approved' ||
    status === 'Rejected'
  ) {
    return status
  }
  return null
}

function isNavItemActive(pathname: string, search: string, to: string): boolean {
  const [toPath, toQuery = ''] = to.split('?')
  if (pathname !== toPath) return false

  const currentParams = new URLSearchParams(search)
  const targetParams = new URLSearchParams(toQuery ? `?${toQuery}` : '')

  const currentStatus = currentParams.get('status') ?? null
  const targetStatus = targetParams.get('status') ?? null

  if (to === '/dashboard') {
    return currentStatus === null
  }

  if (targetStatus === 'all') {
    return currentStatus === 'all'
  }

  return currentStatus === targetStatus
}

export function Sidebar() {
  const user = useCurrentUser()
  const { logout } = useAuth()
  const location = useLocation()
  const sections = getNavSections(user.role)
  const currentFilter = parseStatusFilter(location.search)

  const summaryQuery = useQuery({
    queryKey: queryKeys.requests.summary,
    queryFn: getRequestSummary,
    enabled: user.role === Roles.Requestor,
  })

  const summary = summaryQuery.data

  return (
    <aside
      className="fixed top-[var(--topbar-height)] bottom-0 left-0 z-[100] flex w-[var(--sidebar-width)] flex-col overflow-y-auto border-r border-border bg-surface py-4"
      role="navigation"
      aria-label="Main navigation"
    >
      {sections.map((section) => (
        <div key={section.label} className="mb-1">
          <div className="px-[18px] py-2 text-[10px] font-semibold tracking-[0.7px] text-text-hint uppercase">
            {section.label}
          </div>
          {section.items.map((item) => {
            const Icon = item.icon
            const active = isNavItemActive(location.pathname, location.search, item.to)
            const isDashboardRoot = item.to === '/dashboard'

            return (
              <NavLink
                key={item.to}
                to={item.to}
                end={isDashboardRoot}
                className={cn(
                  'flex items-center gap-2 border-l-2 border-transparent px-[18px] py-2.5 text-[13px] text-text-secondary no-underline transition-colors',
                  active && 'border-brand bg-brand-light font-medium text-brand',
                  !active && 'hover:bg-page hover:text-text-primary',
                )}
                aria-current={active ? 'page' : undefined}
              >
                <Icon
                  className={cn('h-4 w-4 shrink-0 opacity-75', active && 'opacity-100')}
                  aria-hidden="true"
                />
                <span>{item.label}</span>
                {user.role === Roles.Requestor && item.statusFilter && summary
                  ? (() => {
                      const count = getRequestorNavBadgeCount(item.statusFilter, summary)
                      if (count === null || count <= 0) return null
                      return (
                        <span
                          className={cn(
                            'ml-auto rounded-full px-1.5 py-0.5 text-[10px] font-semibold',
                            item.statusFilter === currentFilter
                              ? 'bg-brand text-white'
                              : 'bg-page text-text-secondary',
                          )}
                        >
                          {count}
                        </span>
                      )
                    })()
                  : null}
              </NavLink>
            )
          })}
        </div>
      ))}

      <div className="mt-auto border-t border-border px-[18px] pt-3">
        <Button
          type="button"
          variant="ghost"
          className="h-auto w-full justify-start gap-2 px-2.5 py-2 text-[13px] text-text-secondary hover:text-danger"
          onClick={() => {
            void logout()
          }}
        >
          <LogOut className="h-4 w-4" aria-hidden="true" />
          Sign out
        </Button>
      </div>
    </aside>
  )
}
