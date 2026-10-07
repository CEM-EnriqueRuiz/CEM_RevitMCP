import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCemOpenToolTool(server: McpServer) {
  server.tool(
    "cem_open_tool",
    "CEMENGAL TOOL — press a button of the Cemengal ribbon tab for the user, exactly as if they clicked it: Filter Manager, Nested Manager, CopyValueTo, Convert Inventor, Numerate, Renamer (materials), Place Pipes, Review Fabrication, Sections Visibility, Export SAT files, CEM Rules, CEM Swap… Call it without 'tool' to list the buttons (label, add-in, enabled). The tool opens when this call returns; most Cemengal windows are modal, so further MCP calls time out until the user closes it (CEM Swap is modeless). Select the elements first when the tool works on the selection. It never presses CEM_AIModeler (that would cut this connection).",
    {
      tool: z.string().optional().describe("Button label ('CEM Swap', 'Nested Manager') or add-in name ('CEM_SwapManager', 'SwapManager'); case and spaces ignored. Omit to list the buttons"),
    },
    async (args) => {
      const params = { tool: args.tool ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("cem_open_tool", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `cem_open_tool failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
