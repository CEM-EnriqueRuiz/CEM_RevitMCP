import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const filterRule = z
  .object({
    parameterName: z
      .string()
      .optional()
      .describe("Parameter display name (project/shared params resolved by name)"),
    builtInParameter: z
      .string()
      .optional()
      .describe("BuiltInParameter enum name (takes priority over parameterName)"),
    operator: z
      .enum([
        "Equals",
        "NotEquals",
        "Contains",
        "NotContains",
        "BeginsWith",
        "EndsWith",
        "GreaterThan",
        "GreaterOrEqual",
        "LessThan",
        "LessOrEqual",
        "HasValue",
        "HasNoValue",
      ])
      .default("Equals")
      .describe("Comparison operator"),
    value: z
      .union([z.string(), z.number(), z.boolean()])
      .optional()
      .describe("Comparison value. Numeric lengths are in mm. Omit for HasValue/HasNoValue"),
    valueType: z
      .enum(["string", "double", "integer", "elementid"])
      .optional()
      .describe("Value type hint; inferred when omitted"),
    convertMillimeters: z
      .boolean()
      .default(true)
      .describe("For double values, treat as mm and convert to feet"),
  })
  .describe("One filter rule (rules are AND-combined)");

const overrides = z
  .object({
    color: z
      .array(z.number().int().min(0).max(255))
      .length(3)
      .optional()
      .describe("RGB [r,g,b] for lines and surface/cut patterns"),
    solidFill: z.boolean().default(true).describe("Fill surface with a solid pattern in the override color"),
    transparency: z.number().int().min(0).max(100).optional().describe("Surface transparency 0-100"),
    lineWeight: z.number().int().min(1).max(16).optional().describe("Projection line weight 1-16"),
    halftone: z.boolean().default(false).describe("Render matching elements as halftone"),
  })
  .describe("Graphic overrides for matching elements");

const viewFilter = z
  .object({
    name: z.string().describe("Filter name (required; existing filter of same name is updated)"),
    categories: z
      .array(z.string())
      .min(1)
      .describe('BuiltInCategory names the filter applies to, e.g. ["OST_Walls"]'),
    rules: z.array(filterRule).default([]).describe("Filter rules, combined with logical AND"),
    viewId: z.number().int().default(-1).describe("View ElementId to apply to; -1 = active view"),
    applyToView: z
      .boolean()
      .default(true)
      .describe("Apply to a view; false only creates the filter in the project"),
    visible: z.boolean().default(true).describe("Visibility of matching elements in the view"),
    overrides: overrides.optional(),
  })
  .describe("A single view filter definition");

export function registerCreateViewFilterTool(server: McpServer) {
  server.tool(
    "create_view_filter",
    "Create a ParameterFilterElement with AND-combined rules and apply it to a view with graphic overrides (color, solid fill, transparency, line weight, halftone). An existing filter with the same name is updated. Set applyToView=false to only create the filter in the project.",
    {
      // Note: this command takes a single object (not an array).
      data: viewFilter,
    },
    async (args, extra) => {
      const params = { data: args.data };
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_view_filter", params);
        });

        return {
          content: [{ type: "text", text: JSON.stringify(response, null, 2) }],
        };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `Create view filter failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
