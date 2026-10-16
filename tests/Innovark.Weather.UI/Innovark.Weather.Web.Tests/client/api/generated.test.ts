// @vitest-environment node
import { cpSync, mkdtempSync, readdirSync, readFileSync, rmSync } from "node:fs"
import { tmpdir } from "node:os"
import path from "node:path"
import { afterAll, beforeAll, describe, expect, it } from "vitest"
import { generate, type Options } from "orval"
import orvalConfig from "@web/orval.config"

const webApp = path.resolve(
  import.meta.dirname,
  "../../../../../src/Innovark.Weather.UI/Innovark.Weather.Web",
)
const committed = path.join(webApp, "client/api/generated")

// Every file under a folder, as paths relative to it, sorted.
function filesIn(folder: string): string[] {
  return readdirSync(folder, { recursive: true, withFileTypes: true })
    .filter((entry) => entry.isFile())
    .map((entry) => path.relative(folder, path.join(entry.parentPath, entry.name)))
    .sort()
}

// The page's Orval client (client/api/generated) is generated from the committed openapi.json and
// committed itself. This generates it again, with the app's own orval.config.ts, into a temporary
// folder laid out like the app's, so the imports it writes are the same, and compares the two.
// Fix a failure by running ./scripts/generate-api-clients.sh (or `bun run generate:api`).
describe("the generated API client", () => {
  let output: string
  let regenerated: string

  beforeAll(async () => {
    output = mkdtempSync(path.join(tmpdir(), "innovark-orval-"))
    regenerated = path.join(output, "client/api/generated")
    // The mutator, at the same place relative to the output as in the app.
    cpSync(
      path.join(webApp, "client/api/fetch-api.ts"),
      path.join(output, "client/api/fetch-api.ts"),
    )

    // orval.config.ts exports a plain object (defineConfig only types it).
    const web = (orvalConfig as Record<string, Options>).web!
    if (typeof web.output !== "object" || typeof web.output.override?.mutator !== "object") {
      throw new Error("orval.config.ts no longer has the output this test expects.")
    }
    // One project's options (the config's `web`), with its paths made absolute.
    await generate(
      {
        ...web,
        input: { ...(web.input as object), target: path.join(webApp, "openapi.json") },
        output: {
          ...web.output,
          target: path.join(regenerated, "web-api.ts"),
          schemas: path.join(regenerated, "model"),
          override: {
            ...web.output.override,
            mutator: {
              ...web.output.override.mutator,
              path: path.join(output, "client/api/fetch-api.ts"),
            },
          },
        },
        hooks: { afterAllFilesWrite: `node ${path.join(webApp, "scripts/readonly-params.mjs")}` },
      },
      // The app's folder, for the package.json and tsconfig.json Orval reads (e.g. react-query's version).
      webApp,
      { logLevel: "error", throwOnError: true },
    )
  }, 60_000)

  afterAll(() => {
    if (output) rmSync(output, { recursive: true, force: true })
  })

  it("has the same files as a fresh generation", () => {
    expect(filesIn(regenerated)).toContain("web-api.ts")
    expect(filesIn(committed)).toEqual(filesIn(regenerated))
  })

  it("matches a fresh generation from the committed openapi.json", () => {
    for (const file of filesIn(regenerated)) {
      expect
        .soft(readFileSync(path.join(committed, file), "utf8"), file)
        .toBe(readFileSync(path.join(regenerated, file), "utf8"))
    }
  })
})
