import { useQuery } from '@tanstack/react-query'
import { toast } from 'sonner'
import { PageHeader } from '@/components/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/states/query-states'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { apiFetch } from '@/lib/api/auth-api'
import { queryKeys } from '@/lib/query-client'

async function fetchHealth(): Promise<string> {
  const response = await apiFetch('/health')
  if (!response.ok) {
    throw new Error('Health check failed')
  }
  return response.text()
}

interface QueryStatePanelProps {
  forceEmpty?: boolean
  forceError?: boolean
}

export function QueryStatePanel({ forceEmpty = false, forceError = false }: QueryStatePanelProps) {
  const query = useQuery({
    queryKey: [...queryKeys.health, forceEmpty, forceError],
    queryFn: async () => {
      if (forceError) {
        throw new Error('Unable to reach the API')
      }
      const result = await fetchHealth()
      if (forceEmpty) {
        return null
      }
      return result
    },
  })

  if (query.isLoading) {
    return <LoadingState metricCount={2} tableRows={3} />
  }

  if (query.isError) {
    return (
      <ErrorState
        message={query.error instanceof Error ? query.error.message : 'Unexpected error'}
        onRetry={() => {
          void query.refetch()
        }}
      />
    )
  }

  if (query.data === null) {
    return (
      <EmptyState
        title="No data yet"
        description="When requests are available they will appear here."
        action={
          <Button type="button" variant="outline" onClick={() => void query.refetch()}>
            Refresh
          </Button>
        }
      />
    )
  }

  return (
    <Card>
      <CardContent className="space-y-3 p-5">
        <Alert variant="success">
          <AlertTitle>API connected</AlertTitle>
          <AlertDescription>
            Health status: <strong>{query.data}</strong>
          </AlertDescription>
        </Alert>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => toast.success('Sample toast — changes saved')}
        >
          Show success toast
        </Button>
      </CardContent>
    </Card>
  )
}

export function QueryStateDemoSection() {
  return (
    <section className="space-y-6">
      <PageHeader
        title="UI state patterns"
        subtitle="Loading, empty, error, and success patterns for upcoming feature work."
      />
      <QueryStatePanel />
    </section>
  )
}
