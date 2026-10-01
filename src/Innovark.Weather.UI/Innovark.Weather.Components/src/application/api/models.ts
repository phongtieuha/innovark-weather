import type * as React from "react"

// ASP.NET Core's problem-details responses (RFC 9457): `ProblemDetails` from `Results.Problem()` and
// the `AddProblemDetails()` middleware, and `HttpValidationProblemDetails` (with `errors`) from
// `Results.ValidationProblem()`. Fields are optional and nullable, as in the OpenAPI document, so the
// types Orval generates from it fit this one.
export interface IApiProblemDetails {
  readonly type?: string | null
  readonly title?: string | null
  readonly status?: number | null
  readonly detail?: string | null
  readonly instance?: string | null
  readonly traceId?: string | null
  readonly errors?: Readonly<Record<string, readonly string[]>>
}

export interface IProblemState {
  // Only a validation problem (400 with `errors`) has field errors.
  readonly fieldErrors?: Readonly<Record<string, readonly string[]>>
  readonly title?: string
  readonly traceId?: string
  // The title, field errors and trace ID, ready for a form alert; empty when there's no problem.
  readonly alertMessage: React.ReactNode
}
