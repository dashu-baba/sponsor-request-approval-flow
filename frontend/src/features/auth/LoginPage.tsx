import { Loader2 } from 'lucide-react'
import { type FormEvent, useState } from 'react'
import { useNavigate } from 'react-router-dom'

import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAuth } from '@/features/auth/use-auth'
import { ApiError } from '@/lib/api/api-error'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
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
      navigate('/dashboard', { replace: true })
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
    <div className="flex min-h-screen bg-page">
      <section className="relative hidden w-[420px] shrink-0 flex-col justify-center gap-12 overflow-hidden bg-brand p-12 text-white lg:flex">
        <div className="pointer-events-none absolute -top-36 -right-36 h-[420px] w-[420px] rounded-full bg-white/5" />
        <div className="pointer-events-none absolute -bottom-24 -left-24 h-80 w-80 rounded-full bg-white/[0.04]" />
        <div className="pointer-events-none absolute top-1/2 -right-20 h-60 w-60 -translate-y-1/2 rounded-full border border-white/10" />

        <div className="relative flex items-center gap-2.5">
          <div className="flex h-9 w-9 items-center justify-center rounded-[9px] bg-white/15 text-lg">
            ◈
          </div>
          <div className="text-xl font-semibold tracking-tight">
            Spon<span className="font-light opacity-60">Track</span>
          </div>
        </div>

        <div className="relative">
          <h1 className="mb-4 text-[32px] leading-tight font-light tracking-tight">
            <strong className="block font-semibold">Sponsorship requests,</strong>
            simplified.
          </h1>
          <p className="max-w-[300px] text-sm leading-7 text-white/60">
            Internal sponsorship request &amp; approval workflow for your organisation.
          </p>
        </div>

        <p className="relative text-xs text-white/45">
          Demo: <code className="rounded bg-white/10 px-1.5 py-0.5">requestor@demo.local</code> /{' '}
          <code className="rounded bg-white/10 px-1.5 py-0.5">Password1!</code>
        </p>
      </section>

      <section className="flex flex-1 items-center justify-center p-6 sm:p-10">
        <div className="w-full max-w-[420px] rounded-[16px] border border-border bg-surface p-8 shadow-[0_8px_40px_rgba(74,63,200,0.14),0_2px_8px_rgba(74,63,200,0.08)]">
          <div className="mb-8 lg:hidden">
            <div className="mb-2 flex items-center gap-2">
              <div className="flex h-8 w-8 items-center justify-center rounded-[8px] bg-brand text-white">
                ◈
              </div>
              <span className="text-lg font-semibold">
                Spon<span className="text-brand">Track</span>
              </span>
            </div>
            <p className="text-sm text-text-secondary">Sign in to your account</p>
          </div>

          <div className="mb-8 hidden lg:block">
            <h2 className="text-2xl font-semibold tracking-tight">Welcome back</h2>
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

            <div className="flex items-center justify-between text-xs text-text-secondary">
              <label className="flex cursor-not-allowed items-center gap-2 opacity-50">
                <input type="checkbox" disabled className="rounded border-border" />
                Remember me
              </label>
              <span className="cursor-not-allowed opacity-50">Forgot password?</span>
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
      </section>
    </div>
  )
}
