import type { Meta, StoryObj } from "@storybook/react-vite"
import {
  Button,
  Card,
  CardAction,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@innovark-weather/components"

const meta = {
  title: "Display/Card",
  component: Card,
  tags: ["autodocs"],
} satisfies Meta<typeof Card>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => (
    <Card className="max-w-md">
      <CardHeader>
        <CardTitle>Weather history</CardTitle>
        <CardDescription>Ho Chi Minh City, in UTC+7.</CardDescription>
      </CardHeader>
      <CardContent>
        <p className="text-sm">10 hourly records: the requested hour and the 9 hours before it.</p>
      </CardContent>
      <CardFooter className="justify-end">
        <Button>Send</Button>
      </CardFooter>
    </Card>
  ),
}

export const WithAction: Story = {
  render: () => (
    <Card className="max-w-md">
      <CardHeader>
        <CardTitle>2026-09-28 14:00</CardTitle>
        <CardDescription>33.1 °C · 57% humidity</CardDescription>
        <CardAction>
          <Button variant="outline" size="sm">
            Refresh
          </Button>
        </CardAction>
      </CardHeader>
    </Card>
  ),
}
