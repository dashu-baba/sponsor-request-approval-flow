import { Loader2 } from 'lucide-react'
import { type FormEvent, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'

import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAuth } from '@/features/auth/use-auth'
import { ApiError } from '@/lib/api/api-error'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const redirectTo =
    typeof location.state === 'object' &&
    location.state !== null &&
    'from' in location.state &&
    typeof location.state.from === 'string'
      ? location.state.from
      : '/dashboard'
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      await login(email.trim(), password)
      navigate(redirectTo, { replace: true })
    } catch (caught) {
      if (caught instanceof ApiError) {
        setError(caught.detail ?? caught.message)
      } else if (caught instanceof Error) {
        setError(caught.message)
      } else {
        setError('Sign in failed. Please try again.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="relative flex min-h-screen flex-col items-center justify-center overflow-hidden bg-brand px-6 py-12 text-white">
      <div className="pointer-events-none absolute -top-36 -right-36 h-[420px] w-[420px] rounded-full bg-white/5" />
      <div className="pointer-events-none absolute -bottom-24 -left-24 h-80 w-80 rounded-full bg-white/[0.04]" />
      <div className="pointer-events-none absolute top-1/2 -left-20 h-60 w-60 -translate-y-1/2 rounded-full border border-white/10" />
      <div className="pointer-events-none absolute right-[10%] bottom-[15%] h-40 w-40 rounded-full border border-white/[0.08]" />

      <div className="relative z-10 flex w-full max-w-[420px] flex-col gap-8">
        <div className="flex flex-col items-center gap-4 text-center">
          <div className="flex items-center gap-2.5">
            <div className="flex h-10 w-10 items-center justify-center rounded-[10px] bg-white/15 text-xl">
              ◈
            </div>
            <div className="text-2xl font-semibold tracking-tight">
              Spon<span className="font-light opacity-60">Track</span>
            </div>
          </div>
          <div>
            <h1 className="text-[28px] leading-tight font-light tracking-tight sm:text-[32px]">
              <strong className="block font-semibold">Sponsorship requests,</strong>
              simplified.
            </h1>
            <p className="mx-auto mt-3 max-w-[320px] text-sm leading-7 text-white/60">
              Internal sponsorship request &amp; approval workflow for your organisation.
            </p>
          </div>
        </div>

        <div className="rounded-[16px] border border-white/10 bg-surface p-8 text-text-primary shadow-[0_8px_40px_rgba(26,24,48,0.2),0_2px_8px_rgba(26,24,48,0.12)]">
          <div className="mb-8">
            <h2 className="text-2xl font-semibold tracking-tight text-text-primary">Welcome back</h2>
            <p className="mt-1 text-sm text-text-secondary">Sign in to continue to SponTrack</p>
          </div>

          {error ? (
            <Alert variant="destructive" className="mb-6">
              <AlertTitle>Sign in failed</AlertTitle>
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          ) : null}

          <form className="space-y-5" onSubmit={(event) => void handleSubmit(event)} noValidate>
            <div className="space-y-2">
              <Label htmlFor="email">Email address</Label>
              <Input
                id="email"
                name="email"
                type="email"
                autoComplete="username"
                placeholder="you@company.com"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                required
                disabled={isSubmitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                name="password"
                type="password"
                autoComplete="current-password"
                placeholder="Enter your password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                required
                disabled={isSubmitting}
              />
            </div>

            <div className="flex items-center text-xs text-text-secondary">
              <label className="flex cursor-not-allowed items-center gap-2 opacity-50">
                <input type="checkbox" disabled className="rounded border-border" />
                Remember me
              </label>
            </div>

            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
                  Signing in…
                </>
              ) : (
                'Sign in'
              )}
            </Button>
          </form>
        </div>
      </div>
    </div>
  )
}
