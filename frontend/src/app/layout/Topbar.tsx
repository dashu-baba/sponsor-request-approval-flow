import { Link } from 'react-router-dom'

import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Input } from '@/components/ui/input'
import { useCurrentUser } from '@/features/auth/use-auth'
import { getRoleLabel, Roles } from '@/lib/roles'
import { cn, getInitials } from '@/lib/utils'
import { Bell, Search } from 'lucide-react'

export function Topbar() {
  const user = useCurrentUser()
  const showSearch = user.role === Roles.Requestor

  return (
    <header className="fixed inset-x-0 top-0 z-[200] flex h-[var(--topbar-height)] items-center border-b border-border bg-surface pr-5">
      <Link
        to="/dashboard"
        className="flex h-full w-[var(--sidebar-width)] shrink-0 items-center gap-2 border-r border-border px-5 text-inherit no-underline"
      >
        <div className="flex h-8 w-8 items-center justify-center rounded-[8px] bg-brand text-base text-white">
          ◈
        </div>
        <div className="text-base font-semibold tracking-tight">
          Spon<span className="text-brand">Track</span>
        </div>
      </Link>

      <div className="flex flex-1 px-5">
        {showSearch ? (
          <div className="relative max-w-[360px] flex-1">
            <Search
              className="pointer-events-none absolute top-1/2 left-2.5 h-4 w-4 -translate-y-1/2 text-text-hint"
              aria-hidden="true"
            />
            <Input
              className="bg-page pl-9"
              placeholder="Search requests…"
              aria-label="Search requests"
              disabled
            />
          </div>
        ) : null}
      </div>

      <div className="flex items-center gap-1.5">
        {showSearch ? (
          <button
            type="button"
            className={cn(
              'relative flex h-[34px] w-[34px] items-center justify-center rounded-[8px] border border-border text-text-secondary',
              'hover:border-border-strong hover:bg-page hover:text-text-primary',
            )}
            aria-label="Notifications (coming soon)"
            disabled
          >
            <Bell className="h-4 w-4" />
            <span className="absolute top-1.5 right-1.5 h-1.5 w-1.5 rounded-full border border-surface bg-danger" />
          </button>
        ) : null}

        <button
          type="button"
          className="flex items-center gap-2 rounded-full border border-border py-1 pr-2.5 pl-1 transition-colors hover:border-border-strong hover:bg-page"
          aria-label="User menu"
        >
          <Avatar>
            <AvatarFallback>{getInitials(user.displayName)}</AvatarFallback>
          </Avatar>
          <div className="text-left leading-tight">
            <div className="text-[12.5px] font-medium text-text-primary">{user.displayName}</div>
            <div className="text-[10.5px] text-text-hint">{getRoleLabel(user.role)}</div>
          </div>
        </button>
      </div>
    </header>
  )
}
