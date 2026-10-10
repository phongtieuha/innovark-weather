import { describe, expect, it } from "vitest"
import { render, screen } from "@testing-library/react"
import { Label } from "@innovark-weather/components"

describe("Label", () => {
  it("labels the control it points at", () => {
    render(
      <>
        <Label htmlFor="hour">Hour</Label>
        <input id="hour" />
      </>,
    )

    expect(screen.getByLabelText("Hour")).toHaveAttribute("id", "hour")
  })

  it("merges className", () => {
    render(<Label className="text-base">Hour</Label>)

    const label = screen.getByText("Hour")
    expect(label).toHaveClass("text-base")
    expect(label).not.toHaveClass("text-sm")
  })
})
