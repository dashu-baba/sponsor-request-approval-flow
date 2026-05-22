import { apiJson } from '@/lib/api/auth-api'
import { setAccessToken } from '@/lib/api/token-store'
import {
  loginResponseSchema,
  userProfileSchema,
  type LoginResponse,
  type UserProfile,
} from '@/lib/schemas/auth'

export interface UpdateProfileRequest {
  displayName: string
  department?: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export async function updateProfile(request: UpdateProfileRequest): Promise<UserProfile> {
  return apiJson(
    '/me/profile',
    {
      method: 'PUT',
      body: JSON.stringify(request),
    },
    userProfileSchema,
  )
}

export async function changePassword(request: ChangePasswordRequest): Promise<LoginResponse> {
  const response = await apiJson(
    '/me/password',
    {
      method: 'PUT',
      body: JSON.stringify(request),
    },
    loginResponseSchema,
  )
  setAccessToken(response.accessToken)
  return response
}
