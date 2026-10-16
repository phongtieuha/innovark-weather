import { delay, http, HttpResponse } from "msw"

import { serverErrorProblem, validationProblem } from "./problemDetails"

export interface IWeatherHistoryParams {
  readonly date: string
  readonly hour: number
}

export interface IWeatherHistoryResponse {
  readonly requestedTime: string
  readonly records: ReadonlyArray<{
    readonly time: string
    readonly temperatureC: number
    readonly temperatureF: number
    readonly relativeHumidity: number
  }>
}

// The requested hour and the 9 before it, newest first, as the web endpoint returns them.
function historyFor({ date, hour }: IWeatherHistoryParams): IWeatherHistoryResponse {
  const requested = Date.parse(`${date}T${String(hour).padStart(2, "0")}:00:00+07:00`)
  const utc7 = (ms: number) => `${new Date(ms + 7 * 3_600_000).toISOString().slice(0, 19)}+07:00`
  const records = Array.from({ length: 10 }, (_, i) => {
    const temperatureC = Math.round((33 - i * 0.8) * 10) / 10
    return {
      time: utc7(requested - i * 3_600_000),
      temperatureC,
      temperatureF: Math.round((temperatureC * 1.8 + 32) * 10) / 10,
      relativeHumidity: 57 + i * 4,
    }
  })
  return { requestedTime: utc7(requested), records }
}

// Any origin and base path: fetchApiAsync resolves the path against the page's base URL.
const WEATHER_HISTORY_URL = "*/api/weather/history"

// One handler per outcome of the web app's GET /api/weather/history?date=…&hour=… (which calls the
// API), so each story can pick the response it demonstrates instead of getting a random one.
export const weatherHistoryHandlers = {
  success: http.get(WEATHER_HISTORY_URL, async ({ request }) => {
    await delay(600)
    const query = new URL(request.url).searchParams
    return HttpResponse.json(
      historyFor({ date: query.get("date") ?? "", hour: Number(query.get("hour")) }),
    )
  }),

  // The API's own 400 for an hour outside the 72-hour window.
  validation: http.get(WEATHER_HISTORY_URL, async () => {
    await delay(600)
    return HttpResponse.json(
      validationProblem({
        hour: ["The hour must be no more than 72 hours before the current hour."],
      }),
      { status: 400 },
    )
  }),

  serverError: http.get(WEATHER_HISTORY_URL, async () => {
    await delay(600)
    return HttpResponse.json(serverErrorProblem("An unexpected error occurred."), { status: 500 })
  }),

  // Makes the intercepted fetch reject like a real network failure.
  networkError: http.get(WEATHER_HISTORY_URL, () => HttpResponse.error()),
}
