import { cva, type VariantProps } from 'class-variance-authority'
import * as React from 'react'

import { cn } from '@/lib/utils'

const badgeVariants = cva(
  'inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 text-[11px] font-medium',
  {
    variants: {
      variant: {
        default: 'border-border bg-page text-text-secondary',
        draft: 'border-transparent bg-gray-bg text-gray-text',
        pendingManager: 'border-transparent bg-brand-light text-brand-dark',
        pendingFinance: 'border-transparent bg-warning-bg text-warning',
        approved: 'border-transparent bg-success-bg text-success',
        rejected: 'border-transparent bg-danger-bg text-danger',
        cancelled: 'border-transparent bg-gray-bg text-text-hint',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  },
)

export interface BadgeProps
  extends React.HTMLAttributes<HTMLDivElement>, VariantProps<typeof badgeVariants> {}

function Badge({ className, variant, ...props }: BadgeProps) {
  return <div className={cn(badgeVariants({ variant }), className)} {...props} />
}

export { Badge }
