import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");

const segment = z.object({
  start: point.describe("Wall start (mm)"),
  end: point.describe("Wall end (mm)"),
  height: z.number().optional().describe("This wall's height in mm (overrides default)"),
});

export function registerArchCreateWallTool(server: McpServer) {
  server.tool(
    "arch_create_wall",
    "Create straight walls between points (mm). Wall type and level are resolved by name (or defaulted). Height comes from the segment or the request default. Optional base offset (mm), flip, and structural flag.",
    {
      segments: z.array(segment).min(1).describe("Wall runs to create"),
      typeName: z.string().optional().describe("Wall type name or id"),
      level: z.string().optional().describe("Level name or id; omit for lowest level"),
      height: z.number().default(3000).describe("Default wall height in mm"),
      baseOffset: z.number().default(0).describe("Base offset from level in mm"),
      flip: z.boolean().default(false).describe("Flip the wall orientation"),
      structural: z.boolean().default(false).describe("Create as a structural wall"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("arch_create_wall", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `arch_create_wall failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
