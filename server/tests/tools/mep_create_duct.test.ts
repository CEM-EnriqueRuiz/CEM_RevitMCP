// Verifies: server/src/tools/mep_create_duct.ts zod schema (mep_* domain).
// Traceability: CEM_RevitMCP.md section 7 build-order item 2 ("MEP (7)"); confirms the
// duct segment contract (round diameter XOR rectangular width/height, both optional at
// the schema level per the tool's own description) stays stable. Predates the
// Cemengal-authored layer; see server/tests/TESTS.md.
import { describe, expect, it } from "vitest";
import { registerMepCreateDuctTool } from "../../src/tools/mep_create_duct.js";
import { captureToolSchema } from "../helpers/captureToolSchema.js";

describe("mep_create_duct schema", () => {
  it("accepts a minimal round-duct segment", async () => {
    const schema = await captureToolSchema(registerMepCreateDuctTool, "mep_create_duct");
    const result = schema.safeParse({
      segments: [{ start: { x: 0, y: 0, z: 0 }, end: { x: 3000, y: 0, z: 0 }, diameter: 200 }],
    });
    expect(result.success).toBe(true);
  });

  it("accepts a rectangular-duct segment with width/height and optional type fields", async () => {
    const schema = await captureToolSchema(registerMepCreateDuctTool, "mep_create_duct");
    const result = schema.safeParse({
      segments: [{ start: { x: 0, y: 0, z: 0 }, end: { x: 3000, y: 0, z: 0 }, width: 300, height: 200 }],
      typeName: "Default",
      systemTypeName: "Supply Air",
      level: "Level 1",
    });
    expect(result.success).toBe(true);
  });

  it("accepts a segment with no diameter/width/height (all optional at schema level)", async () => {
    const schema = await captureToolSchema(registerMepCreateDuctTool, "mep_create_duct");
    const result = schema.safeParse({
      segments: [{ start: { x: 0, y: 0, z: 0 }, end: { x: 3000, y: 0, z: 0 } }],
    });
    expect(result.success).toBe(true);
  });

  it("rejects an empty segments array (min 1 enforced)", async () => {
    const schema = await captureToolSchema(registerMepCreateDuctTool, "mep_create_duct");
    const result = schema.safeParse({ segments: [] });
    expect(result.success).toBe(false);
  });

  it("rejects a missing required field (segments)", async () => {
    const schema = await captureToolSchema(registerMepCreateDuctTool, "mep_create_duct");
    const result = schema.safeParse({});
    expect(result.success).toBe(false);
  });

  it("rejects a wrong type for diameter (string instead of number)", async () => {
    const schema = await captureToolSchema(registerMepCreateDuctTool, "mep_create_duct");
    const result = schema.safeParse({
      segments: [{ start: { x: 0, y: 0, z: 0 }, end: { x: 3000, y: 0, z: 0 }, diameter: "200" }],
    });
    expect(result.success).toBe(false);
  });
});
