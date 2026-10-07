import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCemRulesApplyTool(server: McpServer) {
  server.tool(
    "cem_rules_apply",
    "CEMENGAL TOOL (CEM Rules) — run every CEM_Rules rule stored in the model (Project Information, then each type/instance) over the WHOLE model, exactly as the CEM Rules button does, and return its report: errors, failed passes, failed elements, elements owned by other users (skipped), types missing parameters, unique values suffixed _N. It writes parameters across the model: ask the user first. Some rules raise their own error dialogs in Revit, which the user must close. Editing rules is Nested Manager's job (cem_open_tool).",
    {
      maxDetails: z.number().int().positive().optional().describe("Cap on the errors, missing-parameter and duplicate lines returned. Default 30"),
    },
    async (args) => {
      const params = { maxDetails: args.maxDetails ?? 30 };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("cem_rules_apply", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `cem_rules_apply failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
