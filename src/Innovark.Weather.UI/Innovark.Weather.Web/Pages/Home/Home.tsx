import { useEffect } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import { useMutation } from "@tanstack/react-query"
import { useForm } from "react-hook-form"
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
  ControlledField,
  DatePicker,
  FieldGroup,
  Input,
  Spinner,
  toProblemState,
  useFormAction,
} from "@innovark-weather/components"
import type { GetWeatherHistoryParams } from "@web/client/api/generated/model"
import {
  getWeatherHistory,
  type GetWeatherHistoryQueryError,
  type GetWeatherHistoryQueryResult,
} from "@web/client/api/generated/web-api"
import { AppLayout } from "@web/client/components/AppLayout"

import { WeatherHistoryTable } from "./WeatherHistoryTable"
import { weatherHistorySchema, type WeatherHistoryFormValues } from "./weather-history-schema"

const FORM_ID = "weather-history-form"

function WeatherHistoryForm() {
  // A valid submit hands over the endpoint's query parameters (see the schema).
  const form = useForm<WeatherHistoryFormValues, unknown, GetWeatherHistoryParams>({
    resolver: zodResolver(weatherHistorySchema()),
    mode: "onSubmit",
    reValidateMode: "onChange",
    defaultValues: { date: "", hour: undefined },
  })

  // Sent on submit, every time, even for the same date and hour (e.g. to retry after an error).
  // getWeatherHistory is Orval's generated fetch function for the endpoint (typed parameters and
  // response, errors thrown as ProblemDetails); useMutation keeps the latest submit's result.
  const {
    mutate: submit,
    isPending,
    isError,
    data,
    error,
  } = useMutation<
    GetWeatherHistoryQueryResult,
    GetWeatherHistoryQueryError,
    GetWeatherHistoryParams
  >({
    mutationFn: (params) => getWeatherHistory(params),
  })
  const { alertMessage } = toProblemState(error)

  // Only errors get an alert; a successful response shows as the table.
  const {
    FormAlert: ErrorAlert,
    openFormAlertAndFocus: openErrorAlertAndFocus,
    setIsFormAlertOpen: setIsErrorAlertOpen,
  } = useFormAction({ message: alertMessage, variant: "error", className: "w-full" })

  useEffect(() => {
    if (isError) {
      openErrorAlertAndFocus()
    } else {
      setIsErrorAlertOpen(false)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- react only to the request's outcome
  }, [isError])

  return (
    <Card>
      <CardHeader>
        <CardTitle>Weather history</CardTitle>
        <CardDescription>
          Ho Chi Minh City, in UTC+7. Choose an hour within the last 72 hours.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form id={FORM_ID} noValidate onSubmit={form.handleSubmit((params) => submit(params))}>
          <FieldGroup className="gap-4">
            <ControlledField
              control={form.control}
              node={{
                name: "date",
                label: "Date",
                required: true,
                render: ({ field, fieldState }) => (
                  <DatePicker
                    id={field.name}
                    value={field.value}
                    onValueChange={(value) => field.onChange(value ?? "")}
                    onBlur={field.onBlur}
                    disabled={isPending}
                    aria-invalid={fieldState.invalid}
                  />
                ),
              }}
            />
            <ControlledField
              control={form.control}
              node={{
                name: "hour",
                label: "Hour",
                required: true,
                description: "0 to 23",
                render: ({ field, fieldState }) => (
                  <Input
                    {...field}
                    // The form holds a number: the input's valueAsNumber, or undefined when it's
                    // empty (an unparsable entry also reads as empty in a number input).
                    value={field.value ?? ""}
                    onChange={(event) =>
                      field.onChange(
                        event.target.value === "" ? undefined : event.target.valueAsNumber,
                      )
                    }
                    id={field.name}
                    inputMode="numeric"
                    type="number"
                    autoComplete="off"
                    disabled={isPending}
                    placeholder="e.g. 14"
                    aria-invalid={fieldState.invalid}
                  />
                ),
              }}
            />
          </FieldGroup>
        </form>
      </CardContent>
      <CardFooter className="justify-end">
        <Button type="submit" form={FORM_ID} disabled={isPending}>
          {isPending && <Spinner />}
          Send
        </Button>
      </CardFooter>
      <CardFooter>
        {ErrorAlert}
        {data && <WeatherHistoryTable history={data} />}
      </CardFooter>
    </Card>
  )
}

export default function App() {
  return (
    <AppLayout>
      <WeatherHistoryForm />
    </AppLayout>
  )
}
