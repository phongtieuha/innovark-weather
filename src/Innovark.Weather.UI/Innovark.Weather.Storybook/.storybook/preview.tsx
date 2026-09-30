import { useState } from "react"
import type { Preview } from "@storybook/react-vite"
import { mswLoader } from "msw-storybook-addon/csf3"
import { QueryClient, QueryClientProvider } from "@tanstack/react-query"

import { weatherHistoryHandlers } from "../src/mocks/handlers"
import "../src/globals.css"

// Registers the worker relative to wherever the build is served from, instead of MSW's default
// root-absolute `/mockServiceWorker.js`, so a build deployed under a sub-path still works.
const mswSetup = async () => {
  const { setupWorker } = await import("msw/browser")
  const worker = setupWorker()
  await worker.start({
    quiet: true,
    serviceWorker: { url: `${import.meta.env.BASE_URL}mockServiceWorker.js` },
  })
  return worker
}

const preview: Preview = {
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
    // An accessibility violation fails the story's test, not just the panel.
    a11y: {
      test: "error",
    },
    options: {
      storySort: {
        order: ["Introduction", "Application", "*"],
      },
    },
    // Named handler groups: a story replaces `weatherHistory` with another outcome via
    // `parameters: { msw: { handlers: { weatherHistory: ... } } }`.
    msw: {
      handlers: { weatherHistory: weatherHistoryHandlers.success },
    },
  },
  loaders: [mswLoader(mswSetup)],
  decorators: [
    (Story) => {
      // A new QueryClient per story keeps mutation state from leaking between stories. Retries
      // are off so an error story shows its error at once.
      const [queryClient] = useState(
        () =>
          new QueryClient({
            defaultOptions: { mutations: { retry: false }, queries: { retry: false } },
          }),
      )
      return (
        <QueryClientProvider client={queryClient}>
          <Story />
        </QueryClientProvider>
      )
    },
  ],
}

export default preview
