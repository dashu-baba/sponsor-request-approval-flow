export class ApiError extends Error {
  readonly status: number
  readonly title?: string
  readonly detail?: string

  constructor(status: number, message: string, title?: string, detail?: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.title = title
    this.detail = detail
  }
}

export async function parseProblemResponse(response: Response): Promise<ApiError> {
  let title: string | undefined
  let detail: string | undefined

  try {
    const body: unknown = await response.json()
    if (body && typeof body === 'object') {
      if ('title' in body && typeof body.title === 'string') title = body.title
      if ('detail' in body && typeof body.detail === 'string') detail = body.detail
    }
  } catch {
    // ignore parse failures
  }

  const message = detail ?? title ?? `Request failed with status ${response.status}`
  return new ApiError(response.status, message, title, detail)
}
