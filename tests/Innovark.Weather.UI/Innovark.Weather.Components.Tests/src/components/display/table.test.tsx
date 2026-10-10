import { describe, expect, it } from "vitest"
import { render, screen, within } from "@testing-library/react"
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

function WeatherTable() {
  return (
    <Table>
      <TableCaption>The last two hours</TableCaption>
      <TableHeader>
        <TableRow>
          <TableHead>Time</TableHead>
          <TableHead>Temperature (°C)</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        <TableRow>
          <TableCell>14:00</TableCell>
          <TableCell>33</TableCell>
        </TableRow>
        <TableRow>
          <TableCell>13:00</TableCell>
          <TableCell>32.2</TableCell>
        </TableRow>
      </TableBody>
      <TableFooter>
        <TableRow>
          <TableCell>Average</TableCell>
          <TableCell>32.6</TableCell>
        </TableRow>
      </TableFooter>
    </Table>
  )
}

describe("Table", () => {
  it("renders a table named by its caption", () => {
    render(<WeatherTable />)

    expect(screen.getByRole("table", { name: "The last two hours" })).toBeInTheDocument()
  })

  it("renders the header, body and footer rows", () => {
    render(<WeatherTable />)

    const [header, body, footer] = screen.getAllByRole("rowgroup")
    expect(
      within(header!)
        .getAllByRole("columnheader")
        .map((th) => th.textContent),
    ).toEqual(["Time", "Temperature (°C)"])
    expect(within(body!).getAllByRole("row")).toHaveLength(2)
    expect(within(footer!).getByRole("row")).toHaveTextContent("Average32.6")
  })

  it("wraps the table in a horizontally scrolling container", () => {
    render(<WeatherTable />)

    const container = screen.getByRole("table").parentElement
    expect(container).toHaveAttribute("data-slot", "table-container")
    expect(container).toHaveClass("overflow-x-auto")
  })

  it("merges className into each part", () => {
    render(
      <Table className="table-fixed">
        <TableBody>
          <TableRow className="h-12">
            <TableCell className="text-right">33</TableCell>
          </TableRow>
        </TableBody>
      </Table>,
    )

    expect(screen.getByRole("table")).toHaveClass("table-fixed")
    expect(screen.getByRole("row")).toHaveClass("h-12")
    expect(screen.getByRole("cell")).toHaveClass("text-right")
  })
})
