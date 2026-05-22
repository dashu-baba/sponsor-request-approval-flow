import { PageHeader } from '@/components/PageHeader'
import { EmptyState } from '@/components/states/query-states'

export function ProfileStubPage() {
  return (
    <div>
      <PageHeader title="Profile" subtitle="Account settings and personal details." />
      <EmptyState
        title="Profile coming soon"
        description="Profile management is deferred to the backlog."
      />
    </div>
  )
}
