import type { Meta, StoryObj } from "@storybook/react-vite"
import { FormAlert } from "@innovark-weather/components"

const meta = {
  title: "Application/Form Alert",
  component: FormAlert,
  tags: ["autodocs"],
  args: { className: "max-w-md" },
} satisfies Meta<typeof FormAlert>

export default meta
type Story = StoryObj<typeof meta>

export const Success: Story = {
  args: { variant: "success", children: "Request sent for 2026-09-28 at 14:00 (UTC+7)." },
}

export const Error: Story = {
  args: {
    variant: "error",
    children: "Unable to reach the server. Check your network connection and try again.",
  },
}
