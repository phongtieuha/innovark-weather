import { useEffect, useRef, useState } from "react"
import type * as React from "react"
import { format, isValid, parse } from "date-fns"
import { CalendarIcon } from "lucide-react"
import { IMaskMixin } from "react-imask"

import {
  InputGroup,
  InputGroupAddon,
  InputGroupButton,
  InputGroupInput,
} from "@/components/forms/input-group/input-group"
import {
  Popover,
  PopoverAnchor,
  PopoverContent,
  PopoverTrigger,
} from "@/components/interaction/popover/popover"

import { Calendar } from "./calendar"
import type { IDatePickerProps } from "./models"

const DISPLAY_FORMAT = "dd/MM/yyyy"
const ISO_FORMAT = "yyyy-MM-dd"
const MASK_PATTERN = "d/`m/`Y"

// `IMaskMixin` wraps InputGroupInput so the masking behavior stays separate from styling — it
// injects `inputRef` (the actual DOM node), re-mapped here to `ref`.
const MaskedInputGroupInput = IMaskMixin(
  // eslint-disable-next-line @typescript-eslint/no-explicit-any -- react-imask's mixin props are untyped
  ({ inputRef, ...props }: any) => <InputGroupInput {...props} ref={inputRef} />,
)

// `value`/`onValueChange` only ever deal in "yyyy-MM-dd" strings — `Date` is purely an internal
// detail for talking to date-fns/Calendar/the mask.
function toDate(input: string | undefined): Date | undefined {
  if (!input) {
    return undefined
  }
  const parsed = parse(input, ISO_FORMAT, new Date())
  return isValid(parsed) ? parsed : undefined
}

function DatePicker({
  id,
  value,
  onValueChange,
  onBlur,
  placeholder = "dd/mm/yyyy",
  disabled = false,
  className,
  ...props
}: IDatePickerProps) {
  const dateValue = toDate(value)
  const [open, setOpen] = useState(false)
  const [month, setMonth] = useState(dateValue ?? new Date())
  // eslint-disable-next-line @typescript-eslint/no-explicit-any -- react-imask's ref type is untyped
  const maskRef = useRef<any>(null)

  // Keeps the masked text in sync when `value` changes from outside (calendar pick, form reset).
  useEffect(() => {
    const formatted = dateValue ? format(dateValue, DISPLAY_FORMAT) : ""
    const instance = maskRef.current?.maskRef
    if (instance && instance.value !== formatted) {
      instance.value = formatted
    }
    if (dateValue) {
      setMonth(dateValue)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- `dateValue` is derived from `value`
  }, [value])

  function handleAccept(rawText: string) {
    // Commit as soon as the typed text forms a full valid date, so the calendar's displayed month
    // stays in sync while typing rather than only updating on blur/Enter.
    const parsed = parse(rawText.trim(), DISPLAY_FORMAT, new Date())
    if (isValid(parsed)) {
      onValueChange(format(parsed, ISO_FORMAT))
    }
  }

  function commitOrRevert(rawText: string) {
    const trimmed = rawText.trim()
    const parsed = parse(trimmed, DISPLAY_FORMAT, new Date())
    if (!trimmed || !isValid(parsed)) {
      // A partly typed date is not a date: clear it, so validation reports it as missing.
      onValueChange(undefined)
      const instance = maskRef.current?.maskRef
      if (instance) {
        instance.value = ""
      }
    }
  }

  return (
    <Popover open={open} onOpenChange={(nextOpen) => !disabled && setOpen(nextOpen)}>
      <PopoverAnchor asChild>
        <InputGroup className={className} data-disabled={disabled}>
          <MaskedInputGroupInput
            ref={maskRef}
            id={id}
            mask={Date}
            pattern={MASK_PATTERN}
            format={(date: Date) => format(date, DISPLAY_FORMAT)}
            parse={(text: string) => parse(text, DISPLAY_FORMAT, new Date())}
            defaultValue={dateValue ? format(dateValue, DISPLAY_FORMAT) : ""}
            disabled={disabled}
            placeholder={placeholder}
            aria-invalid={props["aria-invalid"]}
            onAccept={(nextValue: string) => handleAccept(nextValue)}
            onBlur={(event: React.FocusEvent<HTMLInputElement>) => {
              commitOrRevert(event.target.value)
              onBlur?.()
            }}
            onKeyDown={(event: React.KeyboardEvent<HTMLInputElement>) => {
              if (event.key === "Enter") {
                commitOrRevert(event.currentTarget.value)
              }
            }}
          />
          <InputGroupAddon align="inline-end">
            <PopoverTrigger asChild>
              <InputGroupButton
                size="icon-xs"
                disabled={disabled}
                aria-label="Open calendar"
                title="Open calendar"
              >
                <CalendarIcon />
              </InputGroupButton>
            </PopoverTrigger>
          </InputGroupAddon>
        </InputGroup>
      </PopoverAnchor>
      <PopoverContent align="end" aria-label="Choose a date" className="w-auto overflow-hidden p-0">
        <Calendar
          mode="single"
          captionLayout="dropdown"
          selected={dateValue}
          month={month}
          onMonthChange={setMonth}
          onSelect={(date) => {
            onValueChange(date ? format(date, ISO_FORMAT) : undefined)
            setOpen(false)
          }}
        />
      </PopoverContent>
    </Popover>
  )
}

export { DatePicker }
