import { useEffect, useState } from "react"
import type { Meta, StoryObj } from "@storybook/react-vite"
import { useQuery } from "@tanstack/react-query"
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
  Spinner,
  fetchApiAsync,
  toProblemState,
  useFormAction,
  type IApiProblemDetails,
} from "@innovark-weather/components"
import { expect, userEvent, waitFor, within } from "storybook/test"

import {
  weatherHistoryHandlers,
  type IWeatherHistoryParams,
  type IWeatherHistoryResponse,
} from "../../mocks/handlers"

const PARAMS: IWeatherHistoryParams = { date: "2026-09-28", hour: 14 }
const HISTORY_URL = `/api/weather/history?date=${PARAMS.date}&hour=${PARAMS.hour}`

function DataView({ data }: { readonly data: unknown }) {
  return (
    <pre tabIndex={0} className="bg-muted overflow-auto rounded-md p-3 text-xs">
      {JSON.stringify(data, null, 2)}
    </pre>
  )
}

// GETs the web app's /api/weather/history, answered by MSW (src/mocks/handlers.ts), the way the
// Orval-generated hook does: useQuery over fetchApiAsync, which throws ProblemDetails on errors.
// toProblemState turns them into `alertMessage`; useFormAction shows it and moves focus to it.
function ApiCallDemo() {
  // The query runs once Send is clicked; clicking again asks again.
  const [sent, setSent] = useState(false)
  const { isFetching, isSuccess, isError, data, error, refetch } = useQuery<
    IWeatherHistoryResponse,
    IApiProblemDetails
  >({
    queryKey: ["weatherHistory", PARAMS],
    queryFn: ({ signal }) => fetchApiAsync(HISTORY_URL, { signal }),
    enabled: sent,
  })
  const { alertMessage } = toProblemState(error)

  const {
    FormAlert: SuccessAlert,
    openFormAlertAndFocus: openSuccessAlertAndFocus,
    setIsFormAlertOpen: setIsSuccessAlertOpen,
  } = useFormAction({ message: "Request sent.", variant: "success", className: "mb-4" })
  const {
    FormAlert: ErrorAlert,
    openFormAlertAndFocus: openErrorAlertAndFocus,
    setIsFormAlertOpen: setIsErrorAlertOpen,
  } = useFormAction({ message: alertMessage, variant: "error", className: "mb-4" })

  useEffect(() => {
    if (isSuccess) {
      openSuccessAlertAndFocus()
    } else {
      setIsSuccessAlertOpen(false)
    }

    if (isError) {
      openErrorAlertAndFocus()
    } else {
      setIsErrorAlertOpen(false)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- react only to the request's outcome
  }, [isSuccess, isError])

  return (
    <Card className="max-w-md">
      <CardHeader>
        <CardTitle>GET /api/weather/history</CardTitle>
        <CardDescription>
          With date={PARAMS.date} and hour={PARAMS.hour}
        </CardDescription>
      </CardHeader>
      <CardContent>
        {SuccessAlert}
        {ErrorAlert}
        {!sent && <p className="text-muted-foreground text-sm">No request made yet.</p>}
        {data && <DataView data={data} />}
      </CardContent>
      <CardFooter className="justify-end">
        <Button onClick={() => (sent ? void refetch() : setSent(true))} disabled={isFetching}>
          {isFetching && <Spinner />}
          Send
        </Button>
      </CardFooter>
    </Card>
  )
}

const meta = {
  title: "Application/Api Call",
  component: ApiCallDemo,
  tags: ["autodocs"],
} satisfies Meta<typeof ApiCallDemo>

export default meta
type Story = StoryObj<typeof meta>

// Sends the request and waits for the alert to finish its fade-in.
async function sendAndExpectAlert(canvasElement: HTMLElement, text: string | RegExp) {
  const canvas = within(canvasElement)
  await userEvent.click(canvas.getByRole("button", { name: "Send" }))
  await waitFor(() => expect(canvas.getByText(text)).toBeVisible(), { timeout: 3000 })
}

export const Success: Story = {
  play: async ({ canvasElement }) => {
    await sendAndExpectAlert(canvasElement, "Request sent.")
  },
}

export const ValidationError: Story = {
  parameters: { msw: { handlers: { weatherHistory: weatherHistoryHandlers.validation } } },
  play: async ({ canvasElement }) => {
    await sendAndExpectAlert(canvasElement, /no more than 72 hours/)
  },
}

export const ServerError: Story = {
  parameters: { msw: { handlers: { weatherHistory: weatherHistoryHandlers.serverError } } },
  play: async ({ canvasElement }) => {
    await sendAndExpectAlert(canvasElement, "An unexpected error occurred.")
  },
}

export const NetworkError: Story = {
  parameters: { msw: { handlers: { weatherHistory: weatherHistoryHandlers.networkError } } },
  play: async ({ canvasElement }) => {
    await sendAndExpectAlert(canvasElement, /Unable to reach the server/)
  },
}
