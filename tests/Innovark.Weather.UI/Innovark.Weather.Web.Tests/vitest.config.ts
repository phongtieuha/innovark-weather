import path from "node:path"
import { defineConfig } from "vitest/config"
import react from "@vitejs/plugin-react"

const webApp = path.resolve(
  import.meta.dirname,
  "../../../src/Innovark.Weather.UI/Innovark.Weather.Web",
)
const componentsSrc = path.resolve(
  import.meta.dirname,
  "../../../src/Innovark.Weather.UI/Innovark.Weather.Components/src",
)

export default defineConfig({
  plugins: [react()],
  resolve: {
    // The web app's own aliases (see its vite.config.ts): "@web/*" for its files, and "@/*", which
    // @innovark-weather/components' raw TSX imports its own files through.
    alias: [
      { find: "@web", replacement: webApp },
      { find: "@", replacement: componentsSrc },
    ],
  },
  test: {
    // happy-dom, not jsdom: jsdom has no PointerEvent, which userEvent needs for Radix's
    // Popper-positioned components (the DatePicker's Popover).
    environment: "happy-dom",
    globals: true,
    // Mirrors the web app's folders; the .NET tests in this folder are run by `dotnet test`.
    include: ["Pages/**/*.test.{ts,tsx}", "client/**/*.test.{ts,tsx}"],
    setupFiles: ["./vitest.setup.ts"],
    coverage: {
      provider: "v8",
      // The web app lives in src/, outside this package, which coverage skips by default.
      allowExternal: true,
      reporter: ["text", "html", "lcov"],
      include: [`${webApp}/Pages/**/*.{ts,tsx}`, `${webApp}/client/**/*.{ts,tsx}`],
      exclude: [`${webApp}/client/api/generated/**`, `${webApp}/**/*.d.ts`],
      thresholds: {
        statements: 80,
        branches: 80,
        functions: 80,
        lines: 80,
      },
    },
  },
})
