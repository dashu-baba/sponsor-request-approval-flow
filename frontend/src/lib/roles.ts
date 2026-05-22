export const Roles = {
  Requestor: 'Requestor',
  Manager: 'Manager',
  FinanceAdmin: 'FinanceAdmin',
  SystemAdmin: 'SystemAdmin',
} as const

export type Role = (typeof Roles)[keyof typeof Roles]

const roleLabels: Record<Role, string> = {
  [Roles.Requestor]: 'Requestor',
  [Roles.Manager]: 'Manager',
  [Roles.FinanceAdmin]: 'Finance Admin',
  [Roles.SystemAdmin]: 'System Admin',
}

export function getRoleLabel(role: Role): string {
  return roleLabels[role]
}

export function isRole(value: string): value is Role {
  return Object.values(Roles).includes(value as Role)
}
