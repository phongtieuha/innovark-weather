import { useEffect } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
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
  useApiCall,
  useFormAction,
} from "@innovark-weather/components"
import { AppLayout } from "@web/client/components/AppLayout"

import {
  weatherHistorySchema,
  type IWeatherHistoryRequest,
  type WeatherHistoryFormValues,
} from "./weather-history-schema"

const FORM_ID = "weather-history-form"

function WeatherHistoryForm() {
  const form = useForm<WeatherHistoryFormValues>({
    resolver: zodResolver(weatherHistorySchema()),
    mode: "onSubmit",
    reValidateMode: "onChange",
    defaultValues: { date: "", hour: "" },
  })

  const { submit, isLoading, isSuccess, isError, data, alertMessage } = useApiCall<
    IWeatherHistoryRequest,
    IWeatherHistoryRequest
  >({
    request: (variables) =>
      fetch("/api/weather/history", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(variables),
      }),
  })

  const {
    FormAlert: SuccessAlert,
    openFormAlertAndFocus: openSuccessAlertAndFocus,
    setIsFormAlertOpen: setIsSuccessAlertOpen,
  } = useFormAction({
    message: data
      ? `Request sent for ${data.date} at ${String(data.hour).padStart(2, "0")}:00 (UTC+7).`
      : "Request sent.",
    variant: "success",
    className: "w-full",
  })

  const {
    FormAlert: ErrorAlert,
    openFormAlertAndFocus: openErrorAlertAndFocus,
    setIsFormAlertOpen: setIsErrorAlertOpen,
  } = useFormAction({ message: alertMessage, variant: "error", className: "mb-6" })

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
    <Card>
      <CardHeader>
        <CardTitle>Weather history</CardTitle>
        <CardDescription>
          Ho Chi Minh City, in UTC+7. Choose an hour within the last 72 hours.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form
          id={FORM_ID}
          noValidate
          onSubmit={form.handleSubmit((values) =>
            submit({ date: values.date, hour: Number(values.hour) }),
          )}
        >
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
                    id={field.name}
                    inputMode="numeric"
                    type="number"
                    autoComplete="off"
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
        <Button type="submit" form={FORM_ID} disabled={isLoading}>
          {isLoading && <Spinner />}
          Send
        </Button>
      </CardFooter>
      <CardFooter>
        {SuccessAlert}
        {ErrorAlert}
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
