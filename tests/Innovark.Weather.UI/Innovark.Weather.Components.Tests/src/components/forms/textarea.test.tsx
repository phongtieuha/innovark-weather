import { describe, expect, it } from "vitest"
import { render, screen } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { Textarea } from "@innovark-weather/components"

describe("Textarea", () => {
  it("lets the user type several lines", async () => {
    render(<Textarea aria-label="Notes" />)

    await userEvent.type(screen.getByRole("textbox", { name: "Notes" }), "Hot{enter}Humid")

    expect(screen.getByRole("textbox", { name: "Notes" })).toHaveValue("Hot\nHumid")
  })

  it("is disabled when the disabled prop is set", () => {
    render(<Textarea aria-label="Notes" disabled />)

    expect(screen.getByRole("textbox", { name: "Notes" })).toBeDisabled()
  })

  it("marks the field invalid via aria-invalid", () => {
    render(<Textarea aria-label="Notes" aria-invalid />)

    expect(screen.getByRole("textbox", { name: "Notes" })).toBeInvalid()
  })

  it("merges className", () => {
    render(<Textarea aria-label="Notes" className="min-h-32" />)

    const textarea = screen.getByRole("textbox", { name: "Notes" })
    expect(textarea).toHaveClass("min-h-32")
    expect(textarea).not.toHaveClass("min-h-16")
  })
})
