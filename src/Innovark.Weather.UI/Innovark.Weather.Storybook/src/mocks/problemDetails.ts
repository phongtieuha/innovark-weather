// The problem-details shapes ASP.NET Core emits (RFC 9457), so mocked errors look exactly like the
// real backend's: `Results.Problem()` for generic errors, `Results.ValidationProblem()` for field
// errors.

export interface IProblemDetails {
  readonly type: string
  readonly title: string
  readonly status: number
  readonly detail?: string
  readonly traceId: string
}

export interface IValidationProblemDetails extends IProblemDetails {
  readonly errors: Readonly<Record<string, readonly string[]>>
}

function randomTraceId(): string {
  const traceId = crypto.randomUUID().replace(/-/g, "")
  const spanId = crypto.randomUUID().replace(/-/g, "").slice(0, 16)
  return `00-${traceId}-${spanId}-00`
}

export function serverErrorProblem(title: string, detail?: string): IProblemDetails {
  return {
    type: "https://tools.ietf.org/html/rfc9110#section-15.6.1",
    title,
    status: 500,
    detail,
    traceId: randomTraceId(),
  }
}

export function validationProblem(errors: Record<string, string[]>): IValidationProblemDetails {
  return {
    type: "https://tools.ietf.org/html/rfc9110#section-15.5.1",
    title: "One or more validation errors occurred.",
    status: 400,
    errors,
    traceId: randomTraceId(),
  }
}
