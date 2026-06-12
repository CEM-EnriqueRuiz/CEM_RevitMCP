import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");

const segment = z.object({
  start: point.describe("Run start (mm)"),
  end: point.describe("Run end (mm)"),
  diameter: z.number().optional().describe("Conduit diameter in mm"),
});

export function registerMepCreateConduitTool(server: McpServer) {
  server.tool(
    "mep_create_conduit",
    "Create straight conduits between points (mm). Conduit type and level are resolved by name (or defaulted). Optional diameter per segment (mm).",
    {
      segments: z.array(segment).min(1).describe("Conduit runs to create"),
      typeName: z.string().optional().describe("Conduit type name or id"),
      level: z.string().optional().describe("Level name or id; omit for lowest level"),
    },
    async (args) => {
      const params = { segments: args.segments, typeName: args.typeName ?? "", level: args.level ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("mep_create_conduit", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `mep_create_conduit failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
