import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const sharedParameter = z
  .object({
    name: z.string().describe("Parameter name (required)"),
    dataType: z
      .enum([
        "Text",
        "Integer",
        "Number",
        "Length",
        "Area",
        "Volume",
        "Angle",
        "YesNo",
        "Material",
        "URL",
      ])
      .default("Text")
      .describe("Parameter data type"),
    definitionGroup: z
      .string()
      .default("MCP")
      .describe("Group name inside the shared parameter file (created if missing)"),
    parameterGroup: z
      .enum([
        "Data",
        "Text",
        "IdentityData",
        "Dimensions",
        "Construction",
        "General",
        "Materials",
      ])
      .default("Data")
      .describe("UI group where the parameter appears in the properties palette"),
    isInstance: z
      .boolean()
      .default(true)
      .describe("True = instance binding, false = type binding"),
    categories: z
      .array(z.string())
      .min(1)
      .describe('BuiltInCategory names to bind to, e.g. ["OST_Walls","OST_Doors"]'),
    varyBetweenGroups: z
      .boolean()
      .default(false)
      .describe("Allow values to vary between group instances (instance parameters only)"),
    guid: z
      .string()
      .optional()
      .describe("Optional explicit GUID; a new one is generated/reused if omitted"),
  })
  .describe("A single shared parameter definition + category binding");

export function registerCreateSharedParameterTool(server: McpServer) {
  server.tool(
    "create_shared_parameter",
    "Create shared parameter definitions in the shared parameter file (auto-creates a temp file if none is configured) and bind them to categories as instance or type parameters. Existing definitions and bindings are reused instead of failing. Returns per-parameter results including whether each already existed.",
    {
      data: z.array(sharedParameter).min(1).describe("List of shared parameters to create"),
    },
    async (args, extra) => {
      const params = { data: args.data };
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_shared_parameter", params);
        });

        return {
          content: [{ type: "text", text: JSON.stringify(response, null, 2) }],
        };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `Create shared parameter failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
