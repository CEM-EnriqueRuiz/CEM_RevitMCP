#!/usr/bin/env node
// Bundle entry point. Identical to index.ts but uses the statically-generated
// tool registrar (register.generated.ts) so esbuild can see every tool.
// The dev/tsc flow keeps using index.ts + register.ts (runtime directory scan).
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { registerTools } from "./tools/register.generated.js";

const server = new McpServer({
    name: "mcp-server-for-revit",
    version: "1.0.0",
});

async function main() {
    registerTools(server);
    const transport = new StdioServerTransport();
    await server.connect(transport);
    console.error("Revit MCP Server start success");
}

main().catch((error) => {
    console.error("Error starting Revit MCP Server:", error);
    process.exit(1);
});
