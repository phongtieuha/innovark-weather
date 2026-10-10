import { describe, expect, it } from "vitest"
import { render, screen } from "@testing-library/react"
import { Spinner } from "@innovark-weather/components"

describe("Spinner", () => {
  it("is announced as a loading status", () => {
    render(<Spinner />)

    expect(screen.getByRole("status", { name: "Loading" })).toBeInTheDocument()
  })

  it("spins, and merges className", () => {
    render(<Spinner className="size-6" />)

    const spinner = screen.getByRole("status")
    expect(spinner).toHaveClass("animate-spin", "size-6")
    expect(spinner).not.toHaveClass("size-4")
  })

  it("lets the accessible name be overridden", () => {
    render(<Spinner aria-label="Sending" />)

    expect(screen.getByRole("status", { name: "Sending" })).toBeInTheDocument()
  })
})
