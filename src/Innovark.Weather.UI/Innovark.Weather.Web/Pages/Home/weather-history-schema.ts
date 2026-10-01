import * as z from "zod"

// The same rules the API applies (HistoryRequestValidator): the date and hour are in UTC+7, and the
// requested hour must be no later than the current hour and no more than 72 hours before it.
export const MAX_AGE_HOURS = 72

const HOUR_MS = 60 * 60 * 1000
const UTC7_OFFSET_MS = 7 * HOUR_MS

// "yyyy-MM-dd" of the calendar day in UTC+7 at the given instant.
function utc7DateOf(instantMs: number): string {
  return new Date(instantMs + UTC7_OFFSET_MS).toISOString().slice(0, 10)
}

// The UTC instant of `hour`:00 on `date` in UTC+7.
function utc7InstantOf(date: string, hour: number): number {
  const [year = 0, month = 1, day = 1] = date.split("-").map(Number)
  return Date.UTC(year, month - 1, day, hour) - UTC7_OFFSET_MS
}

function currentHourMs(now: Date): number {
  return Math.floor(now.getTime() / HOUR_MS) * HOUR_MS
}

function isWholeHour(hour: string): boolean {
  return /^\d{1,2}$/.test(hour) && Number(hour) <= 23
}

function isWithinDateWindow(date: string, latestMs: number): boolean {
  return date >= utc7DateOf(latestMs - MAX_AGE_HOURS * HOUR_MS) && date <= utc7DateOf(latestMs)
}

export function weatherHistorySchema(now: () => Date = () => new Date()) {
  return (
    z
      .object({
        // `abort` stops at a field's first failed check, so each field shows one message.
        date: z
          .string()
          .min(1, { message: "Choose a date.", abort: true })
          .refine((date) => isWithinDateWindow(date, currentHourMs(now())), {
            message: "Choose a date within the last 3 days.",
          }),
        hour: z
          .string()
          .trim()
          .min(1, { message: "Enter an hour.", abort: true })
          .refine(isWholeHour, { message: "Enter a whole hour from 0 to 23." }),
      })
      // Zod runs object refinements even when a field failed, so this checks the hour itself only
      // once both fields are valid. The error goes under `hour`, as the API reports it.
      .superRefine(({ date, hour }, ctx) => {
        const latest = currentHourMs(now())
        if (!isWholeHour(hour) || !isWithinDateWindow(date, latest)) return

        const requested = utc7InstantOf(date, Number(hour))
        if (requested > latest) {
          ctx.addIssue({
            code: "custom",
            path: ["hour"],
            message: "This hour hasn't happened yet.",
          })
        } else if (requested < latest - MAX_AGE_HOURS * HOUR_MS) {
          ctx.addIssue({
            code: "custom",
            path: ["hour"],
            message: `Choose an hour within the last ${MAX_AGE_HOURS} hours.`,
          })
        }
      })
  )
}

export type WeatherHistoryFormValues = z.input<ReturnType<typeof weatherHistorySchema>>
