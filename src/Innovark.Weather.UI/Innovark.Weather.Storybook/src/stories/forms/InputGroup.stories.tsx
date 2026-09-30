import type { Meta, StoryObj } from "@storybook/react-vite"
import { ClockIcon, SearchIcon } from "lucide-react"
import {
  InputGroup,
  InputGroupAddon,
  InputGroupButton,
  InputGroupInput,
  InputGroupText,
} from "@innovark-weather/components"

const meta = {
  title: "Forms/Input Group",
  component: InputGroup,
  tags: ["autodocs"],
} satisfies Meta<typeof InputGroup>

export default meta
type Story = StoryObj<typeof meta>

export const WithIcon: Story = {
  render: () => (
    <InputGroup className="max-w-xs">
      <InputGroupInput placeholder="Search a city" aria-label="City" />
      <InputGroupAddon>
        <SearchIcon />
      </InputGroupAddon>
    </InputGroup>
  ),
}

export const WithText: Story = {
  render: () => (
    <InputGroup className="max-w-xs">
      <InputGroupInput placeholder="14" aria-label="Hour" inputMode="numeric" />
      <InputGroupAddon align="inline-end">
        <InputGroupText>:00 UTC+7</InputGroupText>
      </InputGroupAddon>
    </InputGroup>
  ),
}

export const WithButton: Story = {
  render: () => (
    <InputGroup className="max-w-xs">
      <InputGroupInput placeholder="14" aria-label="Hour" />
      <InputGroupAddon align="inline-end">
        <InputGroupButton size="icon-xs" aria-label="Use current hour" title="Use current hour">
          <ClockIcon />
        </InputGroupButton>
      </InputGroupAddon>
    </InputGroup>
  ),
}
