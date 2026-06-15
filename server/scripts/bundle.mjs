// Builds a single, fully self-contained ESM bundle of the MCP server:
//   dist/index.js   — everything inlined (MCP SDK + ws + zod + all tools + your code)
//
// There are no native dependencies, so the result is one cross-platform file.
// Run: npm run bundle      (regenerates the static tool registry first, then bundles)
// Run the result: node dist/index.js   — no `npm install`, no node_modules at the target.
import { build } from "esbuild";
import { mkdirSync, rmSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = join(__dirname, "..");
const dist = join(root, "dist");

rmSync(dist, { recursive: true, force: true });
mkdirSync(dist, { recursive: true });

await build({
  entryPoints: [join(root, "src", "index.bundle.ts")],
  bundle: true,
  platform: "node",
  format: "esm",
  target: "node20",
  outfile: join(dist, "index.js"),
  // esbuild injects __filename/__dirname/require shims for ESM output as needed.
});

console.error("[bundle] done -> dist/index.js (single self-contained file)");
