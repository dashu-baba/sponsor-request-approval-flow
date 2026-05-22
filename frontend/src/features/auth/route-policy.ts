import { Roles } from '@/lib/roles'

export const adminOnly = [Roles.SystemAdmin] as const

export const requestorOnly = [Roles.Requestor] as const
