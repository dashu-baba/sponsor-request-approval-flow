import {
  BarChart3,
  CheckCircle2,
  Clock3,
  FileText,
  LayoutDashboard,
  Pencil,
  Settings2,
  Users,
  XCircle,
  type LucideIcon,
} from 'lucide-react'

import { Roles, type Role } from '@/lib/roles'

export type DashboardStatusFilter =
  | 'overview'
  | 'all'
  | 'Draft'
  | 'PendingManagerApproval'
  | 'Approved'
  | 'Rejected'

export interface NavItemConfig {
  label: string
  to: string
  icon: LucideIcon
  statusFilter?: DashboardStatusFilter
}

export interface NavSectionConfig {
  label: string
  items: NavItemConfig[]
}

const requestorRequestItems: NavItemConfig[] = [
  { label: 'My Requests', to: '/dashboard?status=all', icon: FileText, statusFilter: 'all' },
  { label: 'Drafts', to: '/dashboard?status=Draft', icon: Pencil, statusFilter: 'Draft' },
  {
    label: 'Pending Approval',
    to: '/dashboard?status=PendingManagerApproval',
    icon: Clock3,
    statusFilter: 'PendingManagerApproval',
  },
  {
    label: 'Approved',
    to: '/dashboard?status=Approved',
    icon: CheckCircle2,
    statusFilter: 'Approved',
  },
  {
    label: 'Rejected',
    to: '/dashboard?status=Rejected',
    icon: XCircle,
    statusFilter: 'Rejected',
  },
]

export function getNavSections(role: Role): NavSectionConfig[] {
  const overview: NavSectionConfig = {
    label: 'Overview',
    items: [{ label: 'Dashboard', to: '/dashboard', icon: LayoutDashboard }],
  }

  switch (role) {
    case Roles.Requestor:
      return [overview, { label: 'Requests', items: requestorRequestItems }]
    case Roles.Manager:
    case Roles.FinanceAdmin:
      return [overview]
    case Roles.SystemAdmin:
      return [
        overview,
        {
          label: 'Administration',
          items: [
            { label: 'Sponsorship Types', to: '/admin/sponsorship-types', icon: Settings2 },
            { label: 'Users', to: '/admin/users', icon: Users },
          ],
        },
      ]
    default:
      return [overview]
  }
}

export function getDashboardHeading(
  role: Role,
  statusFilter: DashboardStatusFilter,
): { title: string; subtitle: string } {
  if (role === Roles.Requestor) {
    if (statusFilter === 'overview') {
      return {
        title: 'Dashboard',
        subtitle: 'Overview of your sponsorship requests',
      }
    }

    if (statusFilter === 'all') {
      return {
        title: 'My requests',
        subtitle: 'All of your sponsorship requests',
      }
    }

    const labels: Record<Exclude<DashboardStatusFilter, 'all' | 'overview'>, string> = {
      Draft: 'Drafts',
      PendingManagerApproval: 'Pending approval',
      Approved: 'Approved requests',
      Rejected: 'Rejected requests',
    }

    return {
      title: labels[statusFilter],
      subtitle: 'Filtered view of your sponsorship requests',
    }
  }

  switch (role) {
    case Roles.Manager:
      return {
        title: 'Approval queue',
        subtitle: 'Requests awaiting your manager approval',
      }
    case Roles.FinanceAdmin:
      return {
        title: 'Finance review queue',
        subtitle: 'Requests awaiting finance approval',
      }
    case Roles.SystemAdmin:
      return {
        title: 'All submitted requests',
        subtitle: 'Submitted requests only — private drafts are visible only to their requestor.',
      }
    default:
      return { title: 'Dashboard', subtitle: '' }
  }
}

export function getDashboardMetricCount(role: Role): number {
  return role === Roles.Requestor ? 5 : 4
}

export function getRoleDashboardIcon(role: Role): LucideIcon {
  if (role === Roles.SystemAdmin) return BarChart3
  return LayoutDashboard
}
