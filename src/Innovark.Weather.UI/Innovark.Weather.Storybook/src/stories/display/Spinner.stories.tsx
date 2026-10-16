import type { Meta, StoryObj } from "@storybook/react-vite"
import { Spinner } from "@innovark-weather/components"

const meta = {
  title: "Display/Spinner",
  component: Spinner,
  tags: ["autodocs"],
} satisfies Meta<typeof Spinner>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {}

export const Sizes: Story = {
  render: () => (
    <div className="flex items-center gap-4">
      <Spinner />
      <Spinner className="size-6" />
      <Spinner className="size-8" />
    </div>
  ),
}
