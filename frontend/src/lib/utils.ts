import { type ClassValue, clsx } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function getInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return '?'
  if (parts.length === 1) {
    const [first] = parts
    return first ? first.slice(0, 2).toUpperCase() : '?'
  }
  const first = parts[0]
  const last = parts[parts.length - 1]
  if (!first || !last) return '?'
  return `${first[0] ?? ''}${last[0] ?? ''}`.toUpperCase()
}
