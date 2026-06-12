import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerFamilySetFormulaTool(server: McpServer) {
  server.tool(
    "family_set_formula",
    "Set or clear a formula on a parameter of the family open in the Family Editor (an .rfa). Pass an empty formula to clear it. Fails clearly if the active document is a project, not a family.",
    {
      parameterName: z.string().describe("Family parameter name"),
      formula: z.string().optional().describe("Formula expression; empty to clear"),
    },
    async (args) => {
      const params = { parameterName: args.parameterName, formula: args.formula ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("family_set_formula", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `family_set_formula failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
