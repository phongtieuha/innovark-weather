import type { Meta, StoryObj } from "@storybook/react-vite"
import { Separator } from "@innovark-weather/components"

const meta = {
  title: "Display/Separator",
  component: Separator,
  tags: ["autodocs"],
} satisfies Meta<typeof Separator>

export default meta
type Story = StoryObj<typeof meta>

export const Horizontal: Story = {
  render: () => (
    <div className="max-w-sm text-sm">
      <p>Temperature</p>
      <Separator className="my-3" />
      <p>Relative humidity</p>
    </div>
  ),
}

export const Vertical: Story = {
  render: () => (
    <div className="flex h-5 items-center gap-3 text-sm">
      <span>°C</span>
      <Separator orientation="vertical" />
      <span>°F</span>
    </div>
  ),
}
