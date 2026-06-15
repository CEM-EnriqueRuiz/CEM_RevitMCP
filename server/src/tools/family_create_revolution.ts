import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number().default(0) });

export function registerFamilyCreateRevolutionTool(server: McpServer) {
  server.tool(
    "family_create_revolution",
    "FAMILY EDITOR ONLY (fails clearly in a project): create a solid or void revolution from a closed profile (mm) revolved about an axis (axisStart→axisEnd, mm, on the sketch plane) through start/end angles (degrees, default 0..360).",
    {
      profile: z.array(point).min(3).describe("Closed profile vertices (mm)"),
      axisStart: point.describe("Revolve axis start (mm)"),
      axisEnd: point.describe("Revolve axis end (mm)"),
      startAngle: z.number().default(0).describe("Start angle (deg)"),
      endAngle: z.number().default(360).describe("End angle (deg)"),
      plane: z.enum(["XY", "XZ", "YZ"]).default("XZ").describe("Sketch-plane normal"),
      isSolid: z.boolean().default(true).describe("Solid (true) or void cut (false)"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("family_create_revolution", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `family_create_revolution failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
