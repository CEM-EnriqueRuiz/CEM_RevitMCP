import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCoordLinkModelTool(server: McpServer) {
  server.tool(
    "coord_link_model",
    "Manage linked models. action=list returns all Revit/CAD links with status and instances; load links a file by absolute path (.rvt creates a RevitLink type+instance; .dwg/.dxf/.dgn as a CAD link); reload/unload/remove act on an existing Revit link by id or name. IFC: open+link the resulting .rvt instead.",
    {
      action: z.enum(["list", "load", "reload", "unload", "remove"]).default("list").describe("Operation"),
      path: z.string().optional().describe("Absolute file path (load): .rvt, .dwg, .dxf, .dgn"),
      linkId: z.string().optional().describe("Link id or name (reload/unload/remove)"),
    },
    async (args) => {
      const params = { ...args, path: args.path ?? "", linkId: args.linkId ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("coord_link_model", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `coord_link_model failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
