import { useEffect, useState } from "react"
import { CircleAlertIcon, CircleCheckIcon } from "lucide-react"

import { cn } from "@/lib/utils"
import { Alert, AlertTitle } from "@/components/display/alert/alert"

import type { IFormAlertProps } from "./models"

// Mirrors the collapsing-row animation in `ControlledField`: mounts collapsed, then flips open on
// the next tick so the height/opacity transition actually plays instead of the alert appearing
// instantly.
function FormAlert({ variant, children, className }: IFormAlertProps) {
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    setVisible(true)
  }, [])

  return (
    <div
      className={cn(
        "overflow-hidden transition-all duration-200 ease-in-out",
        visible ? "max-h-40 opacity-100" : "max-h-0 opacity-0",
        className,
      )}
    >
      <Alert variant={variant === "error" ? "destructive" : "default"}>
        {variant === "error" ? <CircleAlertIcon /> : <CircleCheckIcon />}
        <AlertTitle className="line-clamp-none">{children}</AlertTitle>
      </Alert>
    </div>
  )
}

export { FormAlert }
