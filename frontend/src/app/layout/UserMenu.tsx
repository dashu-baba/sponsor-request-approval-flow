import { ChevronUp, LogOut, User } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'

import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { useAuth, useCurrentUser } from '@/features/auth/use-auth'
import { getRoleLabel } from '@/lib/roles'
import { cn, getInitials } from '@/lib/utils'

export function UserMenu() {
  const user = useCurrentUser()
  const { logout } = useAuth()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return

    function handlePointerDown(event: MouseEvent) {
      if (!menuRef.current?.contains(event.target as Node)) {
        setOpen(false)
      }
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setOpen(false)
      }
    }

    document.addEventListener('mousedown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('mousedown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [open])

  async function handleSignOut() {
    setOpen(false)
    await logout()
    void navigate('/login', { replace: true })
  }

  return (
    <div ref={menuRef} className="relative">
      {open ? (
        <div
          className="absolute right-0 bottom-full left-0 mb-2 overflow-hidden rounded-[10px] border border-border bg-surface shadow-[0_8px_24px_rgba(26,24,48,0.12)]"
          role="menu"
          aria-label="User menu"
        >
          <div className="border-b border-border px-3 py-3">
            <div className="text-[13px] font-medium text-text-primary">{user.displayName}</div>
            <div className="mt-0.5 truncate text-[12px] text-text-secondary">{user.email}</div>
            <div className="mt-1 text-[11px] text-text-hint">{getRoleLabel(user.role)}</div>
          </div>

          <Link
            to="/profile"
            role="menuitem"
            className="flex w-full items-center gap-2 px-3 py-2.5 text-[13px] text-text-secondary no-underline transition-colors hover:bg-page hover:text-text-primary"
            onClick={() => setOpen(false)}
          >
            <User className="h-4 w-4 shrink-0" aria-hidden="true" />
            Profile
          </Link>

          <button
            type="button"
            role="menuitem"
            className="flex w-full items-center gap-2 border-t border-border px-3 py-2.5 text-left text-[13px] text-danger transition-colors hover:bg-danger-bg"
            onClick={() => {
              void handleSignOut()
            }}
          >
            <LogOut className="h-4 w-4 shrink-0" aria-hidden="true" />
            Sign out
          </button>
        </div>
      ) : null}

      <button
        type="button"
        className={cn(
          'flex w-full items-center gap-2 rounded-[10px] border border-border px-2 py-2 text-left transition-colors',
          open ? 'border-border-strong bg-page' : 'hover:border-border-strong hover:bg-page',
        )}
        aria-expanded={open}
        aria-haspopup="menu"
        onClick={() => setOpen((current) => !current)}
      >
        <Avatar className="h-8 w-8">
          <AvatarFallback className="text-[11px]">{getInitials(user.displayName)}</AvatarFallback>
        </Avatar>
        <span className="min-w-0 flex-1">
          <span className="block truncate text-[12.5px] font-medium text-text-primary">
            {user.displayName}
          </span>
          <span className="block truncate text-[10.5px] text-text-hint">
            {getRoleLabel(user.role)}
          </span>
        </span>
        <ChevronUp
          className={cn(
            'h-4 w-4 shrink-0 text-text-hint transition-transform',
            open && 'rotate-180',
          )}
          aria-hidden="true"
        />
      </button>
    </div>
  )
}
