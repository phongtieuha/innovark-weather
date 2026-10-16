import { describe, expect, it } from "vitest"
import { render, screen, waitFor } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { useForm, type Resolver } from "react-hook-form"
import { ControlledField, type IControlledFieldNode } from "@innovark-weather/components"

interface IFormValues {
  readonly hour: string
}

// Fails until the hour is filled in, like a schema resolver would.
const requiredHour: Resolver<IFormValues> = (values) =>
  values.hour
    ? { values, errors: {} as Record<string, never> }
    : { values: {}, errors: { hour: { type: "required", message: "Enter an hour." } } }

function TestForm({
  required,
  description,
  className,
  validate = false,
}: {
  readonly required?: boolean
  readonly description?: string
  readonly className?: string
  readonly validate?: boolean
}) {
  const { control, handleSubmit } = useForm<IFormValues>({
    defaultValues: { hour: "" },
    mode: "onSubmit",
    reValidateMode: "onChange",
    resolver: validate ? requiredHour : undefined,
  })

  const node: IControlledFieldNode<IFormValues> = {
    name: "hour",
    label: "Hour",
    required,
    description,
    render: ({ field, fieldState }) => (
      <input id={field.name} {...field} aria-invalid={fieldState.invalid} />
    ),
  }

  return (
    <form onSubmit={handleSubmit(() => {})}>
      <ControlledField node={node} control={control} className={className} />
      <button type="submit">Send</button>
    </form>
  )
}

describe("ControlledField", () => {
  it("labels the rendered input with node.label", () => {
    render(<TestForm />)

    expect(screen.getByLabelText("Hour")).toBeInTheDocument()
  })

  it("does not mark the field as required by default", () => {
    render(<TestForm />)

    expect(screen.queryByText("*")).not.toBeInTheDocument()
  })

  it("marks a required field, with a screen reader text", () => {
    render(<TestForm required />)

    expect(screen.getByText("*")).toHaveAttribute("aria-hidden", "true")
    expect(screen.getByText("(required)")).toHaveClass("sr-only")
  })

  it("renders node.description", () => {
    render(<TestForm description="0 to 23" />)

    expect(screen.getByText("0 to 23")).toBeInTheDocument()
  })

  it("applies className to the field's group", () => {
    render(<TestForm className="mt-5" />)

    expect(screen.getByRole("group")).toHaveClass("gap-2", "mt-5")
  })

  it("binds the rendered input to the form", async () => {
    render(<TestForm />)

    await userEvent.type(screen.getByLabelText("Hour"), "14")

    expect(screen.getByLabelText("Hour")).toHaveValue("14")
  })

  it("shows the field's error after a failed submit", async () => {
    render(<TestForm validate />)

    await userEvent.click(screen.getByRole("button", { name: "Send" }))

    expect(await screen.findByRole("alert")).toHaveTextContent("Enter an hour.")
    expect(screen.getByRole("group")).toHaveAttribute("data-invalid", "true")
    expect(screen.getByLabelText("Hour")).toHaveAttribute("aria-invalid", "true")
  })

  it("keeps the last error in the collapsing row once the field is valid again", async () => {
    render(<TestForm validate />)
    await userEvent.click(screen.getByRole("button", { name: "Send" }))
    const error = await screen.findByRole("alert")

    await userEvent.type(screen.getByLabelText("Hour"), "14")

    await waitFor(() => expect(screen.getByRole("group")).toHaveAttribute("data-invalid", "false"))
    expect(error).toHaveTextContent("Enter an hour.")
    expect(error.parentElement).toHaveAttribute("aria-hidden", "true")
    expect(error.parentElement).toHaveClass("max-h-0", "opacity-0")
  })
})
