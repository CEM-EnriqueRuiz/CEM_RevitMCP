import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");

const segment = z.object({
  start: point.describe("Run start (mm)"),
  end: point.describe("Run end (mm)"),
  diameter: z.number().optional().describe("Round duct diameter in mm"),
  width: z.number().optional().describe("Rectangular duct width in mm"),
  height: z.number().optional().describe("Rectangular duct height in mm"),
});

export function registerMepCreateDuctTool(server: McpServer) {
  server.tool(
    "mep_create_duct",
    "Create straight ducts between points (mm). System type, duct type and level are resolved by name (or defaulted). Per segment, set either a round diameter or rectangular width/height (mm).",
    {
      segments: z.array(segment).min(1).describe("Duct runs to create"),
      typeName: z.string().optional().describe("Duct type name or id"),
      systemTypeName: z.string().optional().describe("Mechanical system type name or id"),
      level: z.string().optional().describe("Level name or id; omit for lowest level"),
    },
    async (args) => {
      const params = { segments: args.segments, typeName: args.typeName ?? "", systemTypeName: args.systemTypeName ?? "", level: args.level ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("mep_create_duct", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `mep_create_duct failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
