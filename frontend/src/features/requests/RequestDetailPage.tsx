import { useQuery } from '@tanstack/react-query'
import { AlertTriangle, ArrowLeft, RefreshCw } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'

import { ApproveRejectModal, type ApprovalAction } from '@/features/approvals/ApproveRejectModal'
import { EmptyState, ErrorState, LoadingState } from '@/components/states/query-states'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { CancelRequestModal } from '@/features/requests/CancelRequestModal'
import { RequestAttachmentsSection } from '@/features/requests/RequestAttachmentsSection'
import { RequestFormModal } from '@/features/requests/RequestFormModal'
import { SubmitRequestModal } from '@/features/requests/SubmitRequestModal'
import { RequestHistoryTimeline } from '@/features/requests/RequestHistoryTimeline'
import { RequestStatusBadge } from '@/features/requests/RequestStatusBadge'
import { useCurrentUser } from '@/features/auth/use-auth'
import { ApiError } from '@/lib/api/api-error'
import { getRequest, getRequestHistory } from '@/lib/api/requests-api'
import { formatCurrency, formatDate, formatDateTime, formatRequestId } from '@/lib/format'
import { parseRouteEntityId } from '@/lib/parse-entity-id'
import { queryKeys } from '@/lib/query-client'
import {
  canApproveRequest,
  canCancelRequest,
  canEditRequest,
  canSubmitRequest,
  canUploadAttachments,
} from '@/lib/request-status'
import { Roles } from '@/lib/roles'
import { cn } from '@/lib/utils'

interface PendingModalState {
  action: ApprovalAction
}

