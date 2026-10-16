export interface IDatePickerProps {
  readonly id?: string
  /** ISO "yyyy-MM-dd" format. */
  readonly value?: string
  readonly onValueChange: (value: string | undefined) => void
  readonly onBlur?: () => void
  readonly placeholder?: string
  readonly disabled?: boolean
  readonly className?: string
  readonly "aria-invalid"?: boolean
}
