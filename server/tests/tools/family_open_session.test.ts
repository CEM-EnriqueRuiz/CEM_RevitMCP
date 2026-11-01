// Verifies: server/src/tools/family_open_session.ts zod schema (docs/modules/family-editing.md).
// hostDocument (QA-2, 2026-11-01): since family_open_session leaves the family active, a second open by familyName
// must not search the active family's nested families. The host defaults to the open project; hostDocument names
// another open document explicitly (a family, to edit one of its nested families). The C# resolution is pinned in
// tests/guards/modeller-pins.test.ts.
import { describe, expect, it } from "vitest";
import { registerFamilyOpenSessionTool } from "../../src/tools/family_open_session.js";
import { captureToolSchema } from "../helpers/captureToolSchema.js";

const schema = () => captureToolSchema(registerFamilyOpenSessionTool, "family_open_session");

describe("family_open_session schema", () => {
  it("accepts an empty payload: the active family", async () => {
    expect((await schema()).safeParse({}).success).toBe(true);
  });

  it("accepts a family by name with an explicit host document, and keeps it", async () => {
    const result = (await schema()).safeParse({ familyName: "Motor", hostDocument: "Operativa" });
    expect(result.success).toBe(true);
    if (result.success) expect(result.data.hostDocument).toBe("Operativa");
  });

  it("accepts a family by path", async () => {
    expect((await schema()).safeParse({ familyPath: "C:\\families\\pump.rfa" }).success).toBe(true);
  });

  it("rejects a non-string host document", async () => {
    expect((await schema()).safeParse({ familyName: "Motor", hostDocument: 3 }).success).toBe(false);
  });
});
