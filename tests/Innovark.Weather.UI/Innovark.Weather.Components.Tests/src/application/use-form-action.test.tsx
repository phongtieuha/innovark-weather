import { describe, expect, it, vi } from "vitest"
import { act, render, renderHook, screen } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { useFormAction } from "@innovark-weather/components"

function TestForm() {
  const { FormAlert, openFormAlertAndFocus, setIsFormAlertOpen } = useFormAction({
    message: "The weather service is unavailable.",
    variant: "error",
  })

  return (
    <>
      <button onClick={openFormAlertAndFocus}>Send</button>
      <button onClick={() => setIsFormAlertOpen(false)}>Reset</button>
      {FormAlert}
    </>
  )
}

describe("useFormAction", () => {
  it("starts with the alert closed", () => {
    const { result } = renderHook(() => useFormAction({ message: "Saved." }))

    expect(result.current.isFormAlertOpen).toBe(false)
    expect(result.current.FormAlert).toBeNull()
  })

  it("opens the alert with the message when openFormAlertAndFocus is called", () => {
    const { result } = renderHook(() => useFormAction({ message: "Saved." }))

    act(() => {
      result.current.openFormAlertAndFocus()
    })

    expect(result.current.isFormAlertOpen).toBe(true)
    render(<>{result.current.FormAlert}</>)
    expect(screen.getByRole("alert")).toHaveTextContent("Saved.")
  })

  it("closes the alert when setIsFormAlertOpen(false) is called", () => {
    const { result } = renderHook(() => useFormAction({ message: "Saved." }))
    act(() => {
      result.current.openFormAlertAndFocus()
    })

    act(() => {
      result.current.setIsFormAlertOpen(false)
    })

    expect(result.current.isFormAlertOpen).toBe(false)
    expect(result.current.FormAlert).toBeNull()
  })

  it("shows the success variant by default", () => {
    const { result } = renderHook(() => useFormAction({ message: "Saved." }))
    act(() => {
      result.current.openFormAlertAndFocus()
    })

    render(<>{result.current.FormAlert}</>)

    expect(screen.getByRole("alert")).not.toHaveClass("text-destructive")
  })

  it("passes the variant and className to the alert", () => {
    const { result } = renderHook(() =>
      useFormAction({ message: "Failed.", variant: "error", className: "w-full" }),
    )
    act(() => {
      result.current.openFormAlertAndFocus()
    })

    const { container } = render(<>{result.current.FormAlert}</>)

    expect(screen.getByRole("alert")).toHaveClass("text-destructive")
    expect(container.querySelector(".w-full")).toBeInTheDocument()
  })

  it("moves focus to the alert and scrolls it into view when it opens", async () => {
    const scrollIntoView = vi.fn()
    vi.spyOn(HTMLElement.prototype, "scrollIntoView").mockImplementation(scrollIntoView)
    render(<TestForm />)

    await userEvent.click(screen.getByRole("button", { name: "Send" }))

    const alert = screen.getByRole("alert")
    expect(alert.closest("[tabindex='-1']")).toHaveFocus()
    expect(scrollIntoView).toHaveBeenCalledWith({ behavior: "smooth", block: "center" })
    vi.restoreAllMocks()
  })

  it("removes the alert when closed from a component", async () => {
    render(<TestForm />)
    await userEvent.click(screen.getByRole("button", { name: "Send" }))

    await userEvent.click(screen.getByRole("button", { name: "Reset" }))

    expect(screen.queryByRole("alert")).not.toBeInTheDocument()
  })
})
