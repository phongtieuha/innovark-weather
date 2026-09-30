import { delay, http, HttpResponse } from "msw"

import { serverErrorProblem, validationProblem } from "./problemDetails"

export interface IWeatherHistoryRequest {
  readonly date: string
  readonly hour: number
}

const WEATHER_HISTORY_URL = "/api/weather/history"

// One handler per outcome of the web app's POST /api/weather/history, so each story can pick the
// response it demonstrates instead of getting a random one.
export const weatherHistoryHandlers = {
  // What the placeholder endpoint does today: 200 OK echoing the request.
  success: http.post(WEATHER_HISTORY_URL, async ({ request }) => {
    await delay(600)
    const body = (await request.json()) as IWeatherHistoryRequest
    return HttpResponse.json(body)
  }),

  // The API's own 400 for an hour outside the 72-hour window.
  validation: http.post(WEATHER_HISTORY_URL, async () => {
    await delay(600)
    return HttpResponse.json(
      validationProblem({
        hour: ["The hour must be no more than 72 hours before the current hour."],
      }),
      { status: 400 },
    )
  }),

  serverError: http.post(WEATHER_HISTORY_URL, async () => {
    await delay(600)
    return HttpResponse.json(serverErrorProblem("An unexpected error occurred."), { status: 500 })
  }),

  // Makes the intercepted fetch reject like a real network failure.
  networkError: http.post(WEATHER_HISTORY_URL, () => HttpResponse.error()),
}
