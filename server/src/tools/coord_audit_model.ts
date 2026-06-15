import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCoordAuditModelTool(server: McpServer) {
  server.tool(
    "coord_audit_model",
    "Read-only model-health audit: warning count + top recurring warnings, group type/instance counts, in-place family count, link count, design option count, unused-element count (Revit 2024+), and worksharing status. Use to triage a model before cleanup.",
    {},
    async () => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("coord_audit_model", {}));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `coord_audit_model failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
