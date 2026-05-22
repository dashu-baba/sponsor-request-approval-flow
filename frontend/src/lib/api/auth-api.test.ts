import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { clearAccessToken, setAccessToken } from '@/lib/api/token-store'
import { setSessionExpiredHandler } from '@/lib/api/session-expired'

const loginResponse = {
  accessToken: 'refreshed-token',
  accessTokenExpiresAt: '2026-12-01T00:00:00+00:00',
  tokenType: 'Bearer',
}

describe('auth-api', () => {
  const fetchMock = vi.fn()
  const sessionExpiredMock = vi.fn()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    clearAccessToken()
    setSessionExpiredHandler(sessionExpiredMock)
    sessionExpiredMock.mockReset()
    fetchMock.mockReset()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    setSessionExpiredHandler(null)
    clearAccessToken()
  })

  it('retries a request after refresh succeeds on 401', async () => {
    setAccessToken('expired-token')

    fetchMock
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(
        new Response(JSON.stringify(loginResponse), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
      )
      .mockResolvedValueOnce(new Response('Healthy', { status: 200 }))

    const { apiFetch } = await import('@/lib/api/auth-api')
    const response = await apiFetch('/health')

    expect(response.status).toBe(200)
    expect(await response.text()).toBe('Healthy')
    expect(fetchMock).toHaveBeenCalledTimes(3)
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/health')
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/auth/refresh')
    expect(fetchMock.mock.calls[2]?.[0]).toBe('/api/health')
    expect(sessionExpiredMock).not.toHaveBeenCalled()
  })

  it('notifies session expiry when refresh fails after 401', async () => {
    setAccessToken('expired-token')

    fetchMock
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(new Response(null, { status: 401 }))

    const { apiFetch } = await import('@/lib/api/auth-api')
    const response = await apiFetch('/health')

    expect(response.status).toBe(401)
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/health')
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/auth/refresh')
    expect(sessionExpiredMock).toHaveBeenCalledTimes(2)
  })

  it('sets JSON Content-Type on POST bodies', async () => {
    fetchMock.mockResolvedValueOnce(new Response('{}', { status: 200 }))

    const { apiFetch } = await import('@/lib/api/auth-api')
    await apiFetch('/requests', {
      method: 'POST',
      body: JSON.stringify({ title: 'Test' }),
    })

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit]
    expect(url).toBe('/api/requests')
    expect(new Headers(init.headers).get('Content-Type')).toBe('application/json')
  })

  it('does not set Content-Type for FormData uploads', async () => {
    fetchMock.mockResolvedValueOnce(new Response('{}', { status: 200 }))

    const { apiFetch } = await import('@/lib/api/auth-api')
    const formData = new FormData()
    formData.append('file', new File(['x'], 'test.txt'))

    await apiFetch('/requests/1/attachments', {
      method: 'POST',
      body: formData,
    })

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit]
    expect(url).toBe('/api/requests/1/attachments')
    expect(new Headers(init.headers).has('Content-Type')).toBe(false)
  })

  it('notifies session expiry when refresh returns invalid payload', async () => {
    setAccessToken('expired-token')

    fetchMock.mockResolvedValueOnce(new Response(null, { status: 401 })).mockResolvedValueOnce(
      new Response(JSON.stringify({ accessToken: '' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )

    const { apiFetch } = await import('@/lib/api/auth-api')
    const response = await apiFetch('/health')

    expect(response.status).toBe(401)
    expect(sessionExpiredMock).toHaveBeenCalled()
  })
})
