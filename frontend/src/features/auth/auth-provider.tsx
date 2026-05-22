import { useQuery } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'

import { AuthContext, type AuthContextValue, type AuthStatus } from '@/features/auth/auth-context'
import * as authApi from '@/lib/api/auth-api'
import { setSessionExpiredHandler } from '@/lib/api/session-expired'
import { clearAccessToken } from '@/lib/api/token-store'
import { queryClient, queryKeys } from '@/lib/query-client'
import type { UserProfile } from '@/lib/schemas/auth'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [bootstrapComplete, setBootstrapComplete] = useState(false)
  const [sessionEnabled, setSessionEnabled] = useState(false)

  const endSession = useCallback(() => {
    clearAccessToken()
    setSessionEnabled(false)
    queryClient.removeQueries({ queryKey: queryKeys.me })
  }, [])

  useEffect(() => {
    setSessionExpiredHandler(endSession)
    return () => setSessionExpiredHandler(null)
  }, [endSession])

  useEffect(() => {
    let cancelled = false

    async function bootstrap() {
      const refreshed = await authApi.refreshSession()
      if (cancelled) return

      if (refreshed) {
        setSessionEnabled(true)
      } else {
        endSession()
      }

      setBootstrapComplete(true)
    }

    void bootstrap()

    return () => {
      cancelled = true
    }
  }, [endSession])

  const meQuery = useQuery({
    queryKey: queryKeys.me,
    queryFn: async () => {
      try {
        return await authApi.getMe()
      } catch {
        endSession()
        throw new Error('Not authenticated')
      }
    },
    enabled: bootstrapComplete && sessionEnabled,
    retry: false,
  })

  const status = useMemo((): AuthStatus => {
    if (!bootstrapComplete) return 'loading'
    if (!sessionEnabled) return 'unauthenticated'
    if (meQuery.isPending) return 'loading'
    if (meQuery.isSuccess) return 'authenticated'
    return 'unauthenticated'
  }, [bootstrapComplete, sessionEnabled, meQuery.isPending, meQuery.isSuccess])

  const user = meQuery.data ?? null

  const login = useCallback(async (email: string, password: string) => {
    await authApi.login({ email, password })
    setSessionEnabled(true)
    await queryClient.invalidateQueries({ queryKey: queryKeys.me })
  }, [])

  const logout = useCallback(async () => {
    await authApi.logout()
    queryClient.clear()
    endSession()
  }, [endSession])

  const refreshProfile = useCallback(async (): Promise<UserProfile | null> => {
    if (!sessionEnabled) return null

    try {
      return await queryClient.fetchQuery({
        queryKey: queryKeys.me,
        queryFn: authApi.getMe,
      })
    } catch {
      endSession()
      return null
    }
  }, [endSession, sessionEnabled])

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
