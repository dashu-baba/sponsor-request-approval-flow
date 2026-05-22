import { describe, expect, it } from 'vitest'

import { mapRequestMutationFieldErrors } from '@/features/requests/schemas'

describe('mapRequestMutationFieldErrors', () => {
  it('maps API validation keys to form field messages', () => {
    const mapped = mapRequestMutationFieldErrors({
      Title: ['Title is required.'],
      EventDate: ['Event date must be today or later.'],
      'Body.SponsorshipTypeId': ['Sponsorship type is invalid or inactive.'],
    })

    expect(mapped.title).toBe('Title is required.')
    expect(mapped.eventDate).toBe('Event date must be today or later.')
    expect(mapped.sponsorshipTypeId).toBe('Sponsorship type is invalid or inactive.')
  })
})
