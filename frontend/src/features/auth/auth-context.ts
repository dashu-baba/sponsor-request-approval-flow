import { createContext } from 'react'

import type { UserProfile } from '@/lib/schemas/auth'

export type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated'

export interface AuthContextValue {
  status: AuthStatus
  user: UserProfile | null
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
  refreshProfile: () => Promise<UserProfile | null>
}

export const AuthContext = createContext<AuthContextValue | null>(null)
