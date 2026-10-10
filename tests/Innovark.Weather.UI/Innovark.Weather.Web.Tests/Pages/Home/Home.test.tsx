import { afterEach, beforeEach, describe, expect, it, vi } from "vitest"
import { act, render, screen, waitFor, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
// With the extension: Pages/Home/Home.ts beside it is the page's entry, which mounts the app.
import App from "@web/Pages/Home/Home.tsx"

// 14:30 on 28 September 2026 in UTC+7, so 2026-09-28 at 14:00 is a valid choice.
const NOW = new Date("2026-09-28T07:30:00Z")

const HISTORY = {
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
      temperatureC: 32.2,
      temperatureF: 90,
      relativeHumidity: 61,
    },
  ],
}

function json(body: unknown, status = 200, contentType = "application/json"): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": contentType } })
}

function problem(body: unknown, status: number): Response {
  return json(body, status, "application/problem+json")
}

const fetchMock = vi.fn<typeof fetch>()

function dateInput() {
  return screen.getByRole("textbox", { name: /Date/ })
}

function hourInput() {
  return screen.getByRole("spinbutton", { name: /Hour/ })
}

// "Send", or "Loading Send" while the spinner is inside it.
function sendButton() {
  return screen.getByRole("button", { name: /Send$/ })
}

async function fillIn(date: string, hour: string) {
  await userEvent.type(dateInput(), date)
  await userEvent.type(hourInput(), hour)
}

// The form's own alert, as opposed to a field's error (also role="alert").
async function findFormAlert() {
  const alerts = await screen.findAllByRole("alert")
  const formAlert = alerts.find((alert) => alert.getAttribute("data-slot") === "alert")
  expect(formAlert).toBeDefined()
  return formAlert!
}

function requestedUrls() {
  return fetchMock.mock.calls.map(([url]) => new URL(String(url)))
}

