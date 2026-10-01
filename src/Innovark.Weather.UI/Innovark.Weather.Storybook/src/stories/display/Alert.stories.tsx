import type { Meta, StoryObj } from "@storybook/react-vite"
import { CircleAlertIcon, CircleCheckIcon } from "lucide-react"
import { Alert, AlertDescription, AlertTitle } from "@innovark-weather/components"

const meta = {
  title: "Display/Alert",
  component: Alert,
  tags: ["autodocs"],
} satisfies Meta<typeof Alert>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => (
    <Alert>
      <CircleCheckIcon />
      <AlertTitle>Request sent</AlertTitle>
      <AlertDescription>
        The weather history for 2026-09-28 at 14:00 was requested.
      </AlertDescription>
    </Alert>
  ),
}

export const Destructive: Story = {
  render: () => (
    <Alert variant="destructive">
      <CircleAlertIcon />
      <AlertTitle>Weather provider unavailable</AlertTitle>
      <AlertDescription>Open-Meteo couldn't be reached. Try again in a moment.</AlertDescription>
    </Alert>
  ),
}

export const TitleOnly: Story = {
  render: () => (
    <Alert>
      <CircleCheckIcon />
      <AlertTitle>Saved.</AlertTitle>
    </Alert>
  ),
}
