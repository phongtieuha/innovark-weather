import { useMutation } from "@tanstack/react-query"

import type {
  IApiProblemDetails,
  IApiValidationProblemDetails,
  IUseApiCallOptions,
  IUseApiCallResult,
} from "./models"

const REQUEST_TIMEOUT_MS = 15_000

function isValidationProblem(problem: IApiProblemDetails): problem is IApiValidationProblemDetails {
  return "errors" in problem
}

// A bodiless response (e.g. `Results.Ok()`) is valid; `response.json()` would throw on it.
async function parseResponseAsync<TResponse>(response: Response): Promise<TResponse> {
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
  return body as TResponse
}

// `fetch` rejects instead of resolving when the request never reaches a server (offline, DNS
// failure, CORS block, ...). Normalizing that into the same `IApiProblemDetails` shape means every
// consumer keeps working through `problem`/`title` without special-casing "no response".
function networkErrorProblem(error: unknown): IApiProblemDetails {
  return {
    type: "about:blank",
    title: "Unable to reach the server. Check your network connection and try again.",
    status: 0,
    detail: error instanceof Error ? error.message : undefined,
  }
}

// Races the request against a timeout, so the hook always settles even when a request hangs.
function rejectAfterTimeoutAsync(ms: number): Promise<never> {
  return new Promise((_resolve, reject) => {
    setTimeout(() => reject(new Error(`Request timed out after ${ms}ms`)), ms)
  })
}

function problemAlertMessage(
  title: string | undefined,
  fieldErrors: Readonly<Record<string, readonly string[]>> | undefined,
  traceId: string | undefined,
) {
  if (!title && !fieldErrors) return ""
  return (
    <>
      {title && <p>{title}</p>}
      {fieldErrors && (
        <ul className="list-disc pl-5 text-sm font-normal">
          {Object.entries(fieldErrors).map(([field, messages]) => (
            <li key={field}>
              <span className="font-medium">{field}</span>: {messages.join(" ")}
            </li>
          ))}
        </ul>
      )}
      {traceId && <p className="text-muted-foreground mt-1 text-xs">Trace ID: {traceId}</p>}
    </>
  )
}

// Thin `useMutation` wrapper that speaks the problem-details contract .NET minimal APIs emit
// natively, so any form built on it gets consistent field-level vs. generic error handling.
function useApiCall<TResponse, TRequest = void>({
  request,
}: IUseApiCallOptions<TRequest>): IUseApiCallResult<TResponse, TRequest> {
  const mutation = useMutation<TResponse, IApiProblemDetails, TRequest>({
    // The default `networkMode: "online"` pauses a mutation while `navigator.onLine` is false, so
    // it would never settle. "always" attempts the request and surfaces a real error instead.
    networkMode: "always",
    mutationFn: async (variables) => {
      let response: Response
      try {
        response = await Promise.race([
          request(variables),
          rejectAfterTimeoutAsync(REQUEST_TIMEOUT_MS),
        ])
      } catch (error) {
        throw networkErrorProblem(error)
      }
      return parseResponseAsync<TResponse>(response)
    },
  })

  const fieldErrors =
    mutation.error && isValidationProblem(mutation.error) ? mutation.error.errors : undefined
  const problem = mutation.error && !fieldErrors ? mutation.error : undefined

  return {
    submit: (variables) => mutation.mutate(variables),
    isLoading: mutation.isPending,
    isIdle: mutation.isIdle,
    isSuccess: mutation.isSuccess,
    isError: mutation.isError,
    data: mutation.data,
    fieldErrors,
    problem,
    title: mutation.error?.title,
    traceId: mutation.error?.traceId,
    reset: mutation.reset,
    alertMessage: problemAlertMessage(mutation.error?.title, fieldErrors, mutation.error?.traceId),
  }
}

export { useApiCall }