describe("Home", () => {
  beforeEach(() => {
    // Only Date: the form's "now", while timers stay real for react-query and userEvent.
    vi.useFakeTimers({ toFake: ["Date"] })
    vi.setSystemTime(NOW)
    vi.stubGlobal("fetch", fetchMock)
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
    vi.useRealTimers()
  })

  it("shows the form, with both fields required", () => {
    render(<App />)

    expect(screen.getByText("Weather history")).toBeInTheDocument()
    expect(dateInput()).toHaveAccessibleName("Date (required)")
    expect(hourInput()).toHaveAccessibleName("Hour (required)")
    expect(screen.getByText("0 to 23")).toBeInTheDocument()
    expect(screen.queryByRole("table")).not.toBeInTheDocument()
  })

  it("shows the fields' errors and sends nothing when the form is invalid", async () => {
    render(<App />)

    await userEvent.click(sendButton())

    expect(await screen.findByText("Choose a date.")).toBeInTheDocument()
    expect(screen.getByText("Enter an hour.")).toBeInTheDocument()
    expect(dateInput()).toBeInvalid()
    expect(hourInput()).toBeInvalid()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it("re-validates a field as it changes after a failed submit", async () => {
    render(<App />)
    await userEvent.click(sendButton())
    await screen.findByText("Enter an hour.")

    await userEvent.type(hourInput(), "24")
    expect(await screen.findByText("Enter a whole hour from 0 to 23.")).toBeInTheDocument()

    await userEvent.clear(hourInput())
    await userEvent.type(hourInput(), "14")
    await waitFor(() => expect(hourInput()).not.toBeInvalid())
  })

  it("rejects a date outside the last 3 days without calling the endpoint", async () => {
    render(<App />)
    await fillIn("24092026", "14")

    await userEvent.click(sendButton())

    expect(await screen.findByText("Choose a date within the last 3 days.")).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it("asks the endpoint for the date and hour, and shows the records", async () => {
    fetchMock.mockResolvedValue(json(HISTORY))
    render(<App />)
    await fillIn("28092026", "14")

    await userEvent.click(sendButton())

    const table = await screen.findByRole("table")
    expect(table).toHaveAccessibleName(
      "2 hourly records up to 2026-09-28 14:00 (UTC+7), newest first.",
    )
    expect(within(table).getAllByRole("row")).toHaveLength(3)
    expect(fetchMock).toHaveBeenCalledOnce()
    const [url] = requestedUrls()
    expect(url!.pathname).toBe("/api/weather/history")
    expect(Object.fromEntries(url!.searchParams)).toEqual({ date: "2026-09-28", hour: "14" })
    expect(fetchMock.mock.calls[0]![1]).toMatchObject({ method: "GET" })
    expect(screen.queryByRole("alert")).not.toBeInTheDocument()
  })

  it("disables the form and shows a spinner while the request is pending", async () => {
    let respond: (response: Response) => void = () => {}
    fetchMock.mockReturnValue(new Promise((resolve) => (respond = resolve)))
    render(<App />)
    await fillIn("28092026", "14")

    await userEvent.click(sendButton())

    await waitFor(() => expect(sendButton()).toBeDisabled())
    expect(sendButton()).toHaveAccessibleName("Loading Send")
    expect(within(sendButton()).getByRole("status", { name: "Loading" })).toBeInTheDocument()
    expect(dateInput()).toBeDisabled()
    expect(hourInput()).toBeDisabled()

    respond(json(HISTORY))

    await screen.findByRole("table")
    expect(sendButton()).toBeEnabled()
    expect(screen.queryByRole("status", { name: "Loading" })).not.toBeInTheDocument()
  })

  it("shows the API's validation errors in an alert, and moves focus to it", async () => {
    fetchMock.mockResolvedValue(
      problem(
        {
          title: "One or more validation errors occurred.",
          status: 400,
          errors: { hour: ["The hour must be no later than the current hour."] },
          traceId: "00-abc-01",
        },
        400,
      ),
    )
    render(<App />)
    await fillIn("28092026", "14")

    await userEvent.click(sendButton())

    const alert = await findFormAlert()
    expect(alert).toHaveTextContent("One or more validation errors occurred.")
    expect(within(alert).getByRole("listitem")).toHaveTextContent(
      "hour: The hour must be no later than the current hour.",
    )
    expect(alert).toHaveTextContent("Trace ID: 00-abc-01")
    await waitFor(() => expect(alert.closest("[tabindex='-1']")).toHaveFocus())
    expect(screen.queryByRole("table")).not.toBeInTheDocument()
  })

  it("shows a service error in an alert", async () => {
    fetchMock.mockResolvedValue(
      problem(
        { title: "The weather service is unavailable. Try again in a moment.", status: 503 },
        503,
      ),
    )
    render(<App />)
    await fillIn("28092026", "14")

    await userEvent.click(sendButton())

    expect(await findFormAlert()).toHaveTextContent(
      "The weather service is unavailable. Try again in a moment.",
    )
  })

  it("shows a network error in an alert", async () => {
    fetchMock.mockRejectedValue(new TypeError("Failed to fetch"))
    render(<App />)
    await fillIn("28092026", "14")

    await userEvent.click(sendButton())

    expect(await findFormAlert()).toHaveTextContent(
      "Unable to reach the server. Check your network connection and try again.",
    )
  })

  it("sends again on every submit, and replaces the error with the records once it succeeds", async () => {
    fetchMock
      .mockResolvedValueOnce(
        problem({ title: "The weather service returned an error.", status: 502 }, 502),
      )
      .mockResolvedValueOnce(json(HISTORY))
    render(<App />)
    await fillIn("28092026", "14")

    await userEvent.click(sendButton())
    await findFormAlert()
    await userEvent.click(sendButton())

    await screen.findByRole("table")
    expect(screen.queryByRole("alert")).not.toBeInTheDocument()
    expect(requestedUrls().map((url) => url.search)).toEqual([
      "?date=2026-09-28&hour=14",
      "?date=2026-09-28&hour=14",
    ])
  })

  it("replaces the records with an error when a later request fails", async () => {
    fetchMock
      .mockResolvedValueOnce(json(HISTORY))
      .mockResolvedValueOnce(
        problem({ title: "The weather service returned an error.", status: 502 }, 502),
      )
    render(<App />)
    await fillIn("28092026", "14")
    await userEvent.click(sendButton())
    await screen.findByRole("table")

    await userEvent.click(sendButton())

    expect(await findFormAlert()).toHaveTextContent("The weather service returned an error.")
    expect(screen.queryByRole("table")).not.toBeInTheDocument()
  })
})

describe("Home.ts, the page's entry", () => {
  afterEach(() => {
    document.body.innerHTML = ""
  })

  it("mounts the page's app into #app", async () => {
    // What Home.cshtml renders for the app to mount into.
    document.body.innerHTML = '<div id="app"></div>'

    await act(() => import("@web/Pages/Home/Home.ts"))

    expect(document.querySelector("#app")).toContainElement(screen.getByText("Weather history"))
  })
})
