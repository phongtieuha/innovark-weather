// Run by Orval after generating (hooks.afterAllFilesWrite in orval.config.ts), with what it wrote as
// arguments: files, and the models folder. Orval writes query-parameter types (e.g. GetWeatherHistoryParams) without `readonly`
// and has no setting for it, unlike the models it builds from schemas, so this adds it to every field
// of every `export type …Params = { … }`. Safe to run more than once.
import { readdirSync, readFileSync, statSync, writeFileSync } from "node:fs"
import path from "node:path"

// A field line inside the type, e.g. `hour: number;` or `"page-size"?: number;`, not yet readonly.
const FIELD = /^(\s*)(?!readonly\s)([A-Za-z_$][\w$]*|"[^"]+"|'[^']+')(\??):/

// The arguments' files, with folders expanded to the files directly inside them.
const files = process.argv
  .slice(2)
  .flatMap((arg) =>
    statSync(arg).isDirectory() ? readdirSync(arg).map((name) => path.join(arg, name)) : [arg],
  )
  .filter((file) => /Params\.ts$/.test(file))

let changed = 0
for (const file of files) {
  const lines = readFileSync(file, "utf8").split("\n")
  let inParams = false
  const updated = lines.map((line) => {
    if (/^export type \w+Params = \{\s*$/.test(line)) inParams = true
    else if (inParams && /^\};?\s*$/.test(line)) inParams = false
    else if (inParams && FIELD.test(line)) return line.replace(FIELD, "$1readonly $2$3:")
    return line
  })
  if (updated.join("\n") !== lines.join("\n")) {
    writeFileSync(file, updated.join("\n"))
    changed++
  }
}
console.log(`readonly-params: made ${changed} parameter type(s) readonly`)
