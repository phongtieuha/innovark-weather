import { useRef } from "react"
import { Controller } from "react-hook-form"
import type { FieldPath, FieldValues } from "react-hook-form"

import { cn } from "@/lib/utils"
import { Field, FieldDescription, FieldError, FieldLabel } from "@/components/forms/field/field"

import type { IControlledFieldProps } from "./models"

const REQUIRED_MARK = (
  <span className="text-muted-foreground">
    <span aria-hidden="true">*</span>
    <span className="sr-only"> (required)</span>
  </span>
)

// Config-driven `Controller` wrapper: owns the Field/label/collapsing-FieldError wiring shared by
// every field in a react-hook-form, while each call site still writes its own input via
// `node.render`.
function ControlledField<
  TFieldValues extends FieldValues,
  TName extends FieldPath<TFieldValues> = FieldPath<TFieldValues>,
>({ node, control, className }: IControlledFieldProps<TFieldValues, TName>) {
  // `fieldState.error` goes undefined the instant `invalid` flips false, so this ref freezes the
  // last real error and keeps it showing while the row collapses, instead of the text vanishing
  // before the animation even starts.
  const lastErrorRef = useRef<{ readonly message?: string } | undefined>(undefined)

  return (
    <Controller
      name={node.name}
      control={control}
      render={({ field, fieldState }) => {
        if (fieldState.error) {
          lastErrorRef.current = fieldState.error
        }

        return (
          <Field data-invalid={fieldState.invalid} className={cn("gap-2", className)}>
            <FieldLabel htmlFor={node.name}>
              {node.label}
              {node.required && REQUIRED_MARK}
            </FieldLabel>
            {node.render({ field, fieldState })}
            {node.description && <FieldDescription>{node.description}</FieldDescription>}
            {/* Stays mounted so the last error can animate out; hidden from assistive tech meanwhile. */}
            <div
              aria-hidden={!fieldState.invalid}
              className={cn(
                "overflow-hidden transition-all duration-200 ease-in-out",
                fieldState.invalid ? "max-h-10 opacity-100" : "max-h-0 opacity-0",
              )}
            >
              <FieldError errors={[fieldState.invalid ? fieldState.error : lastErrorRef.current]} />
            </div>
          </Field>
        )
      }}
    />
  )
}

export { ControlledField }
