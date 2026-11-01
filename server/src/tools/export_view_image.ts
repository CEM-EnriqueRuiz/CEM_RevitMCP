import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const DETAIL_LEVELS = ["Coarse", "Medium", "Fine"] as const;

export function registerExportViewImageTool(server: McpServer) {
  server.tool(
    "export_view_image",
    "Export ONE view of the active document (a family in the Family Editor, or a project) to a PNG, to check your work visually one view at a time. Unlike take_screenshot it renders the view itself (no ribbon, no other windows), even a view that is not open. Give viewId or viewName (neither = the active view), the width in pixels, and optionally a detail level (Coarse, Medium or Fine) applied ONLY for the export: it is set in a transaction group that is always rolled back, so the document is left exactly as it was. Read-only. The image is a new file under %TEMP%\\Cemengal\\RevitMCP\\images (never overwritten, pruned after 7 days); the result gives its path, size in bytes and pixels. Read the file to see it.",
    {
      viewId: z.number().int().positive().optional().describe("The view's element id"),
      viewName: z.string().optional().describe("The view's name (case-insensitive), used when viewId is absent"),
      pixelSize: z
        .number()
        .int()
        .min(32)
        .max(10000)
        .optional()
        .default(1600)
        .describe("Image width in pixels (the height follows the view). Default 1600"),
      detailLevel: z
        .preprocess(
          (v) => (typeof v === "string" ? DETAIL_LEVELS.find((d) => d.toLowerCase() === v.trim().toLowerCase()) ?? v : v),
          z.enum(DETAIL_LEVELS)
        )
        .optional()
        .describe("Coarse, Medium or Fine (any case), for this export only. Absent = the view's own"),
    },
    async (args) => {
      const params = {
        viewId: args.viewId ?? 0,
        viewName: args.viewName ?? "",
        pixelSize: args.pixelSize,
        detailLevel: args.detailLevel ?? "",
      };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("export_view_image", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `export_view_image failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
