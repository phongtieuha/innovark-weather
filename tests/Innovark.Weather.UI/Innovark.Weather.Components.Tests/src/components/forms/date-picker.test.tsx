import { useState } from "react"
import { describe, expect, it, vi } from "vitest"
import { render, screen, waitFor } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { DatePicker } from "@innovark-weather/components"

// A parent holding the value, as a form does, so the picker sees its own changes come back.
function ControlledDatePicker({
  initialValue,
  onValueChange,
}: {
  readonly initialValue?: string
  readonly onValueChange?: (value: string | undefined) => void
}) {
  const [value, setValue] = useState(initialValue)

  return (
    <>
      <label htmlFor="date">Date</label>
      <DatePicker
        id="date"
        value={value}
        onValueChange={(next) => {
          setValue(next)
          onValueChange?.(next)
        }}
      />
      <button onClick={() => setValue(undefined)}>Reset</button>
      <output>{value ?? "none"}</output>
    </>
  )
}

describe("DatePicker", () => {
  it("renders an empty input with the default placeholder", () => {
    render(<DatePicker value={undefined} onValueChange={vi.fn()} />)

    expect(screen.getByPlaceholderText("dd/mm/yyyy")).toHaveValue("")
  })

  it("renders a custom placeholder", () => {
    render(<DatePicker value={undefined} onValueChange={vi.fn()} placeholder="Pick a date" />)

    expect(screen.getByPlaceholderText("Pick a date")).toBeInTheDocument()
  })

  it("shows the ISO value as dd/MM/yyyy", () => {
    render(<DatePicker value="2026-09-28" onValueChange={vi.fn()} />)

    expect(screen.getByRole("textbox")).toHaveValue("28/09/2026")
  })

  it("shows an invalid ISO value as empty", () => {
    render(<DatePicker value="not-a-date" onValueChange={vi.fn()} />)

    expect(screen.getByRole("textbox")).toHaveValue("")
  })

  it("commits the ISO date as soon as a full valid date is typed", async () => {
    const onValueChange = vi.fn()
    render(<DatePicker value={undefined} onValueChange={onValueChange} />)

    await userEvent.type(screen.getByRole("textbox"), "28092026")

    await waitFor(() => expect(onValueChange).toHaveBeenCalledWith("2026-09-28"))
  })

  it("clears a partly typed date on blur, and calls onBlur", async () => {
    const onValueChange = vi.fn()
    const onBlur = vi.fn()
    render(<DatePicker value={undefined} onValueChange={onValueChange} onBlur={onBlur} />)
    const input = screen.getByRole("textbox")

    await userEvent.type(input, "2809")
    await userEvent.tab()

    expect(onValueChange).toHaveBeenLastCalledWith(undefined)
    expect(input).toHaveValue("")
    expect(onBlur).toHaveBeenCalledOnce()
  })

  it("clears a partly typed date on Enter", async () => {
    const onValueChange = vi.fn()
    render(<DatePicker value={undefined} onValueChange={onValueChange} />)
    const input = screen.getByRole("textbox")

    await userEvent.type(input, "28{Enter}")

    expect(onValueChange).toHaveBeenLastCalledWith(undefined)
    expect(input).toHaveValue("")
  })

  it("keeps a complete date on blur", async () => {
    render(<ControlledDatePicker />)
    const input = screen.getByLabelText("Date")

    await userEvent.type(input, "28092026")
    await userEvent.tab()

    expect(input).toHaveValue("28/09/2026")
    expect(screen.getByRole("status")).toHaveTextContent("2026-09-28")
  })

  it("updates the text when the value changes from outside", async () => {
    render(<ControlledDatePicker initialValue="2026-09-28" />)

    await userEvent.click(screen.getByRole("button", { name: "Reset" }))

    expect(screen.getByLabelText("Date")).toHaveValue("")
  })

  it("opens the calendar on the selected date's month", async () => {
    render(<DatePicker value="2026-09-28" onValueChange={vi.fn()} />)

    await userEvent.click(screen.getByRole("button", { name: "Open calendar" }))

    expect(await screen.findByRole("dialog", { name: "Choose a date" })).toBeInTheDocument()
    expect(screen.getByRole("button", { name: /September 28th, 2026/ })).toHaveAttribute(
      "data-selected-single",
      "true",
    )
  })

  it("commits a day picked in the calendar and closes it", async () => {
    render(<ControlledDatePicker initialValue="2026-09-28" />)

    await userEvent.click(screen.getByRole("button", { name: "Open calendar" }))
    await userEvent.click(await screen.findByRole("button", { name: /September 27th, 2026/ }))

    expect(screen.getByRole("status")).toHaveTextContent("2026-09-27")
    expect(screen.getByLabelText("Date")).toHaveValue("27/09/2026")
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument()
  })

  it("clears the value when the selected day is picked again", async () => {
    const onValueChange = vi.fn()
    render(<DatePicker value="2026-09-28" onValueChange={onValueChange} />)

    await userEvent.click(screen.getByRole("button", { name: "Open calendar" }))
    await userEvent.click(await screen.findByRole("button", { name: /September 28th, 2026/ }))

    expect(onValueChange).toHaveBeenCalledWith(undefined)
  })

  it("disables the input and the calendar button", async () => {
    render(<DatePicker value="2026-09-28" onValueChange={vi.fn()} disabled />)

    expect(screen.getByRole("textbox")).toBeDisabled()
    const button = screen.getByRole("button", { name: "Open calendar" })
    expect(button).toBeDisabled()

    await userEvent.click(button)

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument()
  })

  it("marks the input invalid via aria-invalid", () => {
    render(<DatePicker value={undefined} onValueChange={vi.fn()} aria-invalid />)

    expect(screen.getByRole("textbox")).toBeInvalid()
  })
})
