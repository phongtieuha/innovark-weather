import path from "node:path"
import { defineConfig } from "vitest/config"
import react from "@vitejs/plugin-react"

const componentsSrc = path.resolve(
  import.meta.dirname,
  "../../../src/Innovark.Weather.UI/Innovark.Weather.Components/src",
)

export default defineConfig({
  plugins: [react()],
  resolve: {
    // @innovark-weather/components ships raw TSX that imports its own files through "@/*" (the
    // shadcn convention), so that alias is mirrored here for Vite to resolve it.
    alias: { "@": componentsSrc },
  },
  test: {
    // happy-dom, not jsdom: jsdom has no PointerEvent, which userEvent needs for Radix's
    // Popper-positioned components (Popover, and so the DatePicker).
    environment: "happy-dom",
    globals: true,
    include: ["src/**/*.test.{ts,tsx}"],
    setupFiles: ["./vitest.setup.ts"],
    coverage: {
      provider: "v8",
      // The components live in src/, outside this package, which coverage skips by default.
      allowExternal: true,
      reporter: ["text", "html", "lcov"],
      include: [`${componentsSrc}/**/*.{ts,tsx}`],
      // Barrels and type-only files hold no code to cover.
      exclude: [
        `${componentsSrc}/**/*.d.ts`,
        `${componentsSrc}/**/index.ts`,
        `${componentsSrc}/**/models.ts`,
      ],
      thresholds: {
        statements: 80,
        branches: 80,
        functions: 80,
        lines: 80,
      },
    },
  },
})
