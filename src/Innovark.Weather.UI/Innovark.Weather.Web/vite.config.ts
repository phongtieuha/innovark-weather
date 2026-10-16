import fs from "node:fs"
import path from "node:path"
import react from "@vitejs/plugin-react"
import tailwindcss from "@tailwindcss/vite"
import klawSync, { type Item } from "klaw-sync"
import { defineConfig } from "vite"

const componentsSrc = path.resolve(import.meta.dirname, "../Innovark.Weather.Components/src")

// One entry per Razor page: a `.ts` file next to a `.cshtml` of the same name
// (Pages/Home/Home.ts beside Pages/Home/Home.cshtml). Other `.ts` files (e.g. schemas) are
// modules, not entries. Keys drop the extension, so the output is Pages/Home/Home-<hash>.js,
// which is what BaseModel.JsPath globs for.
function getPageEntries(dirName: string): Record<string, string> {
  const files = klawSync(path.resolve(import.meta.dirname, dirName), {
    nodir: true,
    filter: (item: Item) =>
      item.stats.isDirectory() ||
      (item.path.endsWith(".ts") && fs.existsSync(item.path.replace(/\.ts$/, ".cshtml"))),
  })

  return Object.fromEntries(
    files.map((file) => {
      const relativePath = path.relative(import.meta.dirname, file.path)
      return [relativePath.replace(/\.ts$/, ""), file.path]
    }),
  )
}

const isDev = process.argv.includes("dev")

export default defineConfig({
  plugins: [tailwindcss(), react()],
  // The production build is served from /js/build/ (a subfolder of wwwroot), so asset URLs Vite
  // writes itself (e.g. fonts referenced from CSS) must be prefixed with it.
  base: isDev ? "/" : "/js/build/",
  build: {
    outDir: "wwwroot/js/build",
    emptyOutDir: true,
    assetsDir: "",
    target: "esnext",
    rolldownOptions: {
      input: getPageEntries("Pages"),
      output: {
        manualChunks: (id: string) => (id.includes("node_modules/") ? "vendor" : undefined),
        chunkFileNames: "chunks/[name]-[hash].js",
        assetFileNames: "assets/[name]-[hash][extname]",
      },
    },
  },
  server: {
    port: 5173,
    strictPort: true,
    // The page is served by ASP.NET (launchSettings.json's :5048), not by Vite, so URLs Vite
    // writes into hot-injected CSS must name Vite's own origin or they'd 404 on Kestrel.
    origin: "http://localhost:5173",
    cors: { origin: "http://localhost:5048" },
  },
  resolve: {
    alias: [
      { find: "@web", replacement: path.resolve(import.meta.dirname, "./") },
      // @innovark-weather/components ships raw TSX that imports its own files through "@/*" (the
      // shadcn convention), so that alias is mirrored here for this app's Vite to resolve it.
      { find: "@", replacement: componentsSrc },
    ],
  },
})
