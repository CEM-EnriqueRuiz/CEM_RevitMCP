// Verifies: server/src/tools/create_level.ts zod schema (Core domain).
// Traceability: SKILL.md "Non-negotiable conventions" #1 (mm in/out) and #8 (verb_noun
// naming) — this schema is the request-shape contract the LLM fills in. No single
// Cemengal-authored commit introduced this specific tool (it predates the fork's
// Cemengal layer); see server/tests/TESTS.md for the full traceability table.
import { describe, expect, it } from "vitest";
import { registerCreateLevelTool } from "../../src/tools/create_level.js";
import { captureToolSchema } from "../helpers/captureToolSchema.js";

describe("create_level schema", () => {
  it("accepts a minimal valid single-level payload", async () => {
    const schema = await captureToolSchema(registerCreateLevelTool, "create_level");
    const result = schema.safeParse({
      data: [{ name: "Level 2", elevation: 3000 }],
    });
    expect(result.success).toBe(true);
  });

  it("accepts a fully-specified level payload", async () => {
    const schema = await captureToolSchema(registerCreateLevelTool, "create_level");
    const result = schema.safeParse({
      data: [
        {
          name: "Roof",
          elevation: 9000,
          description: "Top level",
          isMainLevel: true,
          isBuildingStory: false,
          computationHeight: 100,
          viewPlanOffset: 0,
          viewSectionOffset: 0,
          viewElevationOffset: 0,
          createFloorPlan: true,
          createCeilingPlan: false,
        },
      ],
    });
    expect(result.success).toBe(true);
  });

  it("rejects a missing required field (elevation)", async () => {
    const schema = await captureToolSchema(registerCreateLevelTool, "create_level");
    const result = schema.safeParse({ data: [{ name: "Level 2" }] });
    expect(result.success).toBe(false);
  });

  it("rejects a missing required field (name)", async () => {
    const schema = await captureToolSchema(registerCreateLevelTool, "create_level");
    const result = schema.safeParse({ data: [{ elevation: 3000 }] });
    expect(result.success).toBe(false);
  });

  it("rejects a wrong type for elevation (string instead of number)", async () => {
    const schema = await captureToolSchema(registerCreateLevelTool, "create_level");
    const result = schema.safeParse({ data: [{ name: "Level 2", elevation: "3000" }] });
    expect(result.success).toBe(false);
  });

  it("rejects an empty data array as an object shape mismatch only when data itself is missing", async () => {
    const schema = await captureToolSchema(registerCreateLevelTool, "create_level");
    // data is required (an array field), but zod does not enforce non-empty here —
    // confirm the array field is genuinely required by omitting it entirely.
    const result = schema.safeParse({});
    expect(result.success).toBe(false);
  });
});
