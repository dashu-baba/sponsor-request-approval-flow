import { Navigate, useParams } from 'react-router-dom'

export function AdminRequestLegacyRedirect() {
  const { id } = useParams<{ id: string }>()
  return <Navigate to={`/dashboard/requests/${id ?? ''}`} replace />
}
