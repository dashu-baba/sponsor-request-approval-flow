import { ApiError, parseProblemResponse } from '@/lib/api/api-error'
import { notifySessionExpired } from '@/lib/api/session-expired'
import { clearAccessToken, getAccessToken, setAccessToken } from '@/lib/api/token-store'
import {
  loginResponseSchema,
  userProfileSchema,
  type LoginResponse,
  type UserProfile,
} from '@/lib/schemas/auth'

export interface LoginRequest {
  email: string
  password: string
}

export { setSessionExpiredHandler } from '@/lib/api/session-expired'

let refreshPromise: Promise<LoginResponse | null> | null = null

async function requestJson<T>(
  path: string,
  init: RequestInit,
  schema: { parse: (data: unknown) => T },
): Promise<T> {
  const response = await fetch(path, {
    credentials: 'include',
    ...init,
    headers: {
      Accept: 'application/json',
      ...(init.body ? { 'Content-Type': 'application/json' } : {}),
      ...init.headers,
    },
  })

  if (!response.ok) {
    throw await parseProblemResponse(response)
  }

  const data: unknown = await response.json()
  return schema.parse(data)
}

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const response = await requestJson(
    '/auth/login',
    {
      method: 'POST',
      body: JSON.stringify(request),
    },
    loginResponseSchema,
  )
  setAccessToken(response.accessToken)
  return response
}

export async function refreshSession(): Promise<LoginResponse | null> {
  if (refreshPromise) {
    return refreshPromise
  }

  refreshPromise = (async () => {
    try {
      const response = await fetch('/auth/refresh', {
        method: 'POST',
        credentials: 'include',
      })

      if (!response.ok) {
        clearAccessToken()
        notifySessionExpired()
        return null
      }

      const data: unknown = await response.json()
      const parsed = loginResponseSchema.parse(data)
      setAccessToken(parsed.accessToken)
      return parsed
    } catch {
      clearAccessToken()
      notifySessionExpired()
      return null
    } finally {
      refreshPromise = null
    }
  })()

  return refreshPromise
}

export async function logout(): Promise<void> {
  try {
    await fetch('/auth/logout', {
      method: 'POST',
      credentials: 'include',
    })
  } finally {
    clearAccessToken()
  }
}

export async function getMe(): Promise<UserProfile> {
  const token = getAccessToken()
  if (!token) {
    throw new ApiError(401, 'Not authenticated')
  }

  return requestJson(
    '/me',
    {
      method: 'GET',
      headers: {
        Authorization: `Bearer ${token}`,
      },
    },
    userProfileSchema,
  )
}

export async function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const token = getAccessToken()
  const headers = new Headers(init.headers)

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json')
  }

  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  let response = await fetch(path, {
    credentials: 'include',
    ...init,
    headers,
  })

  if (response.status !== 401 || path.startsWith('/auth/')) {
    return response
  }

  const refreshed = await refreshSession()
  if (!refreshed) {
    notifySessionExpired()
    return response
  }

  headers.set('Authorization', `Bearer ${refreshed.accessToken}`)
  response = await fetch(path, {
    credentials: 'include',
    ...init,
    headers,
  })

  return response
}

export async function apiJson<T>(
  path: string,
  init: RequestInit,
  schema: { parse: (data: unknown) => T },
): Promise<T> {
  const response = await apiFetch(path, init)

  if (!response.ok) {
    throw await parseProblemResponse(response)
  }

  const data: unknown = await response.json()
  return schema.parse(data)
}
