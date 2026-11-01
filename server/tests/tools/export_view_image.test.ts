// Verifies: server/src/tools/export_view_image.ts zod schema (Core flat name, Views folder; docs/modules/toolset.md).
// The tool exports one view of the active document (a family or a project) to a PNG under
// %TEMP%\Cemengal\RevitMCP\images; the view by id or by name (the active view when neither), the pixel size, and an
// optional detail level applied only for the export. The C# side is Revit-bound (0016); its rollback-only transaction
// group and its read access tag are pinned in tests/guards/modeller-pins.test.ts.
import { describe, expect, it } from "vitest";
import { registerExportViewImageTool } from "../../src/tools/export_view_image.js";
import { captureToolSchema } from "../helpers/captureToolSchema.js";

const schema = () => captureToolSchema(registerExportViewImageTool, "export_view_image");

describe("export_view_image schema", () => {
  it("accepts an empty payload: the active view at the default size", async () => {
    const result = (await schema()).safeParse({});
    expect(result.success).toBe(true);
    if (result.success) expect(result.data.pixelSize).toBe(1600);
  });

  it("accepts a view by id, a pixel size and a detail level", async () => {
    const result = (await schema()).safeParse({ viewId: 12345, pixelSize: 2048, detailLevel: "Fine" });
    expect(result.success).toBe(true);
  });

  it("accepts a view by name", async () => {
    expect((await schema()).safeParse({ viewName: "Front" }).success).toBe(true);
  });

  it("accepts the detail level by name in any case, and normalises it", async () => {
    const result = (await schema()).safeParse({ detailLevel: "coarse" });
    expect(result.success).toBe(true);
    if (result.success) expect(result.data.detailLevel).toBe("Coarse");
    expect((await schema()).safeParse({ detailLevel: "MEDIUM" }).success).toBe(true);
  });

  it("rejects an unknown detail level", async () => {
    expect((await schema()).safeParse({ detailLevel: "Ultra" }).success).toBe(false);
  });

  it("rejects a pixel size out of range or not an integer", async () => {
    for (const pixelSize of [16, 20000, 800.5])
      expect((await schema()).safeParse({ pixelSize }).success, String(pixelSize)).toBe(false);
  });

  it("rejects a non-integer view id", async () => {
    expect((await schema()).safeParse({ viewId: 12.5 }).success).toBe(false);
  });
});
