import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCemSwapFindTypesTool(server: McpServer) {
  server.tool(
    "cem_swap_find_types",
    "CEMENGAL TOOL (CEM Swap, read-only) — the element types of the open model as CEM Swap lists them: category, family, type, how many are placed, and whether CEM Swap can place one (creatable, with the reason when not). Use it to name the 'sources' (types to replace, placed > 0) and the 'target' (creatable) of a cem_swap_run.",
    {
      text: z.string().optional().describe("Text contained in 'Category Family Type' (case-insensitive). Omit for all types"),
      placedOnly: z.boolean().optional().describe("Only types with placed elements (candidates to replace). Default false"),
      max: z.number().int().positive().optional().describe("Cap on the types returned. Default 100"),
    },
    async (args) => {
      const params = { text: args.text ?? "", placedOnly: args.placedOnly ?? false, max: args.max ?? 100 };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("cem_swap_find_types", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `cem_swap_find_types failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
