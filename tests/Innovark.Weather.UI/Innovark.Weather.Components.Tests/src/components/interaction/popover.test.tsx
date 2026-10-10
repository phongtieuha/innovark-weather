import { describe, expect, it } from "vitest"
import { render, screen } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import {
  Popover,
  PopoverContent,
  PopoverDescription,
  PopoverHeader,
  PopoverTitle,
  PopoverTrigger,
} from "@innovark-weather/components"

function LocationPopover({ className }: { readonly className?: string }) {
  return (
    <Popover>
      <PopoverTrigger>Location</PopoverTrigger>
      <PopoverContent aria-label="Location details" className={className}>
        <PopoverHeader>
          <PopoverTitle>Ho Chi Minh City</PopoverTitle>
          <PopoverDescription>UTC+7</PopoverDescription>
        </PopoverHeader>
      </PopoverContent>
    </Popover>
  )
}

describe("Popover", () => {
  it("is closed until its trigger is clicked", () => {
    render(<LocationPopover />)

    expect(screen.getByRole("button", { name: "Location" })).toHaveAttribute(
      "aria-expanded",
      "false",
    )
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument()
  })

  it("opens its content when the trigger is clicked", async () => {
    render(<LocationPopover />)

    await userEvent.click(screen.getByRole("button", { name: "Location" }))

    const dialog = await screen.findByRole("dialog", { name: "Location details" })
    expect(dialog).toHaveTextContent("Ho Chi Minh City")
    expect(dialog).toHaveTextContent("UTC+7")
    expect(screen.getByRole("button", { name: "Location" })).toHaveAttribute(
      "aria-expanded",
      "true",
    )
  })

  it("closes on Escape", async () => {
    render(<LocationPopover />)
    await userEvent.click(screen.getByRole("button", { name: "Location" }))
    await screen.findByRole("dialog")

    await userEvent.keyboard("{Escape}")

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument()
  })

  it("merges className into the content", async () => {
    render(<LocationPopover className="w-auto" />)

    await userEvent.click(screen.getByRole("button", { name: "Location" }))

    const dialog = await screen.findByRole("dialog")
    expect(dialog).toHaveClass("w-auto")
    expect(dialog).not.toHaveClass("w-72")
  })
})
