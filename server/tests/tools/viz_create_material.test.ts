// Verifies: server/src/tools/viz_create_material.ts zod schema (viz_* domain,
// Autodesk.Revit.DB.Visual per CEM_RevitMCP.md section 2's namespace map).
// Traceability: CEM_RevitMCP.md section 5 "Materials/Appearance (viz_*)" toolset entry;
// this exercises the numeric bound contracts (colorRgb 0-255 x3, transparency 0-100,
// shininess 0-128, smoothness 0-100) the tool's own description promises the LLM.
// Predates the Cemengal-authored layer; see server/tests/TESTS.md.
import { describe, expect, it } from "vitest";
import { registerVizCreateMaterialTool } from "../../src/tools/viz_create_material.js";
import { captureToolSchema } from "../helpers/captureToolSchema.js";

describe("viz_create_material schema", () => {
  it("accepts a minimal payload with only the required name", async () => {
    const schema = await captureToolSchema(registerVizCreateMaterialTool, "viz_create_material");
    const result = schema.safeParse({ name: "Concrete" });
    expect(result.success).toBe(true);
  });

  it("accepts a fully-specified payload within bounds", async () => {
    const schema = await captureToolSchema(registerVizCreateMaterialTool, "viz_create_material");
    const result = schema.safeParse({
      name: "Steel",
      colorRgb: [120, 120, 130],
      transparency: 0,
      shininess: 64,
      smoothness: 50,
      surfacePatternName: "Diagonal",
      cutPatternName: "Solid black",
      appearanceAssetName: "Metal - Brushed",
    });
    expect(result.success).toBe(true);
  });

  it("rejects a missing required field (name)", async () => {
    const schema = await captureToolSchema(registerVizCreateMaterialTool, "viz_create_material");
    const result = schema.safeParse({ transparency: 50 });
    expect(result.success).toBe(false);
  });

  it("rejects colorRgb with fewer than 3 entries", async () => {
    const schema = await captureToolSchema(registerVizCreateMaterialTool, "viz_create_material");
    const result = schema.safeParse({ name: "Glass", colorRgb: [10, 20] });
    expect(result.success).toBe(false);
  });

  it("rejects a colorRgb component out of range (>255)", async () => {
    const schema = await captureToolSchema(registerVizCreateMaterialTool, "viz_create_material");
    const result = schema.safeParse({ name: "Glass", colorRgb: [10, 20, 300] });
    expect(result.success).toBe(false);
  });

  it("rejects transparency out of range (>100)", async () => {
    const schema = await captureToolSchema(registerVizCreateMaterialTool, "viz_create_material");
    const result = schema.safeParse({ name: "Glass", transparency: 150 });
    expect(result.success).toBe(false);
  });

  it("rejects shininess out of range (>128)", async () => {
    const schema = await captureToolSchema(registerVizCreateMaterialTool, "viz_create_material");
    const result = schema.safeParse({ name: "Glass", shininess: 200 });
    expect(result.success).toBe(false);
  });

  it("rejects a non-integer transparency", async () => {
    const schema = await captureToolSchema(registerVizCreateMaterialTool, "viz_create_material");
    const result = schema.safeParse({ name: "Glass", transparency: 50.5 });
    expect(result.success).toBe(false);
  });
});
