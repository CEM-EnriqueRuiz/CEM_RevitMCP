import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const typeKey = z.object({
  family: z.string().describe("Family name, exactly as cem_swap_find_types returns it"),
  type: z.string().describe("Type name, exactly as cem_swap_find_types returns it"),
});

const scope = z.enum(["Instance", "Type"]);

const link = z.object({
  target: z.string().describe("Parameter written on the new element"),
  targetScope: scope.optional().describe("Instance (default) or Type. A Type target is shared by every element of the type: written once, disagreements reported"),
  source: z.string().optional().describe("Parameter read on the old element"),
  sourceScope: scope.optional().describe("Instance (default) or Type of the old element"),
  constant: z.string().optional().describe("Fixed value, as the user would type it (instead of source)"),
  expression: z.string().optional().describe("CEM_Rules expression with {source parameter} tokens in project units (instead of source)"),
});

const swap = z.object({
  name: z.string().optional().describe("Label for the report. Default 'MCP'"),
  sources: z.array(typeKey).min(1).describe("Types whose placed elements are replaced"),
  target: typeKey.describe("The type placed instead (must be creatable)"),
  links: z.array(link).optional().describe("Parameter map. Same-name instance parameters are added automatically, as in the window"),
  offset: z
    .object({
      x: z.number().optional(),
      y: z.number().optional(),
      z: z.number().optional(),
      rotationDegrees: z.number().optional(),
    })
    .optional()
    .describe("Shift of the new element in the OLD element's own axes, in mm, and a turn about its Z in degrees"),
  conditions: z
    .array(
      z.object({
        parameter: z.string(),
        scope: scope.optional(),
        action: z.enum(["Include", "Exclude"]).optional().describe("Default Include"),
        comparison: z.enum(["Equal", "NotEqual", "Contains", "GreaterThan", "LessThan"]).optional().describe("Default Equal"),
        value: z.string(),
      })
    )
    .optional()
    .describe("Which old elements to take, read on the old element"),
  placement: z
    .object({
      mode: z.enum(["Single", "OpeningCentres"]).optional().describe("Single (default): at the old element's origin. OpeningCentres: one new element on each hole of the old one (markers)"),
      expectedCount: z.number().int().positive().optional().describe("Holes expected per old element (OpeningCentres)"),
    })
    .optional(),
  watches: z
    .array(z.object({ source: z.string(), message: z.string() }))
    .optional()
    .describe("Old parameters worth a warning when set: the swap loses something the map cannot express"),
});

export function registerCemSwapRunTool(server: McpServer) {
  server.tool(
    "cem_swap_run",
    "CEMENGAL TOOL (CEM Swap) — replace the placed elements of some types by another type, in the same position (level, host, work plane, flips, optional offset or one per hole) and carrying parameter values, through CEM Swap's own engine. Prefer this to placing + copying + deleting with generic tools. Give a 'preset' (cem_swap_list_presets) or an inline 'swap'. mode 'trial' (default) replaces ONE element (the selected one if it is a candidate), selects it and replaces any earlier trial: screenshot and check it, then run mode 'all' (removes the trial, replaces every candidate in one transaction = one Ctrl+Z) or cem_swap_remove_trial. Old elements are NEVER deleted: the user reviews the pairs and deletes them in the CEM Swap window (cem_open_tool 'CEM Swap'). Group members, nested components and already-replaced elements are skipped and counted.",
    {
      preset: z.string().optional().describe("Name of a preset in SwapPresets.json. Either this or swap"),
      swap: swap.optional().describe("A swap written inline, in the SwapPresets.json shape"),
      mode: z.enum(["trial", "all"]).optional().describe("trial (default): one element; all: every candidate"),
      maxPairs: z.number().int().positive().optional().describe("Cap on the pairs detailed in the report. Default 50"),
    },
    async (args) => {
      if (!args.preset && !args.swap) {
        return { content: [{ type: "text", text: "cem_swap_run needs 'preset' (see cem_swap_list_presets) or 'swap' (sources + target)." }] };
      }
      const params = { preset: args.preset ?? "", swap: args.swap, mode: args.mode ?? "trial", maxPairs: args.maxPairs ?? 50 };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("cem_swap_run", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `cem_swap_run failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
