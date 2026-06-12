import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateProjectParameterTool(server: McpServer) {
  server.tool(
    "create_project_parameter",
    "Create a project parameter bound to categories (instance or type) without a shared parameter file.",
    { name: z.string(), dataType: z.enum(["Text","Integer","Number","Length","Area","Volume","Angle","YesNo","Material"]).default("Text"), group: z.enum(["Data","Text","IdentityData","Dimensions","Construction","General","Materials"]).default("Data"), isInstance: z.boolean().default(true), categories: z.array(z.string()).min(1).describe("BuiltInCategory names") },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("create_project_parameter", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: "create_project_parameter failed: " + (error instanceof Error ? error.message : String(error)) }] };
      }
    }
  );
}
