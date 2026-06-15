import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerFamilyAssociateParameterTool(server: McpServer) {
  server.tool(
    "family_associate_parameter",
    "FAMILY EDITOR ONLY (fails clearly in a project): make a family parametric by associating an element's instance parameter to a family parameter. Create the family parameter first with family_add_parameter. This is what lets a family type drive its geometry/dimensions.",
    {
      elementId: z.number().int().describe("Element id (in the family) whose parameter is driven"),
      elementParameter: z.string().describe("Name of the element's parameter to associate"),
      familyParameter: z.string().describe("Name of the family parameter that will drive it"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("family_associate_parameter", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `family_associate_parameter failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
