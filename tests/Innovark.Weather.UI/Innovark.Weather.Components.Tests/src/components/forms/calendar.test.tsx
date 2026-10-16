import { describe, expect, it, vi } from "vitest"
import { render, screen } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { Calendar } from "@innovark-weather/components"

const SEPTEMBER_2026 = new Date(2026, 8, 1)

describe("Calendar", () => {
  it("shows the given month", () => {
    render(<Calendar mode="single" defaultMonth={SEPTEMBER_2026} />)

    expect(screen.getByRole("grid", { name: "September 2026" })).toBeInTheDocument()
  })

  it("marks the selected day", () => {
    render(
      <Calendar mode="single" defaultMonth={SEPTEMBER_2026} selected={new Date(2026, 8, 28)} />,
    )

    expect(screen.getByRole("button", { name: /September 28th, 2026/ })).toHaveAttribute(
      "data-selected-single",
      "true",
    )
  })

  it("calls onSelect with the clicked day", async () => {
    const onSelect = vi.fn()
    render(<Calendar mode="single" defaultMonth={SEPTEMBER_2026} onSelect={onSelect} />)

    await userEvent.click(screen.getByRole("button", { name: /September 15th, 2026/ }))

    expect(onSelect).toHaveBeenCalledWith(
      new Date(2026, 8, 15),
      expect.anything(),
      expect.anything(),
      expect.anything(),
    )
  })

  it("moves to the next and previous months", async () => {
    render(<Calendar mode="single" defaultMonth={SEPTEMBER_2026} />)

    await userEvent.click(screen.getByRole("button", { name: /next month/i }))
    expect(screen.getByRole("grid", { name: "October 2026" })).toBeInTheDocument()

    await userEvent.click(screen.getByRole("button", { name: /previous month/i }))
    await userEvent.click(screen.getByRole("button", { name: /previous month/i }))
    expect(screen.getByRole("grid", { name: "August 2026" })).toBeInTheDocument()
  })

  it("shows days outside the month by default", () => {
    render(<Calendar mode="single" defaultMonth={SEPTEMBER_2026} />)

    expect(screen.getByRole("button", { name: /August 31st, 2026/ })).toBeInTheDocument()
  })

  it("hides days outside the month when showOutsideDays is false", () => {
    render(<Calendar mode="single" defaultMonth={SEPTEMBER_2026} showOutsideDays={false} />)

    expect(screen.queryByRole("button", { name: /August 31st, 2026/ })).not.toBeInTheDocument()
  })

  it("renders month and year dropdowns with the dropdown caption", () => {
    render(
      <Calendar
        mode="single"
        defaultMonth={SEPTEMBER_2026}
        captionLayout="dropdown"
        startMonth={new Date(2026, 0, 1)}
        endMonth={new Date(2026, 11, 1)}
      />,
    )

    expect(screen.getByRole("combobox", { name: /month/i })).toHaveDisplayValue("Sep")
    expect(screen.getByRole("combobox", { name: /year/i })).toHaveDisplayValue("2026")
  })

  it("renders week numbers when asked", () => {
    render(<Calendar mode="single" defaultMonth={SEPTEMBER_2026} showWeekNumber />)

    expect(screen.getByRole("rowheader", { name: /week 36/i })).toBeInTheDocument()
  })

  it("does not select a disabled day", async () => {
    const onSelect = vi.fn()
    render(
      <Calendar
        mode="single"
        defaultMonth={SEPTEMBER_2026}
        disabled={{ after: new Date(2026, 8, 28) }}
        onSelect={onSelect}
      />,
    )

    const day = screen.getByRole("button", { name: /September 29th, 2026/ })
    await userEvent.click(day)

    expect(day).toBeDisabled()
    expect(onSelect).not.toHaveBeenCalled()
  })
})
