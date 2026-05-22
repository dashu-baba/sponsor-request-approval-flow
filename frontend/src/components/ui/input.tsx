import * as React from 'react'

import { cn } from '@/lib/utils'

const Input = React.forwardRef<HTMLInputElement, React.ComponentProps<'input'>>(
  ({ className, type, ...props }, ref) => {
    return (
      <input
        type={type}
        className={cn(
          'flex h-10 w-full min-w-0 max-w-full rounded-[8px] border border-border bg-surface px-3 py-2 text-[13px] text-text-primary shadow-none transition-colors file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-text-hint focus-visible:border-brand-mid focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-brand/10 disabled:cursor-not-allowed disabled:opacity-50',
          type === 'date' &&
            'relative [&::-webkit-calendar-picker-indicator]:absolute [&::-webkit-calendar-picker-indicator]:top-1/2 [&::-webkit-calendar-picker-indicator]:right-3 [&::-webkit-calendar-picker-indicator]:-translate-y-1/2 [&::-webkit-calendar-picker-indicator]:cursor-pointer',
          className,
        )}
        ref={ref}
        {...props}
      />
    )
  },
)
Input.displayName = 'Input'

export { Input }
