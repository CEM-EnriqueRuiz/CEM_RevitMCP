import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const association = z
  .object({
    elementId: z.number().int().describe("Target element ElementId"),
    parameterName: z.string().optional().describe("Parameter display name"),
    builtInParameter: z
      .string()
      .optional()
      .describe("BuiltInParameter enum name (takes priority over parameterName)"),
  })
  .describe("One element-parameter association target");

const globalParameter = z
  .object({
    name: z.string().describe("Global parameter name (required; reused if it already exists)"),
    dataType: z
      .enum(["Text", "Integer", "Number", "Length", "Area", "Volume", "Angle", "YesNo"])
      .default("Length")
      .describe("Global parameter data type"),
    value: z
      .union([z.string(), z.number(), z.boolean()])
      .optional()
      .describe("Value to assign. Length/Area/Volume in mm/mm²/mm³, angles in degrees"),
    associations: z
      .array(association)
      .default([])
      .describe("Element parameters to drive with this global parameter"),
    isReporting: z
      .boolean()
      .default(false)
      .describe("Mark as reporting parameter (reads value from the model instead of driving it)"),
  })
  .describe("A single global parameter definition + associations");

export function registerCreateGlobalParameterTool(server: McpServer) {
  server.tool(
    "create_global_parameter",
    "Create global parameters, set their values, and associate them to element parameters so a single project value drives many element parameters. Associations are validated with CanBeAssociatedWithGlobalParameter and per-association errors are reported back. Note: family-parameter association inside the family editor is a different API and would need a separate command.",
    {
      data: z.array(globalParameter).min(1).describe("List of global parameters to create"),
    },
    async (args, extra) => {
      const params = { data: args.data };
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_global_parameter", params);
        });

        return {
          content: [{ type: "text", text: JSON.stringify(response, null, 2) }],
        };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `Create global parameter failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
