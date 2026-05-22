import * as React from 'react'

import { cn } from '@/lib/utils'

function Skeleton({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div className={cn('animate-pulse rounded-[8px] bg-brand-light/70', className)} {...props} />
  )
}

export { Skeleton }
