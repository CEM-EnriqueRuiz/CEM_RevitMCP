import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerFamilyOpenSessionTool(server: McpServer) {
  server.tool(
    "family_open_session",
    "Open a family for editing — the entry point for authoring family geometry. Give familyName to EditFamily a family already loaded in the project, or familyPath to open a standalone .rfa from disk; omit both to use the already-active family document. After this, family_create_extrusion and the other family editor tools act on the open family; finish with family_save_session.",
    {
      familyName: z.string().optional().describe("Name of a family loaded in the project (opened with EditFamily)"),
      familyPath: z.string().optional().describe("Absolute path to an .rfa file (opened from disk). Used when familyName is omitted"),
    },
    async (args) => {
      const params = {
        familyName: args.familyName ?? "",
        familyPath: args.familyPath ?? "",
      };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("family_open_session", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `family_open_session failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
