import type * as React from "react"
import type {
  Control,
  ControllerFieldState,
  ControllerRenderProps,
  FieldPath,
  FieldValues,
} from "react-hook-form"

export type FormAlertVariant = "success" | "error"

export interface IControlledFieldNode<
  TFieldValues extends FieldValues,
  TName extends FieldPath<TFieldValues> = FieldPath<TFieldValues>,
> {
  readonly name: TName
  readonly label: string
  readonly description?: React.ReactNode
  readonly required?: boolean
  readonly render: (props: {
    readonly field: ControllerRenderProps<TFieldValues, TName>
    readonly fieldState: ControllerFieldState
  }) => React.ReactNode
}

export interface IControlledFieldProps<
  TFieldValues extends FieldValues,
  TName extends FieldPath<TFieldValues> = FieldPath<TFieldValues>,
> {
  readonly node: IControlledFieldNode<TFieldValues, TName>
  readonly control: Control<TFieldValues>
  readonly className?: string
}

export interface IFormAlertProps {
  readonly variant: FormAlertVariant
  readonly children: React.ReactNode
  readonly className?: string
}

export interface IUseFormActionOptions {
  readonly message: React.ReactNode
  readonly variant?: FormAlertVariant
  readonly className?: string
}

export interface IUseFormActionResult {
  readonly FormAlert: React.ReactNode
  readonly isFormAlertOpen: boolean
  readonly setIsFormAlertOpen: React.Dispatch<React.SetStateAction<boolean>>
  readonly openFormAlertAndFocus: () => void
}

// Mirrors ASP.NET Core's built-in problem-details responses (RFC 9457): `ProblemDetails` from
// `Results.Problem()` / the `AddProblemDetails()` exception middleware, and
// `ValidationProblemDetails` (the `errors` variant) from `Results.ValidationProblem()`.
export interface IApiProblemDetails {
  readonly type?: string
  readonly title: string
  readonly status: number
  readonly detail?: string
  readonly instance?: string
  readonly traceId?: string
}

export interface IApiValidationProblemDetails extends IApiProblemDetails {
  readonly errors: Readonly<Record<string, readonly string[]>>
}

export interface IUseApiCallOptions<TRequest = void> {
  readonly request: (variables: TRequest) => Promise<Response>
}

export interface IUseApiCallResult<TResponse, TRequest = void> {
  readonly submit: (variables: TRequest) => void
  readonly isLoading: boolean
  // True until the first `submit()` settles (or after `reset()`) — no data, no error, not loading.
  readonly isIdle: boolean
  readonly isSuccess: boolean
  readonly isError: boolean
  readonly data?: TResponse
  readonly fieldErrors?: Readonly<Record<string, readonly string[]>>
  readonly problem?: IApiProblemDetails
  readonly title?: string
  readonly traceId?: string
  readonly reset: () => void
  readonly alertMessage: React.ReactNode
}
