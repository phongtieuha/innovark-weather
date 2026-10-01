import type { IApiProblemDetails, IProblemState } from "./models"

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

// Turns the error of a mutation made with `fetchApiAsync` (e.g. an Orval hook's `error`) into what a
// form shows: the field errors of a validation problem, and an alert message for any problem.
function toProblemState(problem: IApiProblemDetails | null | undefined): IProblemState {
  const fieldErrors =
    problem?.errors && Object.keys(problem.errors).length > 0 ? problem.errors : undefined
  const title = problem?.title ?? undefined
  const traceId = problem?.traceId ?? undefined

  return {
    fieldErrors,
    title,
    traceId,
    alertMessage: problemAlertMessage(title, fieldErrors, traceId),
  }
}

export { toProblemState }
