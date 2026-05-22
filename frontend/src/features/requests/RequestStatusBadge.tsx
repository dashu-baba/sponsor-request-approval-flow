import { Badge } from '@/components/ui/badge'
import { getStatusBadgeVariant, requestStatusLabels } from '@/lib/request-status'
import type { RequestStatus } from '@/lib/schemas/requests'

interface RequestStatusBadgeProps {
  status: RequestStatus
}

export function RequestStatusBadge({ status }: RequestStatusBadgeProps) {
  return (
    <Badge variant={getStatusBadgeVariant(status)}>
      <span className="h-1.5 w-1.5 rounded-full bg-current opacity-70" aria-hidden="true" />
      {requestStatusLabels[status]}
    </Badge>
  )
}
