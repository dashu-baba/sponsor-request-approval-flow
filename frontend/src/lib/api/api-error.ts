export type ApiFieldErrors = Record<string, string[]>

export class ApiError extends Error {
  readonly status: number
  readonly title?: string
  readonly detail?: string
  readonly fieldErrors?: ApiFieldErrors

  constructor(
    status: number,
    message: string,
    title?: string,
    detail?: string,
    fieldErrors?: ApiFieldErrors,
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.title = title
    this.detail = detail
    this.fieldErrors = fieldErrors
  }
}

function parseFieldErrors(body: object): ApiFieldErrors | undefined {
  if (!('errors' in body) || typeof body.errors !== 'object' || body.errors === null) {
    return undefined
  }

  const fieldErrors: ApiFieldErrors = {}
  for (const [key, value] of Object.entries(body.errors)) {
    if (Array.isArray(value) && value.every((item) => typeof item === 'string')) {
      fieldErrors[key] = value
    }
  }

  return Object.keys(fieldErrors).length > 0 ? fieldErrors : undefined
}

export async function parseProblemResponse(response: Response): Promise<ApiError> {
  let title: string | undefined
  let detail: string | undefined
  let fieldErrors: ApiFieldErrors | undefined

  try {
    const body: unknown = await response.json()
    if (body && typeof body === 'object') {
      if ('title' in body && typeof body.title === 'string') title = body.title
      if ('detail' in body && typeof body.detail === 'string') detail = body.detail
      fieldErrors = parseFieldErrors(body)
    }
  } catch {
    // ignore parse failures
  }

  const message = detail ?? title ?? `Request failed with status ${response.status}`
  return new ApiError(response.status, message, title, detail, fieldErrors)
}
