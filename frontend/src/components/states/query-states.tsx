import { Loader2 } from 'lucide-react'
import type { ReactNode } from 'react'

import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

interface LoadingStateProps {
  title?: string
  description?: string
  metricCount?: number
  tableRows?: number
}

export function LoadingState({
  title = 'Loading',
  description = 'Please wait while we fetch your data…',
  metricCount = 4,
  tableRows = 5,
}: LoadingStateProps) {
  return (
    <div className="space-y-6">
      <Alert variant="info">
        <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
        <AlertTitle>{title}</AlertTitle>
        <AlertDescription>{description}</AlertDescription>
      </Alert>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {Array.from({ length: metricCount }).map((_, index) => (
          <Card key={index}>
            <CardContent className="space-y-3 p-5">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-8 w-16" />
            </CardContent>
          </Card>
        ))}
      </div>

      <Card>
        <CardContent className="space-y-3 p-5">
          {Array.from({ length: tableRows }).map((_, index) => (
            <Skeleton key={index} className="h-10 w-full" />
          ))}
        </CardContent>
      </Card>
    </div>
  )
}

interface ErrorStateProps {
  title?: string
  message: string
  onRetry?: () => void
}

export function ErrorState({ title = 'Something went wrong', message, onRetry }: ErrorStateProps) {
  return (
    <div className="space-y-4">
      <Alert variant="destructive">
        <AlertTitle>{title}</AlertTitle>
        <AlertDescription>{message}</AlertDescription>
      </Alert>
      <Card>
        <CardContent className="flex flex-col items-center gap-4 px-6 py-12 text-center">
          <p className="max-w-md text-[13px] text-text-secondary">{message}</p>
          {onRetry ? (
            <Button type="button" onClick={onRetry}>
              Retry
            </Button>
          ) : null}
        </CardContent>
      </Card>
    </div>
  )
}

interface EmptyStateProps {
  title: string
  description?: string
  action?: ReactNode
}

export function EmptyState({ title, description, action }: EmptyStateProps) {
  return (
    <Card>
      <CardContent className="flex flex-col items-center gap-3 px-6 py-16 text-center">
        <h3 className="text-base font-semibold text-text-primary">{title}</h3>
        {description ? (
          <p className="max-w-md text-[13px] text-text-secondary">{description}</p>
        ) : null}
        {action}
      </CardContent>
    </Card>
  )
}
