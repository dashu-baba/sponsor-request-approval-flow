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
      className="fixed inset-0 z-50 flex items-center justify-center bg-[rgba(26,24,48,0.45)] p-4 backdrop-blur-[2px]"
      role="presentation"
      onClick={(event) => {
        if (event.target === event.currentTarget) {
          onClose()
        }
      }}
    >
      <div
        className={`w-full ${maxWidthClassName} rounded-[14px] border border-border bg-surface p-7 shadow-[0_20px_60px_rgba(26,24,48,0.2)]`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
      >
        <div className="mb-5 flex items-start justify-between gap-4">
          <div>
            <h2 id="modal-title" className="text-base font-semibold text-text-primary">
              {title}
            </h2>
            {subtitle ? <p className="mt-1 text-[13px] text-text-secondary">{subtitle}</p> : null}
          </div>
          <button
            type="button"
            className="rounded p-1 text-text-hint transition-colors hover:text-text-primary"
            onClick={onClose}
            aria-label="Close"
          >
            <X className="h-4 w-4" aria-hidden="true" />
          </button>
        </div>

        {children}

        {footer ? <div className="mt-6 flex justify-end gap-2">{footer}</div> : null}
      </div>
    </div>
  )
}
