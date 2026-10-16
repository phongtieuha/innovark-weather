import { describe, expect, it, vi } from "vitest"
import { render, screen } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { Button, buttonVariants } from "@innovark-weather/components"

describe("Button", () => {
  it("uses the default variant and size", () => {
    render(<Button>Send</Button>)

    const button = screen.getByRole("button", { name: "Send" })
    expect(button).toHaveAttribute("data-slot", "button")
    expect(button).toHaveAttribute("data-variant", "default")
    expect(button).toHaveAttribute("data-size", "default")
    expect(button).toHaveClass("bg-primary", "h-9")
  })

  it.each([
    ["destructive", "bg-destructive"],
    ["outline", "border"],
    ["secondary", "bg-secondary"],
    ["ghost", "hover:bg-accent"],
    ["link", "underline-offset-4"],
  ] as const)("applies the %s variant", (variant, className) => {
    render(<Button variant={variant}>Send</Button>)

    const button = screen.getByRole("button", { name: "Send" })
    expect(button).toHaveAttribute("data-variant", variant)
    expect(button).toHaveClass(className)
  })

  it.each([
    ["xs", "h-6"],
    ["sm", "h-8"],
    ["lg", "h-10"],
    ["icon", "size-9"],
  ] as const)("applies the %s size", (size, className) => {
    render(<Button size={size}>Send</Button>)

    expect(screen.getByRole("button", { name: "Send" })).toHaveClass(className)
  })

  it("does not call onClick when disabled", async () => {
    const onClick = vi.fn()
    render(
      <Button disabled onClick={onClick}>
        Send
      </Button>,
    )

    await userEvent.click(screen.getByRole("button", { name: "Send" }))

    expect(screen.getByRole("button", { name: "Send" })).toBeDisabled()
    expect(onClick).not.toHaveBeenCalled()
  })

  it("submits the form it belongs to", async () => {
    const onSubmit = vi.fn((event: React.FormEvent) => event.preventDefault())
    render(
      <>
        <form id="weather-form" onSubmit={onSubmit} />
        <Button type="submit" form="weather-form">
          Send
        </Button>
      </>,
    )

    await userEvent.click(screen.getByRole("button", { name: "Send" }))

    expect(onSubmit).toHaveBeenCalledOnce()
  })

  it("renders its child instead of a button with asChild", () => {
    render(
      <Button asChild variant="link">
        <a href="/history">History</a>
      </Button>,
    )

    const link = screen.getByRole("link", { name: "History" })
    expect(link).toHaveAttribute("data-variant", "link")
    expect(screen.queryByRole("button")).not.toBeInTheDocument()
  })

  it("merges className over the variant's classes", () => {
    render(<Button className="h-12">Send</Button>)

    const button = screen.getByRole("button", { name: "Send" })
    expect(button).toHaveClass("h-12")
    expect(button).not.toHaveClass("h-9")
  })
})

describe("buttonVariants", () => {
  it("returns the classes for a variant and size", () => {
    expect(buttonVariants({ variant: "outline", size: "sm" })).toContain("h-8")
  })
})
