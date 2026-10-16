/// <reference types="vitest/config" />
import path from "node:path"
import { defineConfig } from "vite"
import react from "@vitejs/plugin-react"
import tailwindcss from "@tailwindcss/vite"
import { storybookTest } from "@storybook/addon-vitest/vitest-plugin"
import { playwright } from "@vitest/browser-playwright"

const dirname = import.meta.dirname

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    // @innovark-weather/components ships raw TSX that imports its own files through "@/*" (the
    // shadcn convention), so that alias is mirrored here for Storybook's Vite to resolve it.
    alias: [
      { find: "@", replacement: path.resolve(dirname, "../Innovark.Weather.Components/src") },
    ],
  },
  test: {
    projects: [
      {
        extends: true,
        // Runs every story as a test: it must render, pass its play function, and pass the
        // a11y checks (`a11y: { test: "error" }` in .storybook/preview.tsx).
        plugins: [storybookTest({ configDir: path.join(dirname, ".storybook") })],
        test: {
          name: "storybook",
          browser: {
            enabled: true,
            headless: true,
            provider: playwright({}),
            instances: [{ browser: "chromium" }],
          },
        },
      },
    ],
  },
})
