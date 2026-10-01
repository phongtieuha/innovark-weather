import type { Meta, StoryObj } from "@storybook/react-vite"
import {
  Table,
  TableBody,
  TableCaption,
  TableCell,
  TableFooter,
  TableHead,
  TableHeader,
  TableRow,
} from "@innovark-weather/components"
import { expect, within } from "storybook/test"

const RECORDS = [
  { time: "2026-09-28 14:00", temperatureC: 33.1, temperatureF: 91.6, relativeHumidity: 57 },
  { time: "2026-09-28 13:00", temperatureC: 32.4, temperatureF: 90.3, relativeHumidity: 60 },
  { time: "2026-09-28 12:00", temperatureC: 31.8, temperatureF: 89.2, relativeHumidity: 63 },
]

const meta = {
  title: "Display/Table",
  component: Table,
  tags: ["autodocs"],
} satisfies Meta<typeof Table>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => (
    <Table className="max-w-xl">
      <TableCaption>3 hourly records up to 2026-09-28 14:00 (UTC+7), newest first.</TableCaption>
      <TableHeader>
        <TableRow>
          <TableHead>Time (UTC+7)</TableHead>
          <TableHead className="text-right">°C</TableHead>
          <TableHead className="text-right">°F</TableHead>
          <TableHead className="text-right">Humidity (%)</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {RECORDS.map((record) => (
          <TableRow key={record.time}>
            <TableCell>{record.time}</TableCell>
            <TableCell className="text-right tabular-nums">
              {record.temperatureC.toFixed(1)}
            </TableCell>
            <TableCell className="text-right tabular-nums">
              {record.temperatureF.toFixed(1)}
            </TableCell>
            <TableCell className="text-right tabular-nums">{record.relativeHumidity}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  ),
  play: async ({ canvasElement }) => {
    const table = within(canvasElement).getByRole("table")
    await expect(within(table).getAllByRole("row")).toHaveLength(RECORDS.length + 1)
    await expect(within(table).getByRole("columnheader", { name: "Humidity (%)" })).toBeVisible()
  },
}

export const WithFooter: Story = {
  render: () => (
    <Table className="max-w-xl">
      <TableHeader>
        <TableRow>
          <TableHead>Time (UTC+7)</TableHead>
          <TableHead className="text-right">°C</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {RECORDS.map((record) => (
          <TableRow key={record.time}>
            <TableCell>{record.time}</TableCell>
            <TableCell className="text-right tabular-nums">
              {record.temperatureC.toFixed(1)}
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
      <TableFooter>
        <TableRow>
          <TableCell>Average</TableCell>
          <TableCell className="text-right tabular-nums">32.4</TableCell>
        </TableRow>
      </TableFooter>
    </Table>
  ),
}
