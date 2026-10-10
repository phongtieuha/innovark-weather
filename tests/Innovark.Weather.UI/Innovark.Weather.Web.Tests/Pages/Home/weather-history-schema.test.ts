import { afterEach, describe, expect, it, vi } from "vitest"
import {
  MAX_AGE_HOURS,
  weatherHistorySchema,
  type WeatherHistoryFormValues,
} from "@web/Pages/Home/weather-history-schema"

// 14:30 on 28 September 2026 in UTC+7.
const NOW = new Date("2026-09-28T07:30:00Z")

// What the form can hold: an empty hour input is undefined (see Home.tsx), which the schema's input
// type leaves out since it rejects it.
type FormInput = Omit<WeatherHistoryFormValues, "hour"> & { readonly hour: number | undefined }

function parse(values: FormInput, now: Date = NOW) {
  return weatherHistorySchema(() => now).safeParse(values)
}

// Each field's messages, so a test also sees when a field has more than one.
function messages(values: FormInput, now: Date = NOW) {
  const result = parse(values, now)
  const byField: Record<string, string[]> = {}
  for (const issue of result.error?.issues ?? []) {
    const field = String(issue.path[0])
    byField[field] = [...(byField[field] ?? []), issue.message]
  }
  return byField
}

describe("weatherHistorySchema", () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it("turns a valid form into the endpoint's query parameters", () => {
    const result = parse({ date: "2026-09-28", hour: 14 })

    expect(result).toEqual({ success: true, data: { date: "2026-09-28", hour: 14 } })
  })

  it("allows the last 72 hours", () => {
    expect(MAX_AGE_HOURS).toBe(72)
  })

  describe("date", () => {
    it("is required", () => {
      expect(messages({ date: "", hour: 14 })).toEqual({ date: ["Choose a date."] })
    })

    it("accepts today in UTC+7", () => {
      expect(parse({ date: "2026-09-28", hour: 0 }).success).toBe(true)
    })

    it("rejects a date in the future", () => {
      expect(messages({ date: "2026-09-29", hour: 0 })).toEqual({
        date: ["This date is in the future."],
      })
    })

    it("accepts the earliest date within 72 hours of the current hour", () => {
      // 72 hours before 14:00 on the 28th is 14:00 on the 25th.
      expect(parse({ date: "2026-09-25", hour: 14 }).success).toBe(true)
    })

    it("rejects a date more than 3 days back", () => {
      expect(messages({ date: "2026-09-24", hour: 14 })).toEqual({
        date: ["Choose a date within the last 3 days."],
      })
    })

    it("counts days in UTC+7, not UTC: tomorrow in UTC+7 starts at 17:00 UTC", () => {
      // Still the 28th in UTC+7 (23:59), although it's the 28th in UTC too.
      expect(parse({ date: "2026-09-29", hour: 0 }, new Date("2026-09-28T16:59:59Z")).success).toBe(
        false,
      )
      // Already the 29th in UTC+7 (00:00), while UTC is still on the 28th.
      expect(parse({ date: "2026-09-29", hour: 0 }, new Date("2026-09-28T17:00:00Z")).success).toBe(
        true,
      )
    })

    it("moves the earliest date as the UTC+7 day turns", () => {
      // At 23:59 on the 28th in UTC+7, the current hour is 23:00, and 72 hours before is the 25th.
      expect(
        parse({ date: "2026-09-25", hour: 23 }, new Date("2026-09-28T16:59:59Z")).success,
      ).toBe(true)
      // At 00:00 on the 29th, 72 hours before is 00:00 on the 26th, so the 25th is out.
      expect(messages({ date: "2026-09-25", hour: 23 }, new Date("2026-09-28T17:00:00Z"))).toEqual({
        date: ["Choose a date within the last 3 days."],
      })
    })

    it("reads the clock on each validation, not when the schema is made", () => {
      let now = new Date("2026-09-28T16:59:59Z")
      const schema = weatherHistorySchema(() => now)
      expect(schema.safeParse({ date: "2026-09-29", hour: 0 }).success).toBe(false)

      now = new Date("2026-09-28T17:00:00Z")

      expect(schema.safeParse({ date: "2026-09-29", hour: 0 }).success).toBe(true)
    })

    it("uses the system clock by default", () => {
      vi.useFakeTimers({ toFake: ["Date"] })
      vi.setSystemTime(NOW)

      expect(weatherHistorySchema().safeParse({ date: "2026-09-29", hour: 0 }).success).toBe(false)
      expect(weatherHistorySchema().safeParse({ date: "2026-09-28", hour: 0 }).success).toBe(true)
    })
  })

  describe("hour", () => {
    it("is required", () => {
      expect(messages({ date: "2026-09-28", hour: undefined })).toEqual({
        hour: ["Enter an hour."],
      })
    })

    it.each([0, 23])("accepts %i", (hour) => {
      expect(parse({ date: "2026-09-28", hour }).success).toBe(true)
    })

    it.each([
      ["a number input's unparsable text (NaN)", Number.NaN],
      ["a fraction", 1.5],
      ["a negative hour", -1],
      ["a negative fraction", -1.5],
      ["an hour past 23", 24],
    ])("rejects %s with one message", (_, hour) => {
      expect(messages({ date: "2026-09-28", hour })).toEqual({
        hour: ["Enter a whole hour from 0 to 23."],
      })
    })
  })

  it("reports both fields at once", () => {
    expect(messages({ date: "", hour: undefined })).toEqual({
      date: ["Choose a date."],
      hour: ["Enter an hour."],
    })
  })
})
