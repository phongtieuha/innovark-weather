import { describe, expect, it, vi } from "vitest"
import { render, screen } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { Input } from "@innovark-weather/components"

describe("Input", () => {
  it("renders with the provided value", () => {
    render(<Input aria-label="City" defaultValue="Ho Chi Minh City" />)

    expect(screen.getByRole("textbox", { name: "City" })).toHaveValue("Ho Chi Minh City")
  })

  it("renders the placeholder", () => {
    render(<Input aria-label="Hour" placeholder="e.g. 14" />)

    expect(screen.getByPlaceholderText("e.g. 14")).toBeInTheDocument()
  })

  it("calls onChange as the user types", async () => {
    const onChange = vi.fn()
    render(<Input aria-label="City" onChange={onChange} />)

    await userEvent.type(screen.getByRole("textbox", { name: "City" }), "HCM")

    expect(onChange).toHaveBeenCalledTimes(3)
  })

  it("applies the type prop", () => {
    render(<Input aria-label="Hour" type="number" />)

    expect(screen.getByRole("spinbutton", { name: "Hour" })).toHaveAttribute("type", "number")
  })

  it("is disabled when the disabled prop is set", () => {
    render(<Input aria-label="City" disabled />)

    expect(screen.getByRole("textbox", { name: "City" })).toBeDisabled()
  })

  it("marks the field invalid via aria-invalid", () => {
    render(<Input aria-label="City" aria-invalid />)

    expect(screen.getByRole("textbox", { name: "City" })).toBeInvalid()
  })

  it("merges className", () => {
    render(<Input aria-label="City" className="h-12" />)

    const input = screen.getByRole("textbox", { name: "City" })
    expect(input).toHaveClass("h-12")
    expect(input).not.toHaveClass("h-9")
  })
})
