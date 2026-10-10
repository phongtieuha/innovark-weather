import { describe, expect, it } from "vitest"
import { render, screen, within } from "@testing-library/react"
import type { WeatherHistoryResponse } from "@web/client/api/generated/model"
import { WeatherHistoryTable } from "@web/Pages/Home/WeatherHistoryTable"

const HISTORY: WeatherHistoryResponse = {
  requestedTime: "2026-09-28T14:00:00+07:00",
  records: [
    {
      time: "2026-09-28T14:00:00+07:00",
      temperatureC: 33,
      temperatureF: 91.4,
      relativeHumidity: 57,
    },
    {
      time: "2026-09-28T13:00:00+07:00",
      temperatureC: 32.25,
      temperatureF: 90.05,
      relativeHumidity: 61,
    },
    {
      time: "2026-09-28T12:00:00+07:00",
      temperatureC: -0.04,
      temperatureF: 31.93,
      relativeHumidity: 100,
    },
  ],
}

function bodyRows() {
  const [, body] = screen.getAllByRole("rowgroup")
  return within(body!)
    .getAllByRole("row")
    .map((row) =>
      within(row)
        .getAllByRole("cell")
        .map((cell) => cell.textContent),
    )
}

describe("WeatherHistoryTable", () => {
  it("names the table by the record count and the requested time", () => {
    render(<WeatherHistoryTable history={HISTORY} />)

    expect(
      screen.getByRole("table", {
        name: "3 hourly records up to 2026-09-28 14:00 (UTC+7), newest first.",
      }),
    ).toBeInTheDocument()
  })

  it("has a column for the time, both temperatures and the humidity", () => {
    render(<WeatherHistoryTable history={HISTORY} />)

    expect(screen.getAllByRole("columnheader").map((th) => th.textContent)).toEqual([
      "Time (UTC+7)",
      "°C",
      "°F",
      "Humidity (%)",
    ])
  })

  it("shows one row per record, in the API's order, with times as yyyy-MM-dd HH:mm", () => {
    render(<WeatherHistoryTable history={HISTORY} />)

    expect(bodyRows()).toEqual([
      ["2026-09-28 14:00", "33.0", "91.4", "57"],
      ["2026-09-28 13:00", "32.3", "90.0", "61"],
      ["2026-09-28 12:00", "-0.0", "31.9", "100"],
    ])
  })

  it("shows the UTC+7 time as sent, without converting it to the browser's time zone", () => {
    render(
      <WeatherHistoryTable
        history={{
          requestedTime: "2026-09-29T00:00:00+07:00",
          records: [
            {
              time: "2026-09-29T00:00:00+07:00",
              temperatureC: 27,
              temperatureF: 80.6,
              relativeHumidity: 80,
            },
          ],
        }}
      />,
    )

    expect(bodyRows()).toEqual([["2026-09-29 00:00", "27.0", "80.6", "80"]])
  })

  it("aligns the numbers to the right, with tabular figures", () => {
    render(<WeatherHistoryTable history={HISTORY} />)

    const [, celsius] = within(screen.getAllByRole("row")[1]!).getAllByRole("cell")
    expect(celsius).toHaveClass("text-right", "tabular-nums")
  })

  it("renders an empty body for no records", () => {
    render(
      <WeatherHistoryTable history={{ requestedTime: "2026-09-28T14:00:00+07:00", records: [] }} />,
    )

    expect(screen.getByRole("table")).toHaveAccessibleName(
      "0 hourly records up to 2026-09-28 14:00 (UTC+7), newest first.",
    )
    const [, body] = screen.getAllByRole("rowgroup")
    expect(within(body!).queryAllByRole("row")).toHaveLength(0)
  })
})
