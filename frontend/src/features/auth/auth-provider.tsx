import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'

import { AuthContext, type AuthContextValue, type AuthStatus } from '@/features/auth/auth-context'
import { ApiError } from '@/lib/api/api-error'
import * as authApi from '@/lib/api/auth-api'
import { clearAccessToken } from '@/lib/api/token-store'
import { queryClient } from '@/lib/query-client'
import type { UserProfile } from '@/lib/schemas/auth'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>('loading')
  const [user, setUser] = useState<UserProfile | null>(null)

  const refreshProfile = useCallback(async (): Promise<UserProfile | null> => {
    try {
      const profile = await authApi.getMe()
      setUser(profile)
      setStatus('authenticated')
      return profile
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        setUser(null)
        setStatus('unauthenticated')
        return null
      }
      throw error
    }
  }, [])

  useEffect(() => {
    let cancelled = false

    async function bootstrap() {
      const refreshed = await authApi.refreshSession()
      if (cancelled) return

      if (!refreshed) {
        setUser(null)
        setStatus('unauthenticated')
        return
      }

      try {
        const profile = await authApi.getMe()
        if (cancelled) return
        setUser(profile)
        setStatus('authenticated')
      } catch {
        if (cancelled) return
        clearAccessToken()
        setUser(null)
        setStatus('unauthenticated')
      }
    }

    void bootstrap()

    return () => {
      cancelled = true
    }
  }, [])

  const login = useCallback(
    async (email: string, password: string) => {
      await authApi.login({ email, password })
      await refreshProfile()
    },
    [refreshProfile],
  )

  const logout = useCallback(async () => {
    await authApi.logout()
    queryClient.clear()
    setUser(null)
    setStatus('unauthenticated')
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      user,
      login,
      logout,
      refreshProfile,
    }),
    [status, user, login, logout, refreshProfile],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
