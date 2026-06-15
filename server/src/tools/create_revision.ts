import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({
  x: z.number().describe("X in mm"),
  y: z.number().describe("Y in mm"),
  z: z.number().describe("Z in mm").default(0),
});

export function registerCreateRevisionTool(server: McpServer) {
  server.tool(
    "create_revision",
    "Create a project revision (description, issued-to/by, issued flag). Optionally draw a revision cloud in a view by providing cloudViewId + a boundary polygon (mm, >=3 points). Returns the revision id and any cloud id.",
    {
      description: z.string().describe("Revision description"),
      issuedTo: z.string().optional().describe("Issued-to"),
      issuedBy: z.string().optional().describe("Issued-by"),
      issued: z.boolean().default(false).describe("Mark the revision as issued"),
      cloudViewId: z.string().optional().describe("View id/name to draw a revision cloud in; omit for no cloud"),
      cloudBoundary: z.array(point).optional().describe("Cloud boundary points (mm, >=3) when cloudViewId is set"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("create_revision", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `create_revision failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
