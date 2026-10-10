import { describe, expect, it } from "vitest"
import { render, screen } from "@testing-library/react"
import { Alert, AlertDescription, AlertTitle } from "@innovark-weather/components"

describe("Alert", () => {
  it("renders its title and description inside an alert", () => {
    render(
      <Alert>
        <AlertTitle>Request failed</AlertTitle>
        <AlertDescription>Try again in a minute.</AlertDescription>
      </Alert>,
    )

    const alert = screen.getByRole("alert")
    expect(alert).toHaveTextContent("Request failed")
    expect(alert).toHaveTextContent("Try again in a minute.")
  })

  it("uses the default variant when none is given", () => {
    render(<Alert>Saved.</Alert>)

    expect(screen.getByRole("alert")).toHaveClass("text-card-foreground")
  })

  it("applies the destructive variant", () => {
    render(<Alert variant="destructive">Failed.</Alert>)

    expect(screen.getByRole("alert")).toHaveClass("text-destructive")
  })

  it("merges className into each part", () => {
    render(
      <Alert className="mt-4">
        <AlertTitle className="line-clamp-none">Title</AlertTitle>
        <AlertDescription className="text-xs">Description</AlertDescription>
      </Alert>,
    )

    expect(screen.getByRole("alert")).toHaveClass("mt-4")
    expect(screen.getByText("Title")).toHaveClass("line-clamp-none")
    expect(screen.getByText("Title")).not.toHaveClass("line-clamp-1")
    expect(screen.getByText("Description")).toHaveClass("text-xs")
  })
})
