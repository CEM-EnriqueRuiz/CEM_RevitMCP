import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCoordManagePhasesTool(server: McpServer) {
  server.tool(
    "coord_manage_phases",
    "Manage phases. action=list returns phases and phase filters. action=set_element_phase sets Phase Created and/or Phase Demolished on the given elementIds (phase by name or id; pass phaseDemolished='None' to clear demolition). Per-element failures are reported, not fatal.",
    {
      action: z.enum(["list", "set_element_phase"]).default("list").describe("Operation"),
      elementIds: z.array(z.number().int()).optional().describe("Elements to update (set_element_phase)"),
      phaseCreated: z.string().optional().describe("Phase name/id to set as Phase Created"),
      phaseDemolished: z.string().optional().describe("Phase name/id to set as Phase Demolished; 'None' clears it"),
    },
    async (args) => {
      const params = {
        action: args.action,
        elementIds: args.elementIds ?? [],
        phaseCreated: args.phaseCreated ?? "",
        phaseDemolished: args.phaseDemolished ?? "",
      };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("coord_manage_phases", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `coord_manage_phases failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
