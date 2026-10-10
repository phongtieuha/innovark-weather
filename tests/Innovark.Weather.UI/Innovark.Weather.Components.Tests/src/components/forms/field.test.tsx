import { describe, expect, it } from "vitest"
import { render, screen } from "@testing-library/react"
import {
  Field,
  FieldContent,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
  FieldLegend,
  FieldSeparator,
  FieldSet,
  FieldTitle,
} from "@innovark-weather/components"

describe("Field", () => {
  it("groups a label, control and description", () => {
    render(
      <Field>
        <FieldLabel htmlFor="hour">Hour</FieldLabel>
        <input id="hour" />
        <FieldDescription>0 to 23</FieldDescription>
      </Field>,
    )

    expect(screen.getByRole("group")).toHaveAttribute("data-orientation", "vertical")
    expect(screen.getByLabelText("Hour")).toBeInTheDocument()
    expect(screen.getByText("0 to 23")).toHaveAttribute("data-slot", "field-description")
  })

  it("applies the orientation", () => {
    render(<Field orientation="horizontal" />)

    const field = screen.getByRole("group")
    expect(field).toHaveAttribute("data-orientation", "horizontal")
    expect(field).toHaveClass("flex-row")
  })

  it("renders a fieldset with its legend, a group, content and a title", () => {
    render(
      <FieldSet>
        <FieldLegend variant="label">When</FieldLegend>
        <FieldGroup>
          <FieldContent>
            <FieldTitle>Date</FieldTitle>
          </FieldContent>
        </FieldGroup>
      </FieldSet>,
    )

    expect(screen.getByRole("group", { name: "When" })).toHaveAttribute("data-slot", "field-set")
    expect(screen.getByText("When")).toHaveAttribute("data-variant", "label")
    expect(screen.getByText("Date")).toHaveAttribute("data-slot", "field-label")
    expect(screen.getByText("Date").parentElement).toHaveAttribute("data-slot", "field-content")
  })

  it("uses the legend variant by default", () => {
    render(
      <FieldSet>
        <FieldLegend>When</FieldLegend>
      </FieldSet>,
    )

    expect(screen.getByText("When")).toHaveAttribute("data-variant", "legend")
  })
})

describe("FieldSeparator", () => {
  it("renders a line without content", () => {
    const { container } = render(<FieldSeparator />)

    const separator = container.querySelector("[data-slot='field-separator']")
    expect(separator).toHaveAttribute("data-content", "false")
    expect(container.querySelector("[data-slot='field-separator-content']")).toBeNull()
  })

  it("renders its content over the line", () => {
    render(<FieldSeparator>or</FieldSeparator>)

    expect(screen.getByText("or")).toHaveAttribute("data-slot", "field-separator-content")
    expect(screen.getByText("or").parentElement).toHaveAttribute("data-content", "true")
  })
})

describe("FieldError", () => {
  it("renders nothing without children or errors", () => {
    const { container } = render(<FieldError />)

    expect(container).toBeEmptyDOMElement()
  })

  it("renders nothing for an empty errors list", () => {
    const { container } = render(<FieldError errors={[]} />)

    expect(container).toBeEmptyDOMElement()
  })

  it("renders its children in an alert", () => {
    render(<FieldError>Enter an hour.</FieldError>)

    expect(screen.getByRole("alert")).toHaveTextContent("Enter an hour.")
  })

  it("renders a single error's message", () => {
    render(<FieldError errors={[{ message: "Enter an hour." }]} />)

    expect(screen.getByRole("alert")).toHaveTextContent("Enter an hour.")
    expect(screen.queryByRole("list")).not.toBeInTheDocument()
  })

  it("renders one message for repeated errors", () => {
    render(<FieldError errors={[{ message: "Enter an hour." }, { message: "Enter an hour." }]} />)

    expect(screen.getByRole("alert")).toHaveTextContent("Enter an hour.")
    expect(screen.queryByRole("list")).not.toBeInTheDocument()
  })

  it("lists several different errors", () => {
    render(<FieldError errors={[{ message: "Too early." }, { message: "Too late." }]} />)

    expect(screen.getAllByRole("listitem").map((li) => li.textContent)).toEqual([
      "Too early.",
      "Too late.",
    ])
  })

  it("renders nothing for an undefined error", () => {
    const { container } = render(<FieldError errors={[undefined]} />)

    expect(container).toBeEmptyDOMElement()
  })
})
