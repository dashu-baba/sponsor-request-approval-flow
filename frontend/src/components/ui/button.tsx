import { Slot } from '@radix-ui/react-slot'
import { cva, type VariantProps } from 'class-variance-authority'
import * as React from 'react'

import { cn } from '@/lib/utils'

const buttonVariants = cva(
  'inline-flex cursor-pointer items-center justify-center gap-1.5 whitespace-nowrap rounded-[8px] text-[13px] font-medium transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50',
  {
    variants: {
      variant: {
        default:
          'border border-brand bg-brand text-white hover:border-brand-dark hover:bg-brand-dark hover:shadow-[0_2px_8px_rgba(74,63,200,0.25)]',
        outline:
          'border border-border bg-transparent text-text-primary hover:border-border-strong hover:bg-page',
        ghost:
          'border border-transparent bg-transparent text-text-secondary hover:bg-page hover:text-text-primary',
        destructive:
          'border border-danger bg-danger text-white hover:border-danger hover:bg-danger/90',
        success:
          'border border-success bg-success text-white hover:border-success hover:bg-success/90',
        link: 'border-transparent text-brand underline-offset-4 hover:underline',
      },
      size: {
        default: 'h-9 px-3.5 py-2',
        sm: 'h-8 px-3 text-xs',
        lg: 'h-10 px-5',
        icon: 'h-[34px] w-[34px] rounded-[8px] border border-border p-0 text-text-secondary hover:border-border-strong hover:bg-page hover:text-text-primary',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  },
)

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>, VariantProps<typeof buttonVariants> {
  asChild?: boolean
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : 'button'
    return (
      <Comp className={cn(buttonVariants({ variant, size, className }))} ref={ref} {...props} />
    )
  },
)
Button.displayName = 'Button'

export { Button }
