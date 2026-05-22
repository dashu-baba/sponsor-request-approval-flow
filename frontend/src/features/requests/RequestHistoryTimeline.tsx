import { formatDateTime } from '@/lib/format'
import { formatStatusTransition, requestStatusLabels } from '@/lib/request-status'
import type { WorkflowHistoryEntry } from '@/lib/schemas/requests'
import { cn } from '@/lib/utils'

interface RequestHistoryTimelineProps {
  entries: WorkflowHistoryEntry[]
}

function getTransitionLabel(entry: WorkflowHistoryEntry): string {
  if (entry.fromStatus === 'Draft' && entry.toStatus === 'PendingManagerApproval') {
    return `Submitted: ${requestStatusLabels.Draft} → ${requestStatusLabels.PendingManagerApproval}`
  }

  if (entry.toStatus === 'Approved') {
    return `Approved: ${formatStatusTransition(entry.fromStatus, entry.toStatus)}`
  }

  if (entry.toStatus === 'Rejected') {
    return `Rejected: ${formatStatusTransition(entry.fromStatus, entry.toStatus)}`
  }

  if (entry.toStatus === 'Cancelled') {
    return `Cancelled: ${formatStatusTransition(entry.fromStatus, entry.toStatus)}`
  }

  return formatStatusTransition(entry.fromStatus, entry.toStatus)
}

function getDotClass(entry: WorkflowHistoryEntry): string {
  if (entry.toStatus === 'Approved') return 'bg-success'
  if (entry.toStatus === 'Rejected') return 'bg-danger'
  if (entry.toStatus === 'Cancelled') return 'bg-text-hint'
  return 'bg-brand'
}

export function RequestHistoryTimeline({ entries }: RequestHistoryTimelineProps) {
  if (entries.length === 0) {
    return <p className="text-[13px] text-text-secondary">No workflow history yet.</p>
  }

  return (
    <ol className="space-y-0">
      {entries.map((entry, index) => (
        <li key={entry.id} className="relative flex gap-3 pb-5 last:pb-0">
          {index < entries.length - 1 ? (
            <span
              className="absolute top-3 left-[5px] h-[calc(100%-4px)] w-px bg-border"
              aria-hidden="true"
            />
          ) : null}
          <span
            className={cn(
              'relative z-10 mt-1.5 h-2.5 w-2.5 shrink-0 rounded-full',
              getDotClass(entry),
            )}
            aria-hidden="true"
          />
          <div className="min-w-0 flex-1">
            <p className="text-[13px] font-medium text-text-primary">{getTransitionLabel(entry)}</p>
            <p className="mt-0.5 text-[12px] text-text-hint">
              {entry.actorName} · {formatDateTime(entry.occurredAt)}
            </p>
            {entry.remarks ? (
              <p className="mt-1.5 rounded-[8px] bg-page px-3 py-2 text-[12px] text-text-secondary">
                {entry.remarks}
              </p>
            ) : null}
          </div>
        </li>
      ))}
    </ol>
  )
}
