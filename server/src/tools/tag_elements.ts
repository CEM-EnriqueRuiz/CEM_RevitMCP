import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerTagElementsTool(server: McpServer) {
  server.tool(
    "tag_elements",
    "Tag elements in a view — either an explicit list of elementIds, or every element of the given categories visible in the view. A sensible tag type is chosen per element when tagTypeName is omitted. Generalizes tag_walls / tag_rooms to any category.",
    {
      elementIds: z.array(z.number().int()).optional().describe("Explicit elements to tag; omit to use categories"),
      categories: z
        .array(z.string())
        .optional()
        .describe('BuiltInCategory names to tag, e.g. ["OST_Walls","OST_Doors"] (used when elementIds omitted)'),
      viewId: z.number().int().default(-1).describe("View to tag in; -1 = active view"),
      tagTypeName: z.string().optional().describe("Tag family type name or id; omit for a sensible default"),
      addLeader: z.boolean().default(false).describe("Add a leader line"),
      orientation: z.enum(["Horizontal", "Vertical"]).default("Horizontal").describe("Tag orientation"),
    },
    async (args) => {
      const params = {
        elementIds: args.elementIds ?? [],
        categories: args.categories ?? [],
        viewId: args.viewId,
        tagTypeName: args.tagTypeName ?? "",
        addLeader: args.addLeader,
        orientation: args.orientation,
      };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("tag_elements", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `tag_elements failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
