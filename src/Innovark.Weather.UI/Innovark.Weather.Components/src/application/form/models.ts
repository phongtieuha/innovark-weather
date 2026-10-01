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

// TTransformedValues: what the form's resolver turns valid values into (e.g. a zod schema that
// converts the inputs' strings into an API request); the inputs' own type when it doesn't.
export interface IControlledFieldProps<
  TFieldValues extends FieldValues,
  TName extends FieldPath<TFieldValues> = FieldPath<TFieldValues>,
  TTransformedValues = TFieldValues,
> {
  readonly node: IControlledFieldNode<TFieldValues, TName>
  readonly control: Control<TFieldValues, unknown, TTransformedValues>
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
