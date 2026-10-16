import { defineConfig } from "orval"

// Generates a typed react-query hook per endpoint of this app (useGetWeatherHistory) and its
// models from the OpenAPI document the Debug build writes. Regenerate with `bun run generate:api`.
export default defineConfig({
  web: {
    input: {
      target: "openapi.json",
      override: {
        // Marks every schema property readOnly, so the models built from schemas are readonly (arrays
        // included; query-parameter types get it from the hook below). Only Orval's in-memory copy
        // changes; the document on disk stays as written.
        transformer: (spec) => {
          for (const schema of Object.values(spec.components?.schemas ?? {})) {
            // OpenAPI 3.1 also allows true/false as a schema; those have no properties.
            if (typeof schema !== "object" || !("properties" in schema)) continue
            // References too (OpenAPI 3.1 allows siblings next to $ref), e.g. `errors`.
            for (const property of Object.values(schema.properties ?? {})) {
              ;(property as { readOnly?: boolean }).readOnly = true
            }
          }
          return spec
        },
      },
    },
    output: {
      target: "client/api/generated/web-api.ts",
      schemas: "client/api/generated/model",
      mode: "split",
      client: "react-query",
      httpClient: "fetch",
      override: {
        // Every request goes through fetchApiAsync, which throws ProblemDetails on errors, so hooks
        // resolve to the response body itself rather than Orval's { data, status, headers } wrapper.
        mutator: { path: "client/api/fetch-api.ts", name: "fetchApiAsync" },
        fetch: { includeHttpResponseReturnType: false },
      },
    },
    // Query-parameter types aren't built from schemas, so the transformer above can't make them
    // readonly, and Orval has no setting for it; this script adds `readonly` to their fields.
    hooks: {
      afterAllFilesWrite: "node scripts/readonly-params.mjs",
    },
  },
})
