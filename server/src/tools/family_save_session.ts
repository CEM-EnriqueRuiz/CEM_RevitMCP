import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerFamilySaveSessionTool(server: McpServer) {
  server.tool(
    "family_save_session",
    "Finish a family-edit session: save the open family (Save in place, or SaveAs to a temp .rfa if it has no path), optionally load it back into the project overwriting the existing one, and optionally close it. Pair with family_open_session.",
    {
      familyTitle: z.string().optional().describe("Title of the open family to act on; omit if only one family is open"),
      save: z.boolean().optional().describe("Save the family before loading/closing. Default true"),
      loadIntoProject: z.boolean().optional().describe("Load the saved family back into the project, overwriting. Default false"),
      close: z.boolean().optional().describe("Close the family document after saving/loading. Default false"),
    },
    async (args) => {
      const params = {
        familyTitle: args.familyTitle ?? "",
        save: args.save ?? true,
        loadIntoProject: args.loadIntoProject ?? false,
        close: args.close ?? false,
      };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("family_save_session", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `family_save_session failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
