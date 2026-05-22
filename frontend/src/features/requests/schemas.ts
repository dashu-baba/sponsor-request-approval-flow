import { z } from 'zod'

const maxRequestedAmount = 1_000_000

function todayDateString(): string {
  const now = new Date()
  const year = now.getFullYear()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export const requestMutationSchema = z.object({
  title: z
    .string()
    .trim()
    .min(1, 'Title is required.')
    .max(200, 'Title must be at most 200 characters.'),
  department: z.string().trim().min(1, 'Department is required.').max(120),
  sponsorshipTypeId: z.coerce.number().int().positive('Select a sponsorship type.'),
  eventName: z
    .string()
    .trim()
    .min(1, 'Event or organisation is required.')
    .max(200, 'Event name must be at most 200 characters.'),
  eventDate: z
    .string()
    .min(1, 'Event date is required.')
    .refine((value) => value >= todayDateString(), {
      message: 'Event date must be today or later.',
    }),
  requestedAmount: z.coerce
    .number({ error: 'Amount is required.' })
    .positive('Amount must be greater than zero.')
    .max(maxRequestedAmount, `Amount must be at most ${maxRequestedAmount.toLocaleString()}.`),
  purpose: z
    .string()
    .trim()
    .min(1, 'Purpose is required.')
    .max(4000, 'Purpose must be at most 4000 characters.'),
  expectedBenefit: z
    .string()
    .trim()
    .max(4000, 'Expected benefit must be at most 4000 characters.')
    .transform((value) => (value.length > 0 ? value : null)),
  remarks: z
    .string()
    .trim()
    .max(4000)
    .transform((value) => (value.length > 0 ? value : null))
    .optional(),
})

export type RequestMutationFormValues = z.input<typeof requestMutationSchema>
export type RequestMutationValues = z.output<typeof requestMutationSchema>

export const ATTACHMENT_MAX_SIZE_BYTES = 10 * 1024 * 1024

export const ATTACHMENT_ACCEPT =
  '.pdf,.doc,.docx,.jpg,.jpeg,.png,.gif,.webp,application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document,image/jpeg,image/png,image/gif,image/webp'

export const ATTACHMENT_HINT = 'PDF, Word, or images · 10 MB max per file · Multiple files allowed'

const apiFieldToFormField: Record<string, keyof RequestMutationFormValues> = {
  Title: 'title',
  Department: 'department',
  SponsorshipTypeId: 'sponsorshipTypeId',
  EventName: 'eventName',
  EventDate: 'eventDate',
  RequestedAmount: 'requestedAmount',
  Purpose: 'purpose',
  ExpectedBenefit: 'expectedBenefit',
  Remarks: 'remarks',
  'Body.Title': 'title',
  'Body.Department': 'department',
  'Body.SponsorshipTypeId': 'sponsorshipTypeId',
  'Body.EventName': 'eventName',
  'Body.EventDate': 'eventDate',
  'Body.RequestedAmount': 'requestedAmount',
  'Body.Purpose': 'purpose',
  'Body.ExpectedBenefit': 'expectedBenefit',
  'Body.Remarks': 'remarks',
}

export function mapRequestMutationFieldErrors(
  fieldErrors: Record<string, string[]>,
): Partial<Record<keyof RequestMutationFormValues, string>> {
  const mapped: Partial<Record<keyof RequestMutationFormValues, string>> = {}

  for (const [apiField, messages] of Object.entries(fieldErrors)) {
    const formField = apiFieldToFormField[apiField]
    if (formField && messages[0]) {
      mapped[formField] = messages[0]
    }
  }

  return mapped
}
