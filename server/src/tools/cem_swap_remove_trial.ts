import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCemSwapRemoveTrialTool(server: McpServer) {
  server.tool(
    "cem_swap_remove_trial",
    "CEMENGAL TOOL (CEM Swap) — take away the CEM Swap trial standing in the model (from cem_swap_run mode 'trial', or left by the window) and put back the type values it changed. Running cem_swap_run again already replaces the trial; use this to drop it without another.",
    {},
    async () => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("cem_swap_remove_trial", {}));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `cem_swap_remove_trial failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
