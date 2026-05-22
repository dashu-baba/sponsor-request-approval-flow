import { useContext } from 'react'

import { AuthContext, type AuthContextValue } from '@/features/auth/auth-context'
import type { UserProfile } from '@/lib/schemas/auth'

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return context
}

export function useCurrentUser(): UserProfile {
  const { user } = useAuth()
  if (!user) {
    throw new Error('Authenticated user is required')
  }
  return user
}
