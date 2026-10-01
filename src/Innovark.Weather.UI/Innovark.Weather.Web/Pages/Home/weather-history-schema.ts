import * as z from "zod"
import type { GetWeatherHistoryParams } from "@web/client/api/generated/model"

// The form's schema: a valid form becomes the endpoint's query parameters (GetWeatherHistoryParams,
// generated from openapi.json by Orval). The date (in UTC+7) must not be in the future or more than
// 3 days back. The API still checks the exact hour (no later than the current hour, at most 72
// hours before it) and reports it under `hour`.
export const MAX_AGE_HOURS = 72

const HOUR_MS = 60 * 60 * 1000
const UTC7_OFFSET_MS = 7 * HOUR_MS

// "yyyy-MM-dd" of the calendar day in UTC+7 at the given instant.
function utc7DateOf(instantMs: number): string {
  return new Date(instantMs + UTC7_OFFSET_MS).toISOString().slice(0, 10)
}

function currentHourMs(now: Date): number {
  return Math.floor(now.getTime() / HOUR_MS) * HOUR_MS
}

// Dates compare as "yyyy-MM-dd" strings, which sort like the dates themselves.
function isInTheFuture(date: string, latestMs: number): boolean {
  return date > utc7DateOf(latestMs)
}

function isWithinLastThreeDays(date: string, latestMs: number): boolean {
  return date >= utc7DateOf(latestMs - MAX_AGE_HOURS * HOUR_MS)
}

// `satisfies` checks that a valid form is the endpoint's query parameters, while the form's own
// input type is still inferred (WeatherHistoryFormValues, below).
export function weatherHistorySchema(now: () => Date = () => new Date()) {
  return (
    z
      // The form's fields, with the form's own messages. `abort` stops at a field's first failed
      // check, so each field shows one message.
      .object({
        date: z
          .string()
          .min(1, { message: "Choose a date.", abort: true })
          .refine((date) => !isInTheFuture(date, currentHourMs(now())), {
            message: "This date is in the future.",
            abort: true,
          })
          .refine((date) => isWithinLastThreeDays(date, currentHourMs(now())), {
            message: "Choose a date within the last 3 days.",
          }),
        // A number already: the input hands over its valueAsNumber, or undefined when it's empty.
        hour: z
          .number({
            error: (issue) =>
              issue.input === undefined ? "Enter an hour." : "Enter a whole hour from 0 to 23.",
          })
          .int({ message: "Enter a whole hour from 0 to 23.", abort: true })
          .min(0, { message: "Enter a whole hour from 0 to 23.", abort: true })
          .max(23, { message: "Enter a whole hour from 0 to 23." }),
      }) satisfies z.ZodType<GetWeatherHistoryParams>
  )
}

// What the form holds, inferred from the schema.
export type WeatherHistoryFormValues = z.input<ReturnType<typeof weatherHistorySchema>>