export function RequestDetailPage() {
  const { id: idParam } = useParams<{ id: string }>()
  const requestId = parseRouteEntityId(idParam)
  const user = useCurrentUser()
  const [pendingModal, setPendingModal] = useState<PendingModalState | null>(null)
  const [conflictDetected, setConflictDetected] = useState(false)
  const [forbiddenDetected, setForbiddenDetected] = useState(false)
  const [editModalOpen, setEditModalOpen] = useState(false)
  const [cancelModalOpen, setCancelModalOpen] = useState(false)
  const [submitModalOpen, setSubmitModalOpen] = useState(false)

  const detailQuery = useQuery({
    queryKey: queryKeys.requests.detail(requestId ?? ''),
    queryFn: () => {
      if (requestId === null) throw new Error('Request id is required')
      return getRequest(requestId)
    },
    enabled: requestId !== null,
  })

  const historyQuery = useQuery({
    queryKey: queryKeys.requests.history(requestId ?? ''),
    queryFn: () => {
      if (requestId === null) throw new Error('Request id is required')
      return getRequestHistory(requestId)
    },
    enabled: requestId !== null,
  })

  if (requestId === null) {
    return <ErrorState message="Invalid request id in the URL." />
  }

  if (detailQuery.isLoading || historyQuery.isLoading) {
    return (
      <LoadingState
        title="Loading request"
        description="Fetching request details and workflow history…"
        metricCount={0}
        tableRows={4}
      />
    )
  }

  if (detailQuery.isError) {
    const error = detailQuery.error
    const isForbidden = error instanceof ApiError && error.status === 403

    if (isForbidden) {
      return (
        <div className="space-y-4">
          <Alert variant="destructive">
            <AlertTitle>Access denied</AlertTitle>
            <AlertDescription>You do not have permission to view this request.</AlertDescription>
          </Alert>
          <Button type="button" variant="outline" asChild>
            <Link to="/dashboard">
              <ArrowLeft className="h-4 w-4" aria-hidden="true" />
              Back to dashboard
            </Link>
          </Button>
        </div>
      )
    }

    const message =
      error instanceof ApiError
        ? error.message
        : error instanceof Error
          ? error.message
          : 'Unable to load request'

    return <ErrorState message={message} onRetry={() => void detailQuery.refetch()} />
  }

  const request = detailQuery.data
  if (!request) {
    return <EmptyState title="Request not found" description="This request could not be loaded." />
  }

  const isOwnerRequestor = user.role === Roles.Requestor && request.requestorId === user.id
  const canAct =
    !conflictDetected && !forbiddenDetected && canApproveRequest(request.status, user.role)
  const showEdit = isOwnerRequestor && canEditRequest(request.status)
  const showSubmit = isOwnerRequestor && canSubmitRequest(request.status)
  const showCancel = isOwnerRequestor && canCancelRequest(request.status)
  const showUpload = isOwnerRequestor && canUploadAttachments(request.status)

  function refreshRequest() {
    setConflictDetected(false)
    void detailQuery.refetch()
    void historyQuery.refetch()
  }

  return (
    <div className="space-y-6">
      <nav className="text-[13px] text-text-secondary" aria-label="Breadcrumb">
        <Link to="/dashboard" className="text-brand hover:underline">
          Dashboard
        </Link>
        <span className="mx-2 text-text-hint">/</span>
        <span className="font-mono text-text-primary">{formatRequestId(request.id)}</span>
      </nav>

      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-xl font-semibold tracking-tight text-text-primary">
            {request.title}
          </h1>
          <p className="mt-1 text-[13px] text-text-secondary">
            {formatRequestId(request.id)} · Submitted {formatDateTime(request.createdAt)}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <RequestStatusBadge status={request.status} />
          <Button type="button" variant="outline" size="sm" asChild>
            <Link to="/dashboard">
              <ArrowLeft className="h-3.5 w-3.5" aria-hidden="true" />
              Back to list
            </Link>
          </Button>
        </div>
      </div>

      {conflictDetected ? (
        <Alert variant="warning">
          <AlertTriangle className="h-4 w-4" aria-hidden="true" />
          <AlertTitle>This request was already actioned</AlertTitle>
          <AlertDescription>
            Another reviewer updated this request while you were reviewing it. The status may have
            changed — refresh to see the latest state before taking further action.
          </AlertDescription>
        </Alert>
      ) : null}

      {forbiddenDetected ? (
        <Alert variant="destructive">
          <AlertTitle>Action not permitted</AlertTitle>
          <AlertDescription>
            You no longer have permission to approve or reject this request.
          </AlertDescription>
        </Alert>
      ) : null}

      {showEdit || showSubmit || showCancel ? (
        <div className="flex flex-wrap gap-2">
          {showEdit ? (
            <Button type="button" variant="outline" onClick={() => setEditModalOpen(true)}>
              Edit draft
            </Button>
          ) : null}
          {showSubmit ? (
            <Button type="button" variant="success" onClick={() => setSubmitModalOpen(true)}>
              Submit for approval
            </Button>
          ) : null}
          {showCancel ? (
            <Button type="button" variant="destructive" onClick={() => setCancelModalOpen(true)}>
              Cancel request
            </Button>
          ) : null}
        </div>
      ) : null}

      {canAct || conflictDetected ? (
        <div
          className={cn(
            'flex flex-wrap gap-2',
            conflictDetected && 'pointer-events-none opacity-50',
          )}
          aria-disabled={conflictDetected || undefined}
        >
          <Button
            type="button"
            variant="success"
            disabled={!canAct}
            onClick={() => setPendingModal({ action: 'approve' })}
          >
            Approve
          </Button>
          <Button
            type="button"
            variant="destructive"
            disabled={!canAct}
            onClick={() => setPendingModal({ action: 'reject' })}
          >
            Reject
          </Button>
          {conflictDetected ? (
            <Button type="button" variant="default" onClick={refreshRequest}>
              <RefreshCw className="h-3.5 w-3.5" aria-hidden="true" />
              Refresh
            </Button>
          ) : null}
        </div>
      ) : null}

      <div className="grid gap-5 xl:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)]">
        <div className="space-y-5">
          <Card>
            <CardHeader>
              <CardTitle>Request details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-[13px]">
              <DetailRow label="Requestor" value={request.requestorName} />
              <DetailRow label="Department" value={request.department} />
              <DetailRow label="Sponsorship type" value={request.sponsorshipTypeName} />
              <DetailRow label="Event / organisation" value={request.eventName} />
              <DetailRow label="Event date" value={formatDate(request.eventDate)} />
              <DetailRow
                label="Requested amount"
                value={formatCurrency(request.requestedAmount)}
                mono
              />
              <DetailRow label="Purpose" value={request.purpose} plain />
              <DetailRow label="Expected benefit" value={request.expectedBenefit ?? '—'} plain />
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Supporting documents</CardTitle>
            </CardHeader>
            <CardContent>
              <RequestAttachmentsSection requestId={request.id} allowUpload={showUpload} />
            </CardContent>
          </Card>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Workflow history</CardTitle>
          </CardHeader>
          <CardContent>
            {historyQuery.isError ? (
              <ErrorState
                title="Unable to load workflow history"
                message="We could not load the workflow history for this request."
                onRetry={() => void historyQuery.refetch()}
              />
            ) : (
              <RequestHistoryTimeline entries={historyQuery.data ?? []} />
            )}
          </CardContent>
        </Card>
      </div>

      {editModalOpen ? (
        <RequestFormModal
          open
          onClose={() => setEditModalOpen(false)}
          requestId={request.id}
          onSuccess={() => {
            void detailQuery.refetch()
            void historyQuery.refetch()
          }}
        />
      ) : null}

      {cancelModalOpen ? (
        <CancelRequestModal
          open
          onClose={() => setCancelModalOpen(false)}
          requestId={request.id}
          requestTitle={request.title}
          onSuccess={() => {
            void detailQuery.refetch()
            void historyQuery.refetch()
          }}
        />
      ) : null}

      {submitModalOpen ? (
        <SubmitRequestModal
          open
          onClose={() => setSubmitModalOpen(false)}
          requestId={request.id}
          requestTitle={request.title}
          onSuccess={() => {
            void detailQuery.refetch()
            void historyQuery.refetch()
          }}
        />
      ) : null}

      {pendingModal ? (
        <ApproveRejectModal
          open
          onOpenChange={(open) => {
            if (!open) setPendingModal(null)
          }}
          action={pendingModal.action}
          requestId={request.id}
          requestTitle={request.title}
          onSuccess={() => {
            void detailQuery.refetch()
            void historyQuery.refetch()
          }}
          onConflict409={() => {
            setConflictDetected(true)
            void detailQuery.refetch()
            void historyQuery.refetch()
          }}
          onForbidden403={() => setForbiddenDetected(true)}
        />
      ) : null}
    </div>
  )
}

function DetailRow({
  label,
  value,
  mono = false,
  plain = false,
}: {
  label: string
  value: string
  mono?: boolean
  plain?: boolean
}) {
  return (
    <div className="flex flex-col gap-1 border-b border-border pb-3 last:border-b-0 last:pb-0 sm:flex-row sm:items-start sm:justify-between">
      <span className="text-text-hint">{label}</span>
      <span
        className={cn(
          'sm:max-w-[65%] sm:text-right',
          mono && 'font-mono font-medium text-text-primary',
          plain ? 'font-normal text-text-primary' : 'font-medium text-text-primary',
        )}
      >
        {value}
      </span>
    </div>
  )
}
