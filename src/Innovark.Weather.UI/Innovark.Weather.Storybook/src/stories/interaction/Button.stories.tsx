import type { Meta, StoryObj } from "@storybook/react-vite"
import { ArrowRightIcon, PlusIcon } from "lucide-react"
import { Button } from "@innovark-weather/components"
import { expect, fn, userEvent, within } from "storybook/test"

const meta = {
  title: "Interaction/Button",
  component: Button,
  tags: ["autodocs"],
  args: { children: "Send", onClick: fn() },
  argTypes: {
    variant: {
      control: "select",
      options: ["default", "destructive", "outline", "secondary", "ghost", "link"],
    },
    size: { control: "select", options: ["default", "sm", "lg", "icon"] },
  },
} satisfies Meta<typeof Button>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  play: async ({ args, canvasElement }) => {
    await userEvent.click(within(canvasElement).getByRole("button", { name: "Send" }))
    await expect(args.onClick).toHaveBeenCalledOnce()
  },
}

export const Variants: Story = {
  render: (args) => (
    <div className="flex flex-wrap items-center gap-3">
      <Button {...args}>Default</Button>
      <Button {...args} variant="secondary">
        Secondary
      </Button>
      <Button {...args} variant="outline">
        Outline
      </Button>
      <Button {...args} variant="ghost">
        Ghost
      </Button>
      <Button {...args} variant="destructive">
        Destructive
      </Button>
      <Button {...args} variant="link">
        Link
      </Button>
    </div>
  ),
}

export const Sizes: Story = {
  render: (args) => (
    <div className="flex flex-wrap items-center gap-3">
      <Button {...args} size="sm">
        Small
      </Button>
      <Button {...args}>Default</Button>
      <Button {...args} size="lg">
        Large
      </Button>
      <Button {...args} size="icon" aria-label="Add" title="Add">
        <PlusIcon />
      </Button>
    </div>
  ),
}

export const WithIcon: Story = {
  render: (args) => (
    <Button {...args}>
      Continue
      <ArrowRightIcon />
    </Button>
  ),
}

export const Disabled: Story = {
  args: { disabled: true },
}
