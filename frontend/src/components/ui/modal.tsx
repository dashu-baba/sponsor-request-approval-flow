import { X } from 'lucide-react'
import { type ReactNode, useEffect } from 'react'

interface ModalProps {
  open: boolean
  onClose: () => void
  title: string
  subtitle?: string
  children: ReactNode
  footer?: ReactNode
  maxWidthClassName?: string
}

export function Modal({
  open,
  onClose,
  title,
  subtitle,
  children,
  footer,
  maxWidthClassName = 'max-w-lg',
}: ModalProps) {
  useEffect(() => {
    if (!open) return

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        onClose()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [open, onClose])

  if (!open) {
    return null
  }

  return (
    <div
      className="fixed inset-0 z-[300] flex items-center justify-center overflow-x-hidden bg-[rgba(26,24,48,0.45)] p-3 backdrop-blur-[2px] sm:p-4"
      role="presentation"
      onClick={(event) => {
        if (event.target === event.currentTarget) {
          onClose()
        }
      }}
    >
      <div
        className={`flex max-h-[calc(100dvh-1.5rem)] w-full min-w-0 flex-col overflow-hidden rounded-[14px] border border-border bg-surface shadow-[0_20px_60px_rgba(26,24,48,0.2)] ${maxWidthClassName}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
      >
        <div className="flex shrink-0 items-start justify-between gap-4 border-b border-border px-5 py-4 sm:px-7 sm:py-5">
          <div className="min-w-0">
            <h2 id="modal-title" className="text-base font-semibold text-text-primary">
              {title}
            </h2>
            {subtitle ? <p className="mt-1 text-[13px] text-text-secondary">{subtitle}</p> : null}
          </div>
          <button
            type="button"
            className="shrink-0 rounded p-1 text-text-hint transition-colors hover:text-text-primary"
            onClick={onClose}
            aria-label="Close"
          >
            <X className="h-4 w-4" aria-hidden="true" />
          </button>
        </div>

        <div className="min-h-0 min-w-0 flex-1 overflow-x-hidden overflow-y-auto overscroll-contain [scrollbar-gutter:stable] px-5 py-4 sm:px-7 sm:py-5">
          {children}
        </div>

        {footer ? (
          <div className="flex shrink-0 flex-col-reverse gap-2 border-t border-border px-5 py-4 sm:flex-row sm:justify-end sm:px-7">
            {footer}
          </div>
        ) : null}
      </div>
    </div>
  )
}
