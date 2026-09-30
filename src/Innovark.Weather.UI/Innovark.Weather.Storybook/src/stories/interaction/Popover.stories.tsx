import type { Meta, StoryObj } from "@storybook/react-vite"
import {
  Button,
  Popover,
  PopoverContent,
  PopoverDescription,
  PopoverHeader,
  PopoverTitle,
  PopoverTrigger,
} from "@innovark-weather/components"
import { expect, screen, userEvent, within } from "storybook/test"

const meta = {
  title: "Interaction/Popover",
  component: Popover,
  tags: ["autodocs"],
} satisfies Meta<typeof Popover>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => (
    <Popover>
      <PopoverTrigger asChild>
        <Button variant="outline">Location</Button>
      </PopoverTrigger>
      <PopoverContent aria-labelledby="popover-location-title">
        <PopoverHeader>
          <PopoverTitle id="popover-location-title">Ho Chi Minh City</PopoverTitle>
          <PopoverDescription>10.762622, 106.660172 · UTC+7</PopoverDescription>
        </PopoverHeader>
      </PopoverContent>
    </Popover>
  ),
  play: async ({ canvasElement }) => {
    await userEvent.click(within(canvasElement).getByRole("button", { name: "Location" }))
    // Popover content is portalled to document.body, outside the story's canvas.
    await expect(await screen.findByText("Ho Chi Minh City")).toBeVisible()
  },
}
