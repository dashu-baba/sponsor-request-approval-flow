import { PageHeader } from '@/components/PageHeader'
import { EmptyState } from '@/components/states/query-states'

export function SponsorshipTypesStubPage() {
  return (
    <div>
      <PageHeader
        title="Sponsorship types"
        subtitle="Manage sponsorship categories available to requestors."
      />
      <EmptyState
        title="Administration UI coming in T3.4"
        description="Sponsorship type CRUD will be implemented in the admin feature task."
      />
    </div>
  )
}
