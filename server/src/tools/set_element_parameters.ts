import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const parameterSetRequest = z
  .object({
    elementIds: z
      .array(z.number().int())
      .min(1)
      .describe("Element ElementIds to modify"),
    parameterName: z
      .string()
      .optional()
      .describe("Parameter display name (used with LookupParameter). Ignored if builtInParameter is set"),
    builtInParameter: z
      .string()
      .optional()
      .describe('BuiltInParameter enum name, e.g. "ALL_MODEL_INSTANCE_COMMENTS". Takes priority over parameterName'),
    value: z
      .union([z.string(), z.number(), z.boolean()])
      .describe("Value to set. Length/Area/Volume numbers are mm/mm²/mm³, angles in degrees; converted to storage type"),
    isTypeParameter: z
      .boolean()
      .default(false)
      .describe("Look up the parameter on the element's type instead of the instance"),
    fallbackToOtherScope: z
      .boolean()
      .default(true)
      .describe("If not found on the chosen scope, also try the other (instance<->type)"),
  })
  .describe("One parameter write request targeting one or more elements");

export function registerSetElementParametersTool(server: McpServer) {
  server.tool(
    "set_element_parameters",
    "Universal parameter writer. Set instance or type parameters by display name or BuiltInParameter across many elements at once, with automatic storage-type detection and unit conversion (mm/deg in). Returns a per-element success/error report so you can retry only the failures.",
    {
      data: z.array(parameterSetRequest).min(1).describe("List of parameter write requests"),
    },
    async (args, extra) => {
      const params = { data: args.data };
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("set_element_parameters", params);
        });

        return {
          content: [{ type: "text", text: JSON.stringify(response, null, 2) }],
        };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `Set element parameters failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
