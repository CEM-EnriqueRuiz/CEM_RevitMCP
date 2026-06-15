import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerParentReconstructNativeTool(server: McpServer) {
  server.tool(
    "parent_reconstruct_native",
    "PARENT TOOL — write. Rebuild a linked model's elements of one category as native Revit elements (robust DirectShape copy of the source solids). Idempotent: each rebuild is tagged with a CEMAI app-id and carries a back-pointer to its source element, so re-running with clearPrevious removes the prior pass first. This replaces the hand-written link→native send_code_to_revit recipes. Validate the result with parent_validate_deviation.",
    {
      linkName: z.string().optional().describe("Revit link name or id; omit for the first loaded link"),
      category: z.string().describe("Source category to reconstruct (e.g. OST_Walls, OST_StructuralFraming, OST_StructuralColumns)"),
      mode: z.enum(["directshape", "native"]).optional().describe("'directshape' (robust generic solid copy, default) or 'native' (reserved; currently falls back to directshape)"),
      clearPrevious: z.boolean().optional().describe("Delete the prior CEMAI-tagged reconstruction for this category before rebuilding. Default true (idempotent)"),
      roundMm: z.number().optional().describe("Rounding step in mm for any derived sizing. Default 5"),
      maxElements: z.number().int().optional().describe("Cap on source elements processed. Default 2000"),
    },
    async (args) => {
      const params = {
        linkName: args.linkName ?? "",
        category: args.category,
        mode: args.mode ?? "directshape",
        clearPrevious: args.clearPrevious ?? true,
        roundMm: args.roundMm ?? 5,
        maxElements: args.maxElements ?? 2000,
      };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("parent_reconstruct_native", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `parent_reconstruct_native failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
