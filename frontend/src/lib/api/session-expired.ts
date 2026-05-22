let sessionExpiredHandler: (() => void) | null = null

export function setSessionExpiredHandler(handler: (() => void) | null): void {
  sessionExpiredHandler = handler
}

export function notifySessionExpired(): void {
  sessionExpiredHandler?.()
}
