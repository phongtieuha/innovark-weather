import { useState } from "react"
import type { Meta, StoryObj } from "@storybook/react-vite"
import { DatePicker, Field, FieldLabel } from "@innovark-weather/components"
import type { IDatePickerProps } from "@innovark-weather/components"
import { expect, screen, userEvent, within } from "storybook/test"

// Holds the value so the picker is interactive; shows it as the ISO string the form receives.
function DatePickerDemo(
  props: Omit<IDatePickerProps, "value" | "onValueChange"> & {
    readonly initialValue?: string
  },
) {
  const { initialValue, ...pickerProps } = props
  const [value, setValue] = useState(initialValue)

  return (
    <Field className="max-w-xs">
      <FieldLabel htmlFor="date">Date</FieldLabel>
      <DatePicker id="date" value={value} onValueChange={setValue} {...pickerProps} />
      <p className="text-muted-foreground text-sm">
        Value: <output data-testid="value">{value ?? "(none)"}</output>
      </p>
    </Field>
  )
}

const meta = {
  title: "Forms/Date Picker",
  component: DatePickerDemo,
  tags: ["autodocs"],
} satisfies Meta<typeof DatePickerDemo>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement)
    await userEvent.type(canvas.getByLabelText("Date"), "28092026")
    await expect(canvas.getByTestId("value")).toHaveTextContent("2026-09-28")
  },
}

export const WithValue: Story = {
  args: { initialValue: "2026-09-28" },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement)
    await expect(canvas.getByLabelText("Date")).toHaveValue("28/09/2026")

    await userEvent.click(canvas.getByRole("button", { name: "Open calendar" }))
    // The calendar is portalled to document.body, outside the story's canvas.
    await userEvent.click(await screen.findByRole("button", { name: /September 27th, 2026/ }))
    await expect(canvas.getByTestId("value")).toHaveTextContent("2026-09-27")
  },
}

export const Invalid: Story = {
  args: { "aria-invalid": true },
}

export const Disabled: Story = {
  args: { initialValue: "2026-09-28", disabled: true },
}
