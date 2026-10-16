// The UI tests use the UI workspace's Prettier config. Its plugins are resolved from there, since
// they're installed in that workspace's node_modules, not next to this file.
import { readFileSync } from "node:fs"
import { createRequire } from "node:module"

const uiWorkspace = new URL("../../src/Innovark.Weather.UI/", import.meta.url)
const config = JSON.parse(readFileSync(new URL(".prettierrc", uiWorkspace), "utf8"))
const require = createRequire(new URL("package.json", uiWorkspace))

export default {
  ...config,
  plugins: config.plugins.map((plugin) => require.resolve(plugin)),
}
