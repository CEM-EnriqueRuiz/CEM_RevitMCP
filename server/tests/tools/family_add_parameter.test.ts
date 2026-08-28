// Verifies: server/src/tools/family_add_parameter.ts zod schema (family_* domain).
// Traceability: SKILL.md "Decisions and rationale" — "Small composable tools beat a
// monolith" section documents the family_* pack's design; this schema is one of the
// small composable request contracts described there. Predates the Cemengal-authored
// layer; see server/tests/TESTS.md.
import { describe, expect, it } from "vitest";
import { registerFamilyAddParameterTool } from "../../src/tools/family_add_parameter.js";
import { captureToolSchema } from "../helpers/captureToolSchema.js";

describe("family_add_parameter schema", () => {
  it("accepts a minimal valid payload relying on defaults", async () => {
    const schema = await captureToolSchema(registerFamilyAddParameterTool, "family_add_parameter");
    const result = schema.safeParse({ name: "Width" });
    expect(result.success).toBe(true);
    if (result.success) {
      // dataType and group both default
      expect(result.data.dataType).toBe("Text");
      expect(result.data.group).toBe("Data");
      expect(result.data.isInstance).toBe(false);
    }
  });

  it("accepts a fully-specified payload with a formula", async () => {
    const schema = await captureToolSchema(registerFamilyAddParameterTool, "family_add_parameter");
    const result = schema.safeParse({
      name: "Height",
      dataType: "Length",
      group: "Dimensions",
      isInstance: true,
      formula: "Width * 2",
    });
    expect(result.success).toBe(true);
  });

  it("rejects a missing required field (name)", async () => {
    const schema = await captureToolSchema(registerFamilyAddParameterTool, "family_add_parameter");
    const result = schema.safeParse({ dataType: "Length" });
    expect(result.success).toBe(false);
  });

  it("rejects a dataType outside the enum", async () => {
    const schema = await captureToolSchema(registerFamilyAddParameterTool, "family_add_parameter");
    const result = schema.safeParse({ name: "Width", dataType: "Currency" });
    expect(result.success).toBe(false);
  });

  it("rejects a group outside the enum", async () => {
    const schema = await captureToolSchema(registerFamilyAddParameterTool, "family_add_parameter");
    const result = schema.safeParse({ name: "Width", group: "NotARealGroup" });
    expect(result.success).toBe(false);
  });

  it("rejects a wrong type for isInstance (string instead of boolean)", async () => {
    const schema = await captureToolSchema(registerFamilyAddParameterTool, "family_add_parameter");
    const result = schema.safeParse({ name: "Width", isInstance: "true" });
    expect(result.success).toBe(false);
  });
});
