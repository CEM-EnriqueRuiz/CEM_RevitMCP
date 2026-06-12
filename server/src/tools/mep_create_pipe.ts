import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");

const segment = z.object({
  start: point.describe("Run start (mm)"),
  end: point.describe("Run end (mm)"),
  diameter: z.number().optional().describe("Pipe diameter in mm"),
});

export function registerMepCreatePipeTool(server: McpServer) {
  server.tool(
    "mep_create_pipe",
    "Create straight pipes between points (mm). Piping system type, pipe type and level are resolved by name (or defaulted). Optional diameter per segment (mm).",
    {
      segments: z.array(segment).min(1).describe("Pipe runs to create"),
      typeName: z.string().optional().describe("Pipe type name or id"),
      systemTypeName: z.string().optional().describe("Piping system type name or id"),
      level: z.string().optional().describe("Level name or id; omit for lowest level"),
    },
    async (args) => {
      const params = { segments: args.segments, typeName: args.typeName ?? "", systemTypeName: args.systemTypeName ?? "", level: args.level ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("mep_create_pipe", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `mep_create_pipe failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
