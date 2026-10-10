import { describe, expect, it } from "vitest"
import { render, screen } from "@testing-library/react"
import { toProblemState } from "@innovark-weather/components"

describe("toProblemState", () => {
  it.each([null, undefined])("returns an empty state for %s", (problem) => {
    expect(toProblemState(problem)).toEqual({
      fieldErrors: undefined,
      title: undefined,
      traceId: undefined,
      alertMessage: "",
    })
  })

  it("returns the title and trace ID of a problem without field errors", () => {
    const state = toProblemState({
      title: "The weather service is unavailable.",
      traceId: "00-abc",
    })

    expect(state.title).toBe("The weather service is unavailable.")
    expect(state.traceId).toBe("00-abc")
    expect(state.fieldErrors).toBeUndefined()
  })

  it("returns the field errors of a validation problem", () => {
    const errors = { hour: ["The hour must be from 0 to 23."] }

    const state = toProblemState({ title: "One or more validation errors occurred.", errors })

    expect(state.fieldErrors).toEqual(errors)
  })

  it("treats an empty errors object as no field errors", () => {
    expect(toProblemState({ title: "Bad request", errors: {} }).fieldErrors).toBeUndefined()
  })

  it("turns null title and trace ID into undefined", () => {
    const state = toProblemState({ title: null, traceId: null })

    expect(state.title).toBeUndefined()
    expect(state.traceId).toBeUndefined()
    expect(state.alertMessage).toBe("")
  })

  it("puts the title, each field's errors and the trace ID in the alert message", () => {
    const { alertMessage } = toProblemState({
      title: "One or more validation errors occurred.",
      errors: { date: ["Required."], hour: ["Too early.", "Too late."] },
      traceId: "00-abc",
    })

    render(<>{alertMessage}</>)

    expect(screen.getByText("One or more validation errors occurred.")).toBeInTheDocument()
    const items = screen.getAllByRole("listitem")
    expect(items.map((item) => item.textContent)).toEqual([
      "date: Required.",
      "hour: Too early. Too late.",
    ])
    expect(screen.getByText("Trace ID: 00-abc")).toBeInTheDocument()
  })

  it("shows only the title when there are no field errors or trace ID", () => {
    const { alertMessage } = toProblemState({ title: "Something went wrong." })

    render(<>{alertMessage}</>)

    expect(screen.getByText("Something went wrong.")).toBeInTheDocument()
    expect(screen.queryByRole("list")).not.toBeInTheDocument()
    expect(screen.queryByText(/Trace ID/)).not.toBeInTheDocument()
  })

  it("shows the field errors even without a title", () => {
    const { alertMessage } = toProblemState({ errors: { hour: ["Too late."] } })

    render(<>{alertMessage}</>)

    expect(screen.getByRole("listitem")).toHaveTextContent("hour: Too late.")
  })
})
