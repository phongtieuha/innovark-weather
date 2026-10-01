import type { Meta, StoryObj } from "@storybook/react-vite"
import { Input } from "@innovark-weather/components"
import { expect, userEvent, within } from "storybook/test"

const meta = {
  title: "Forms/Input",
  component: Input,
  tags: ["autodocs"],
  args: { placeholder: "e.g. 14", "aria-label": "Hour", className: "max-w-xs" },
} satisfies Meta<typeof Input>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const input = within(canvasElement).getByRole("textbox", { name: "Hour" })
    await userEvent.type(input, "14")
    await expect(input).toHaveValue("14")
  },
}

export const Invalid: Story = {
  args: { "aria-invalid": true, defaultValue: "24" },
}

export const Disabled: Story = {
  args: { disabled: true },
}
