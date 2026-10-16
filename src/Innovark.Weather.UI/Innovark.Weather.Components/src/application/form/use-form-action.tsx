import { useEffect, useRef, useState } from "react"

import { FormAlert as FormAlertComponent } from "./form-alert"
import type { IUseFormActionOptions, IUseFormActionResult } from "./models"

// Owns a form's result alert: whether it's open, and moving focus to it when it opens so screen
// readers announce the outcome and it scrolls into view.
function useFormAction({
  message,
  variant = "success",
  className,
}: IUseFormActionOptions): IUseFormActionResult {
  const [isFormAlertOpen, setIsFormAlertOpen] = useState(false)
  const formAlertRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (isFormAlertOpen) {
      formAlertRef.current?.scrollIntoView?.({ behavior: "smooth", block: "center" })
      formAlertRef.current?.focus()
    }
  }, [isFormAlertOpen])

  return {
    FormAlert: isFormAlertOpen ? (
      <div ref={formAlertRef} tabIndex={-1} className="outline-none">
        <FormAlertComponent variant={variant} className={className}>
          {message}
        </FormAlertComponent>
      </div>
    ) : null,
    isFormAlertOpen,
    setIsFormAlertOpen,
    openFormAlertAndFocus: () => setIsFormAlertOpen(true),
  }
}

export { useFormAction }
