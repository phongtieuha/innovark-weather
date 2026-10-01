import { QueryClient } from "@tanstack/react-query"

import type { IApiProblemDetails } from "./models"

// Longer than any server-side timeout behind it (the web app waits up to 20 s for the API), so a
// slow request ends with the server's own error rather than a generic timeout here.
const REQUEST_TIMEOUT_MS = 30_000

// Paths in the OpenAPI document start with "/", which fetch would resolve from the site root,
// ignoring <base href>. Resolving them against the page's base URL keeps calls working when the app
// is served under a sub-path.
function resolveAgainstBase(url: string): string {
  return /^[a-z][a-z\d+.-]*:/i.test(url)
    ? url
    : new URL(url.replace(/^\//, ""), document.baseURI).href
}

// `fetch` rejects when the request never reaches a server (offline, DNS failure, CORS block,
// timeout). Normalizing that into the same problem shape means callers handle "no response" and
// "error response" the same way.
function networkErrorProblem(error: unknown): IApiProblemDetails {
  return {
    type: "about:blank",
    title: "Unable to reach the server. Check your network connection and try again.",
    status: 0,
    detail: error instanceof Error ? error.message : undefined,
  }
}

// The fetch function for Orval's generated clients (its "mutator"), usable on its own too. Returns
// the parsed JSON body; throws the ProblemDetails body, or a problem built from the status, when the
// response isn't 2xx. A bodiless response (e.g. `Results.Ok()`) returns undefined.
async function fetchApiAsync<T>(url: string, init: RequestInit = {}): Promise<T> {
  const timeout = AbortSignal.timeout(REQUEST_TIMEOUT_MS)
  let response: Response
  try {
    response = await fetch(resolveAgainstBase(url), {
      ...init,
      signal: init.signal ? AbortSignal.any([init.signal, timeout]) : timeout,
    })
  } catch (error) {
    throw networkErrorProblem(error)
  }

  const text = await response.text()
  const body: unknown = text ? JSON.parse(text) : undefined
  if (!response.ok) {
    throw (
      (body as IApiProblemDetails | undefined) ?? {
        title: response.statusText || `Request failed with status ${response.status}.`,
        status: response.status,
      }
    )
  }
  return body as T
}

// A QueryClient for API calls through `fetchApiAsync`. TanStack Query's default networkMode
// ("online") pauses a mutation while `navigator.onLine` is false, so it would never settle; "always"
// sends it and surfaces the network error instead.
function createQueryClient(): QueryClient {
  return new QueryClient({ defaultOptions: { mutations: { networkMode: "always" } } })
}

export { createQueryClient, fetchApiAsync }
