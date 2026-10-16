import type { Meta, StoryObj } from "@storybook/react-vite"
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
  Input,
} from "@innovark-weather/components"

const meta = {
  title: "Forms/Field",
  component: Field,
  tags: ["autodocs"],
} satisfies Meta<typeof Field>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => (
    <Field className="max-w-sm">
      <FieldLabel htmlFor="hour">Hour</FieldLabel>
      <Input id="hour" placeholder="e.g. 14" />
      <FieldDescription>0 to 23, in UTC+7.</FieldDescription>
    </Field>
  ),
}

export const Invalid: Story = {
  render: () => (
    <Field data-invalid className="max-w-sm">
      <FieldLabel htmlFor="hour-invalid">Hour</FieldLabel>
      <Input id="hour-invalid" defaultValue="24" aria-invalid />
      <FieldError errors={[{ message: "Enter a whole hour from 0 to 23." }]} />
    </Field>
  ),
}

export const Horizontal: Story = {
  render: () => (
    <FieldGroup className="max-w-md">
      <Field orientation="horizontal">
        <FieldLabel htmlFor="hour-horizontal">Hour</FieldLabel>
        <Input id="hour-horizontal" placeholder="e.g. 14" className="w-32" />
      </Field>
    </FieldGroup>
  ),
}
