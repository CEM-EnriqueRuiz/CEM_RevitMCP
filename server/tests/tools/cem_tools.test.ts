// Verifies: the zod schemas of the cem_* tools (server/src/tools/cem_*.ts), the Cemengal add-ins of the
// sibling CEM_RevitAPI repo reached through the MCP (docs/concepts/cemengal-addins-via-mcp.md).
// The C# side (commandset/Services/Cemengal) runs only inside Revit and is exempt (0016).
import { describe, expect, it } from "vitest";
import { registerCemOpenToolTool } from "../../src/tools/cem_open_tool.js";
import { registerCemSwapFindTypesTool } from "../../src/tools/cem_swap_find_types.js";
import { registerCemSwapListPresetsTool } from "../../src/tools/cem_swap_list_presets.js";
import { registerCemSwapRunTool } from "../../src/tools/cem_swap_run.js";
import { registerCemSwapRemoveTrialTool } from "../../src/tools/cem_swap_remove_trial.js";
import { registerCemRulesApplyTool } from "../../src/tools/cem_rules_apply.js";
import { captureToolSchema } from "../helpers/captureToolSchema.js";

describe("cem_open_tool schema", () => {
  it("accepts no tool (list the buttons) and a tool label", async () => {
    const schema = await captureToolSchema(registerCemOpenToolTool, "cem_open_tool");
    expect(schema.safeParse({}).success).toBe(true);
    expect(schema.safeParse({ tool: "CEM Swap" }).success).toBe(true);
  });

  it("rejects a non-string tool", async () => {
    const schema = await captureToolSchema(registerCemOpenToolTool, "cem_open_tool");
    expect(schema.safeParse({ tool: 3 }).success).toBe(false);
  });
});

describe("cem_swap_find_types schema", () => {
  it("accepts an empty and a fully specified search", async () => {
    const schema = await captureToolSchema(registerCemSwapFindTypesTool, "cem_swap_find_types");
    expect(schema.safeParse({}).success).toBe(true);
    expect(schema.safeParse({ text: "Placa", placedOnly: true, max: 20 }).success).toBe(true);
  });

  it("rejects a non-positive or fractional max", async () => {
    const schema = await captureToolSchema(registerCemSwapFindTypesTool, "cem_swap_find_types");
    expect(schema.safeParse({ max: 0 }).success).toBe(false);
    expect(schema.safeParse({ max: 2.5 }).success).toBe(false);
  });
});

describe("cem_swap_list_presets and cem_swap_remove_trial schemas", () => {
  it("take no arguments", async () => {
    const presets = await captureToolSchema(registerCemSwapListPresetsTool, "cem_swap_list_presets");
    const removeTrial = await captureToolSchema(registerCemSwapRemoveTrialTool, "cem_swap_remove_trial");
    expect(presets.safeParse({}).success).toBe(true);
    expect(removeTrial.safeParse({}).success).toBe(true);
  });
});

describe("cem_swap_run schema", () => {
  it("accepts a preset by name, trial by default", async () => {
    const schema = await captureToolSchema(registerCemSwapRunTool, "cem_swap_run");
    expect(schema.safeParse({ preset: "Placas de anclaje" }).success).toBe(true);
    expect(schema.safeParse({ preset: "Placas de anclaje", mode: "all", maxPairs: 10 }).success).toBe(true);
  });

  it("accepts an inline swap in the SwapPresets.json shape", async () => {
    const schema = await captureToolSchema(registerCemSwapRunTool, "cem_swap_run");
    const result = schema.safeParse({
      swap: {
        sources: [{ family: "Old plate", type: "200x200" }],
        target: { family: "New plate", type: "200x200" },
        links: [
          { target: "CEM_Thickness", targetScope: "Instance", source: "Thickness", sourceScope: "Type" },
          { target: "CEM_Thread", targetScope: "Instance", constant: "M" },
          { target: "CEM_Area", targetScope: "Instance", expression: "{Width} * {Height}" },
        ],
        offset: { x: 0, y: 0, z: -15, rotationDegrees: 90 },
        conditions: [{ parameter: "Mark", scope: "Instance", action: "Exclude", comparison: "Contains", value: "TEMP" }],
        placement: { mode: "OpeningCentres", expectedCount: 4 },
        watches: [{ source: "Comments", message: "The comment is not carried over" }],
      },
      mode: "trial",
    });
    expect(result.success).toBe(true);
  });

  it("rejects an unknown mode", async () => {
    const schema = await captureToolSchema(registerCemSwapRunTool, "cem_swap_run");
    expect(schema.safeParse({ preset: "x", mode: "everything" }).success).toBe(false);
  });

  it("rejects an inline swap without sources or target", async () => {
    const schema = await captureToolSchema(registerCemSwapRunTool, "cem_swap_run");
    expect(schema.safeParse({ swap: { target: { family: "F", type: "T" } } }).success).toBe(false);
    expect(schema.safeParse({ swap: { sources: [], target: { family: "F", type: "T" } } }).success).toBe(false);
    expect(schema.safeParse({ swap: { sources: [{ family: "F", type: "T" }] } }).success).toBe(false);
  });

  it("rejects values outside the add-in's enums", async () => {
    const schema = await captureToolSchema(registerCemSwapRunTool, "cem_swap_run");
    const base = { sources: [{ family: "F", type: "A" }], target: { family: "F", type: "B" } };
    expect(schema.safeParse({ swap: { ...base, links: [{ target: "P", targetScope: "Both" }] } }).success).toBe(false);
    expect(schema.safeParse({ swap: { ...base, placement: { mode: "Everywhere" } } }).success).toBe(false);
    expect(schema.safeParse({ swap: { ...base, conditions: [{ parameter: "P", comparison: "Like", value: "x" }] } }).success).toBe(false);
  });
});

describe("cem_rules_apply schema", () => {
  it("accepts no arguments and a detail cap", async () => {
    const schema = await captureToolSchema(registerCemRulesApplyTool, "cem_rules_apply");
    expect(schema.safeParse({}).success).toBe(true);
    expect(schema.safeParse({ maxDetails: 5 }).success).toBe(true);
  });

  it("rejects a non-positive detail cap", async () => {
    const schema = await captureToolSchema(registerCemRulesApplyTool, "cem_rules_apply");
    expect(schema.safeParse({ maxDetails: 0 }).success).toBe(false);
  });
});
