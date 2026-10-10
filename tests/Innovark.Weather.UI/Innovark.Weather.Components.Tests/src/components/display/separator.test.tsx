import { describe, expect, it } from "vitest"
import { render, screen } from "@testing-library/react"
import { Separator } from "@innovark-weather/components"

describe("Separator", () => {
  it("is decorative and horizontal by default", () => {
    const { container } = render(<Separator />)

    const separator = container.querySelector("[data-slot='separator']")
    expect(separator).toHaveAttribute("data-orientation", "horizontal")
    expect(separator).toHaveAttribute("role", "none")
    expect(screen.queryByRole("separator")).not.toBeInTheDocument()
  })

  it("is announced as a vertical separator when not decorative", () => {
    render(<Separator orientation="vertical" decorative={false} />)

    const separator = screen.getByRole("separator")
    expect(separator).toHaveAttribute("aria-orientation", "vertical")
    expect(separator).toHaveAttribute("data-orientation", "vertical")
  })

  it("merges className", () => {
    const { container } = render(<Separator className="my-4" />)

    expect(container.querySelector("[data-slot='separator']")).toHaveClass("bg-border", "my-4")
  })
})
