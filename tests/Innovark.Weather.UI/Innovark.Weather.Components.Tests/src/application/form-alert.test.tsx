import { describe, expect, it } from "vitest"
import { render, screen } from "@testing-library/react"
import { FormAlert } from "@innovark-weather/components"

describe("FormAlert", () => {
  it("renders its children inside an alert", () => {
    render(<FormAlert variant="success">Saved.</FormAlert>)

    expect(screen.getByRole("alert")).toHaveTextContent("Saved.")
  })

  it("uses the destructive alert for the error variant", () => {
    render(<FormAlert variant="error">Something went wrong.</FormAlert>)

    expect(screen.getByRole("alert")).toHaveClass("text-destructive")
  })

  it("uses the default alert for the success variant", () => {
    render(<FormAlert variant="success">Saved.</FormAlert>)

    expect(screen.getByRole("alert")).not.toHaveClass("text-destructive")
  })

  it("is expanded once mounted, so the open transition plays", () => {
    const { container } = render(<FormAlert variant="success">Saved.</FormAlert>)

    expect(container.firstChild).toHaveClass("max-h-40", "opacity-100")
  })

  it("applies className to its wrapper", () => {
    const { container } = render(
      <FormAlert variant="success" className="w-full">
        Saved.
      </FormAlert>,
    )

    expect(container.firstChild).toHaveClass("w-full")
  })
})
