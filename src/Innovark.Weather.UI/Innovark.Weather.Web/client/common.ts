import { createElement, type Attributes, type ComponentType } from "react"
import { createRoot } from "react-dom/client"

import "./common.css"

// Mounts a page's React app, passing the page model's `Data` (window.siteData) as props.
export function createApp(Component: ComponentType<unknown>, props?: Attributes | null) {
  return {
    mount(selector: string) {
      const container = document.querySelector(selector)
      if (container) {
        const root = createRoot(container)
        const data = window.siteData
        delete window.siteData
        root.render(createElement(Component, { ...props, ...data }))
        return root
      }
    },
  }
}
