import { afterEach, describe, expect, it } from "vitest"
import { act, screen } from "@testing-library/react"
import type { Root } from "react-dom/client"
import { createApp } from "@web/client/common"

function Greeting({ name = "nobody", city }: { readonly name?: string; readonly city?: string }) {
  return (
    <p>
      Hello {name}
      {city && ` from ${city}`}
    </p>
  )
}

describe("createApp", () => {
  let root: Root | undefined

  afterEach(() => {
    act(() => root?.unmount())
    root = undefined
    document.body.innerHTML = ""
    delete window.siteData
  })

  function mount(selector = "#app", props?: Parameters<typeof createApp>[1]) {
    act(() => {
      root = createApp(Greeting as never, props).mount(selector)
    })
  }

  it("renders the component into the container", () => {
    document.body.innerHTML = '<div id="app"></div>'

    mount()

    expect(document.querySelector("#app")).toContainElement(screen.getByText("Hello nobody"))
  })

  it("passes the page model's data (window.siteData) as props, and clears it", () => {
    document.body.innerHTML = '<div id="app"></div>'
    window.siteData = { name: "Tony" }

    mount()

    expect(screen.getByText("Hello Tony")).toBeInTheDocument()
    expect(window.siteData).toBeUndefined()
  })

  it("passes its own props too, with the page model's data taking precedence", () => {
    document.body.innerHTML = '<div id="app"></div>'
    window.siteData = { name: "Tony" }

    mount("#app", { name: "ignored", city: "Ho Chi Minh City" } as never)

    expect(screen.getByText("Hello Tony from Ho Chi Minh City")).toBeInTheDocument()
  })

  it("returns the React root it mounted", () => {
    document.body.innerHTML = '<div id="app"></div>'

    mount()

    expect(root).toBeDefined()
  })

  it("does nothing when the container isn't on the page, and keeps the data", () => {
    window.siteData = { name: "Tony" }

    mount("#missing")

    expect(root).toBeUndefined()
    expect(document.body).toBeEmptyDOMElement()
    expect(window.siteData).toEqual({ name: "Tony" })
  })
})
