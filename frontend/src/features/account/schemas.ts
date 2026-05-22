import { z } from 'zod'

export const profileUpdateSchema = z.object({
  displayName: z.string().trim().min(1, 'Display name is required').max(120),
  department: z
    .string()
    .trim()
    .max(120)
    .optional()
    .transform((value) => (value === '' ? undefined : value)),
})

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Current password is required'),
    newPassword: z
      .string()
      .min(8, 'Password must be at least 8 characters')
      .regex(/[a-z]/, 'Password must include a lowercase letter')
      .regex(/[A-Z]/, 'Password must include an uppercase letter')
      .regex(/\d/, 'Password must include a digit'),
    confirmPassword: z.string().min(1, 'Please confirm your new password'),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  })

export type ProfileUpdateValues = z.input<typeof profileUpdateSchema>
export type ProfileUpdateInput = z.output<typeof profileUpdateSchema>
export type ChangePasswordValues = z.infer<typeof changePasswordSchema>
