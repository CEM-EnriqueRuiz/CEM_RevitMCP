// Verifies: server/src/tools/arch_create_wall.ts zod schema (arch_* domain).
// Traceability: CEM_RevitMCP.md section 4 "The anatomy of one tool" — "TS tool zod
// schema mirrors the C# DTO field-for-field"; this checks the mm-in-mm-out point/segment
// contract described in section 4's Conventions #1. Predates the Cemengal-authored
// layer; see server/tests/TESTS.md.
import { describe, expect, it } from "vitest";
import { registerArchCreateWallTool } from "../../src/tools/arch_create_wall.js";
import { captureToolSchema } from "../helpers/captureToolSchema.js";

describe("arch_create_wall schema", () => {
  it("accepts a minimal single-segment payload relying on defaults", async () => {
    const schema = await captureToolSchema(registerArchCreateWallTool, "arch_create_wall");
    const result = schema.safeParse({
      segments: [{ start: { x: 0, y: 0, z: 0 }, end: { x: 5000, y: 0, z: 0 } }],
    });
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.height).toBe(3000);
      expect(result.data.baseOffset).toBe(0);
      expect(result.data.flip).toBe(false);
    }
  });

  it("accepts multiple fully-specified segments plus optional fields", async () => {
    const schema = await captureToolSchema(registerArchCreateWallTool, "arch_create_wall");
    const result = schema.safeParse({
      segments: [
        { start: { x: 0, y: 0, z: 0 }, end: { x: 5000, y: 0, z: 0 }, height: 3500 },
        { start: { x: 5000, y: 0, z: 0 }, end: { x: 5000, y: 5000, z: 0 } },
      ],
      typeName: "Generic - 200mm",
      level: "Level 1",
      height: 3000,
      baseOffset: 100,
      flip: true,
      structural: true,
    });
    expect(result.success).toBe(true);
  });

  it("rejects an empty segments array (min 1 enforced)", async () => {
    const schema = await captureToolSchema(registerArchCreateWallTool, "arch_create_wall");
    const result = schema.safeParse({ segments: [] });
    expect(result.success).toBe(false);
  });

  it("rejects a missing required field (segments)", async () => {
    const schema = await captureToolSchema(registerArchCreateWallTool, "arch_create_wall");
    const result = schema.safeParse({});
    expect(result.success).toBe(false);
  });

  it("rejects a segment point missing a coordinate", async () => {
    const schema = await captureToolSchema(registerArchCreateWallTool, "arch_create_wall");
    const result = schema.safeParse({
      segments: [{ start: { x: 0, y: 0 }, end: { x: 5000, y: 0, z: 0 } }],
    });
    expect(result.success).toBe(false);
  });

  it("rejects a wrong type for a coordinate (string instead of number)", async () => {
    const schema = await captureToolSchema(registerArchCreateWallTool, "arch_create_wall");
    const result = schema.safeParse({
      segments: [{ start: { x: "0", y: 0, z: 0 }, end: { x: 5000, y: 0, z: 0 } }],
    });
    expect(result.success).toBe(false);
  });
});
