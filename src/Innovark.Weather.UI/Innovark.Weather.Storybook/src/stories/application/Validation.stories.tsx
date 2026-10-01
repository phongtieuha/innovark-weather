import type { Meta, StoryObj } from "@storybook/react-vite"
import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import * as z from "zod"
import {
  Button,
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
  ControlledField,
  DatePicker,
  FieldGroup,
  Input,
  useFormAction,
} from "@innovark-weather/components"
import { expect, userEvent, within } from "storybook/test"

// A story-sized version of the web app's schema: required fields and the hour's range. The web
// app adds the 72-hour window on top (Pages/Home/weather-history-schema.ts).
const schema = z.object({
  date: z.string().min(1, "Choose a date."),
  hour: z
    .string()
    .trim()
    .min(1, { message: "Enter an hour.", abort: true })
    .refine((hour) => /^\d{1,2}$/.test(hour) && Number(hour) <= 23, {
      message: "Enter a whole hour from 0 to 23.",
    }),
})

type FormValues = z.infer<typeof schema>

// ControlledField gives every field the same label, required mark and collapsing error row;
// useFormAction shows the success alert and moves focus to it.
function ValidationForm() {
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    mode: "onSubmit",
    reValidateMode: "onChange",
    defaultValues: { date: "", hour: "" },
  })
  const values = form.getValues()
  const { FormAlert: SuccessAlert, openFormAlertAndFocus } = useFormAction({
    message: `Valid: ${values.date} at ${values.hour}:00.`,
    className: "mb-6",
  })

  return (
    <Card className="max-w-md">
      <CardHeader>
        <CardTitle>Weather history</CardTitle>
      </CardHeader>
      <CardContent>
        {SuccessAlert}
        <form id="validation-form" noValidate onSubmit={form.handleSubmit(openFormAlertAndFocus)}>
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
        <Button type="submit" form="validation-form">
          Send
        </Button>
      </CardFooter>
    </Card>
  )
}

const meta = {
  title: "Application/Validation",
  component: ValidationForm,
  tags: ["autodocs"],
} satisfies Meta<typeof ValidationForm>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {}

// Smoke checks of the react-hook-form + zod + ControlledField wiring. The schema's own rules are
// unit-tested in the web app.
export const ValidationErrors: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement)
    await userEvent.click(canvas.getByRole("button", { name: "Send" }))

    await expect(await canvas.findByText("Choose a date.")).toBeInTheDocument()
    await expect(canvas.getByText("Enter an hour.")).toBeInTheDocument()

    // Errors re-validate on change once the form has been submitted.
    await userEvent.type(canvas.getByLabelText(/Hour/), "30")
    await expect(await canvas.findByText("Enter a whole hour from 0 to 23.")).toBeInTheDocument()
  },
}

export const Valid: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement)
    await userEvent.type(canvas.getByLabelText(/Date/), "28092026")
    await userEvent.type(canvas.getByLabelText(/Hour/), "14")
    await userEvent.click(canvas.getByRole("button", { name: "Send" }))

    await expect(await canvas.findByText("Valid: 2026-09-28 at 14:00.")).toBeInTheDocument()
  },
}
