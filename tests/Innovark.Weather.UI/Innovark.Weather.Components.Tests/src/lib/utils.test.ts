import { describe, expect, it } from "vitest"
import { cn } from "@innovark-weather/components"

describe("cn", () => {
  it("joins class names and skips falsy ones", () => {
    expect(cn("flex", false, undefined, null, "gap-2")).toBe("flex gap-2")
  })

  it("accepts conditional objects and arrays", () => {
    expect(cn(["px-2", { "text-sm": true, hidden: false }])).toBe("px-2 text-sm")
  })

  it("lets a later Tailwind class override a conflicting earlier one", () => {
    expect(cn("h-9 px-4", "h-12")).toBe("px-4 h-12")
  })
})
