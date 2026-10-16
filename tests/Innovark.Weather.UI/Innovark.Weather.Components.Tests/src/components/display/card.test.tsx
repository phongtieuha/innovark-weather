import { describe, expect, it } from "vitest"
import { render, screen } from "@testing-library/react"
import {
  Card,
  CardAction,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@innovark-weather/components"

describe("Card", () => {
  it("renders every part with its slot", () => {
    const { container } = render(
      <Card>
        <CardHeader>
          <CardTitle>Weather history</CardTitle>
          <CardDescription>Ho Chi Minh City</CardDescription>
          <CardAction>Action</CardAction>
        </CardHeader>
        <CardContent>Content</CardContent>
        <CardFooter>Footer</CardFooter>
      </Card>,
    )

    const slots = [...container.querySelectorAll("[data-slot]")].map((el) =>
      el.getAttribute("data-slot"),
    )
    expect(slots).toEqual([
      "card",
      "card-header",
      "card-title",
      "card-description",
      "card-action",
      "card-content",
      "card-footer",
    ])
    expect(screen.getByText("Weather history")).toBeInTheDocument()
    expect(screen.getByText("Ho Chi Minh City")).toBeInTheDocument()
  })

  it("merges className and forwards other props", () => {
    render(
      <Card className="max-w-md" aria-label="Weather">
        <CardContent className="px-0">Content</CardContent>
      </Card>,
    )

    const card = screen.getByLabelText("Weather")
    expect(card).toHaveClass("max-w-md")
    expect(screen.getByText("Content")).toHaveClass("px-0")
    expect(screen.getByText("Content")).not.toHaveClass("px-6")
  })
})
