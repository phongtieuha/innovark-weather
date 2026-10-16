import { describe, expect, it, vi } from "vitest"
import { render, screen } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import {
  InputGroup,
  InputGroupAddon,
  InputGroupButton,
  InputGroupInput,
  InputGroupText,
  InputGroupTextarea,
} from "@innovark-weather/components"

describe("InputGroup", () => {
  it("renders an input with its addons in a group", () => {
    render(
      <InputGroup aria-label="Temperature">
        <InputGroupInput aria-label="Value" />
        <InputGroupAddon align="inline-end">
          <InputGroupText>°C</InputGroupText>
        </InputGroupAddon>
      </InputGroup>,
    )

    expect(screen.getByRole("group", { name: "Temperature" })).toHaveAttribute(
      "data-slot",
      "input-group",
    )
    expect(screen.getByRole("textbox", { name: "Value" })).toHaveAttribute(
      "data-slot",
      "input-group-control",
    )
    expect(screen.getByText("°C").parentElement).toHaveAttribute("data-align", "inline-end")
  })

  it("puts an addon at the inline start by default", () => {
    render(
      <InputGroup>
        <InputGroupAddon>
          <InputGroupText>Search</InputGroupText>
        </InputGroupAddon>
      </InputGroup>,
    )

    expect(screen.getByText("Search").parentElement).toHaveAttribute("data-align", "inline-start")
  })

  it("focuses the input when its addon is clicked", async () => {
    render(
      <InputGroup>
        <InputGroupInput aria-label="Value" />
        <InputGroupAddon>
          <InputGroupText>°C</InputGroupText>
        </InputGroupAddon>
      </InputGroup>,
    )

    await userEvent.click(screen.getByText("°C"))

    expect(screen.getByRole("textbox", { name: "Value" })).toHaveFocus()
  })

  it("leaves the focus on a button clicked inside an addon", async () => {
    const onClick = vi.fn()
    render(
      <InputGroup>
        <InputGroupInput aria-label="Value" />
        <InputGroupAddon align="inline-end">
          <InputGroupButton onClick={onClick}>Clear</InputGroupButton>
        </InputGroupAddon>
      </InputGroup>,
    )

    await userEvent.click(screen.getByRole("button", { name: "Clear" }))

    expect(onClick).toHaveBeenCalledOnce()
    expect(screen.getByRole("button", { name: "Clear" })).toHaveFocus()
  })

  it("renders a ghost, extra-small, non-submitting button by default", () => {
    render(<InputGroupButton>Clear</InputGroupButton>)

    const button = screen.getByRole("button", { name: "Clear" })
    expect(button).toHaveAttribute("type", "button")
    expect(button).toHaveAttribute("data-variant", "ghost")
    expect(button).toHaveAttribute("data-size", "xs")
  })

  it("applies the button's size", () => {
    render(<InputGroupButton size="icon-sm" aria-label="Open" />)

    expect(screen.getByRole("button", { name: "Open" })).toHaveAttribute("data-size", "icon-sm")
  })

  it("renders a textarea control", () => {
    render(
      <InputGroup>
        <InputGroupTextarea aria-label="Notes" />
      </InputGroup>,
    )

    expect(screen.getByRole("textbox", { name: "Notes" })).toHaveAttribute(
      "data-slot",
      "input-group-control",
    )
  })
})
