export function formatCurrency(amount: number): string {
  return `RM ${amount.toLocaleString('en-MY', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`
}

export function formatDate(value: string): string {
  return new Date(value).toLocaleDateString('en-MY', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  })
}

export function formatDateTime(value: string): string {
  return new Date(value).toLocaleString('en-MY', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export function formatRequestId(id: number | string): string {
  return `SR-${String(id).padStart(6, '0')}`
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}
