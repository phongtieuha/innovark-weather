import { afterEach, beforeEach, describe, expect, it, vi } from "vitest"
import { createQueryClient, fetchApiAsync } from "@innovark-weather/components"

function jsonResponse(body: unknown, init: ResponseInit = {}): Response {
  return new Response(JSON.stringify(body), {
    headers: { "Content-Type": "application/json" },
    ...init,
  })
}

describe("fetchApiAsync", () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal("fetch", fetchMock)
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it("returns the parsed JSON body of a 2xx response", async () => {
    fetchMock.mockResolvedValue(jsonResponse({ temperatureC: 31.2 }))

    const result = await fetchApiAsync<{ temperatureC: number }>("/api/weather/history")

    expect(result).toEqual({ temperatureC: 31.2 })
  })

  it("returns undefined for a bodiless 2xx response", async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }))

    const result = await fetchApiAsync("/api/weather/history")

    expect(result).toBeUndefined()
  })

  it("resolves a site-root path against the page's base URL", async () => {
    fetchMock.mockResolvedValue(jsonResponse({}))

    await fetchApiAsync("/api/weather/history?date=2026-09-28&hour=14")

    expect(fetchMock).toHaveBeenCalledWith(
      new URL("api/weather/history?date=2026-09-28&hour=14", document.baseURI).href,
      expect.anything(),
    )
  })

  it("leaves an absolute URL as it is", async () => {
    fetchMock.mockResolvedValue(jsonResponse({}))

    await fetchApiAsync("https://example.com/api/weather/history")

    expect(fetchMock).toHaveBeenCalledWith(
      "https://example.com/api/weather/history",
      expect.anything(),
    )
  })

  it("passes the request options through, with a timeout signal", async () => {
    fetchMock.mockResolvedValue(jsonResponse({}))

    await fetchApiAsync("/api/weather/history", { method: "GET", headers: { "X-Test": "1" } })

    const init = fetchMock.mock.calls[0]?.[1]
    expect(init).toMatchObject({ method: "GET", headers: { "X-Test": "1" } })
    expect(init?.signal).toBeInstanceOf(AbortSignal)
  })

  it("aborts the request when the caller's signal aborts", async () => {
    fetchMock.mockResolvedValue(jsonResponse({}))
    const controller = new AbortController()

    await fetchApiAsync("/api/weather/history", { signal: controller.signal })
    controller.abort()

    expect(fetchMock.mock.calls[0]?.[1]?.signal?.aborted).toBe(true)
  })

  it("throws the ProblemDetails body of an error response", async () => {
    const problem = {
      title: "One or more validation errors occurred.",
      status: 400,
      errors: { hour: ["The hour must be from 0 to 23."] },
    }
    fetchMock.mockResolvedValue(jsonResponse(problem, { status: 400 }))

    await expect(fetchApiAsync("/api/weather/history")).rejects.toEqual(problem)
  })

  it("throws a problem built from the status text when an error response has no body", async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 502, statusText: "Bad Gateway" }))

    await expect(fetchApiAsync("/api/weather/history")).rejects.toEqual({
      title: "Bad Gateway",
      status: 502,
    })
  })

  it("throws a problem built from the status code when there's no status text either", async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 503, statusText: "" }))

    await expect(fetchApiAsync("/api/weather/history")).rejects.toEqual({
      title: "Request failed with status 503.",
      status: 503,
    })
  })

  it("throws a network problem with status 0 when the request never reaches a server", async () => {
    fetchMock.mockRejectedValue(new TypeError("Failed to fetch"))

    await expect(fetchApiAsync("/api/weather/history")).rejects.toEqual({
      type: "about:blank",
      title: "Unable to reach the server. Check your network connection and try again.",
      status: 0,
      detail: "Failed to fetch",
    })
  })

  it("leaves the network problem's detail empty when the rejection isn't an Error", async () => {
    fetchMock.mockRejectedValue("offline")

    await expect(fetchApiAsync("/api/weather/history")).rejects.toMatchObject({
      status: 0,
      detail: undefined,
    })
  })
})

describe("createQueryClient", () => {
  it("sends requests even while offline, without retries or refetches on focus", () => {
    const { queries, mutations } = createQueryClient().getDefaultOptions()

    expect(queries).toMatchObject({
      networkMode: "always",
      retry: false,
      refetchOnWindowFocus: false,
    })
    expect(mutations).toMatchObject({ networkMode: "always" })
  })
})
