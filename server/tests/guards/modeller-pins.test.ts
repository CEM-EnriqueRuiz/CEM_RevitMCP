// Pins three fixes for the modelling agent (2026-11-01) in the C# source, which runs only inside Revit (exempt, 0016),
// as no-acc-writes.test.ts does for root decision 0031:
// 1. family_open_session leaves the opened .rfa as the ACTIVE document, which the family_* editor tools act on
//    (docs/modules/family-editing.md); family_save_session re-activates the project before it closes the family.
// 2. send_code_to_revit traces the exact code it runs to %TEMP%\Cemengal\RevitMCP\code, and logs a snippet that ends
//    after the timeout as a late completion (docs/decisions/0027-send-code-scripts-traced-to-local-temp.md).
// 3. export_view_image applies the detail level only for the export, in a transaction group that is always rolled
//    back, writes under images\, and is tagged read in command.json (docs/modules/toolset.md).
import { describe, expect, it } from "vitest";
import { readdirSync, readFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const repo = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..", "..");
const read = (path: string) => readFileSync(join(repo, path), "utf8");
// Comments may describe what is pinned; only code counts.
const code = (text: string) => text.replace(/\/\*[\s\S]*?\*\//g, "").replace(/(^|\s)\/\/.*$/gm, "$1");
const between = (text: string, from: string, to?: string) => {
  const start = text.indexOf(from);
  expect(start, `missing ${from}`).toBeGreaterThanOrEqual(0);
  const end = to ? text.indexOf(to, start) : -1;
  return text.slice(start, end > start ? end : undefined);
};

const SESSION = "commandset/Services/Family/FamilySessionEventHandlers.cs";
const SEND_CODE_COMMAND = "commandset/Commands/ExecuteDynamicCode/ExecuteCodeCommand.cs";
const SEND_CODE_HANDLER = "commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs";
const EXPORT_COMMAND = "commandset/Commands/Views/ExportViewImageCommand.cs";
const EXPORT_HANDLER = "commandset/Services/Views/ExportViewImageEventHandler.cs";

describe("family_open_session activates the family it opens", () => {
  it("the open handler activates the family document it resolved", () => {
    const open = between(code(read(SESSION)), "class FamilyOpenSessionEventHandler", "class FamilySaveSessionEventHandler");
    expect(open).toMatch(/famDoc\s*=\s*FamilySessionHelpers\.Activate\(\s*uiApp,\s*famDoc,/);
  });

  it("activation goes through OpenAndActivateDocument, saving a pathless (EditFamily) document to Families first", () => {
    const activate = between(code(read(SESSION)), "public static Document Activate(", "public static ");
    expect(activate).toMatch(
      /string\.IsNullOrEmpty\(famDoc\.PathName\)[\s\S]*?NewFamilyCopyPath\(famDoc\.Title\)[\s\S]*?famDoc\.SaveAs\([\s\S]*?OverwriteExistingFile = false[\s\S]*?OpenAndActivateDocument\(famDoc\.PathName\)/
    );
  });

  it("family_save_session re-activates the project before closing the family (Revit cannot close the active document)", () => {
    const save = between(code(read(SESSION)), "class FamilySaveSessionEventHandler");
    expect(save).toMatch(/OpenAndActivateDocument\(projectDoc\.PathName\)[\s\S]*?famDoc\.Close\(false\)/);
  });
});

describe("send_code_to_revit traces what it runs", () => {
  it("the command writes the exact code to code\\ before it raises the event", () => {
    const command = code(read(SEND_CODE_COMMAND));
    expect(command).toMatch(
      /CemTempFolder\.NewArtifactFile\(\s*McpLocalFiles\.App,\s*"code",[\s\S]*?File\.WriteAllText\(\s*codeFile,\s*code\b[\s\S]*?RaiseAndWaitForCompletion\(/
    );
  });

  it("a timed-out call abandons its run instead of reporting a result it does not have", () => {
    expect(code(read(SEND_CODE_COMMAND))).toMatch(/RaiseAndWaitForCompletion\(\s*\w+\s*\)\s*\|\|\s*!\s*run\.TryAbandon\(\)/);
  });

  it("the result carries the code file, mode, document, outcome and duration", () => {
    const handler = code(read(SEND_CODE_HANDLER));
    for (const key of ["codeFile", "transactionMode", "documentTitle", "isFamilyDocument", "outcome", "durationMs"])
      expect(handler, key).toContain(`[JsonProperty("${key}")]`);
    for (const outcome of ["ok", "error", "rolledBack"]) expect(handler, outcome).toContain(`"${outcome}"`);
  });

  it("an auto-mode commit that Revit rolls back is reported as rolled back", () => {
    expect(code(read(SEND_CODE_HANDLER))).toMatch(/transaction\.Commit\(\)\s*!=\s*TransactionStatus\.Committed/);
  });

  it("a snippet that ends after the timeout appends its own late-completion line to the action log", () => {
    const handler = code(read(SEND_CODE_HANDLER));
    expect(handler).toMatch(/CemTempFolder\.AppendLog\(\s*McpLocalFiles\.App,\s*"actions",[\s\S]*?"\.jsonl"\s*\)/);
    expect(handler).toContain(`["late"] = true`);
  });

  it("waiting never resets the completion signal (a fast completion was lost as a false timeout)", () => {
    const wait = between(code(read(SEND_CODE_HANDLER)), "public bool WaitForCompletion(", "}");
    expect(wait).not.toMatch(/\.Reset\(\)/);
  });

  it("the action log points at the code file instead of logging the body", () => {
    expect(code(read("plugin/Utils/ActionLogger.cs"))).toContain(`"<see result.codeFile>"`);
  });
});

describe("export_view_image", () => {
  it("the command, the TS tool and command.json share the name", () => {
    expect(code(read(EXPORT_COMMAND))).toContain(`CommandName => "export_view_image"`);
    expect(read("server/src/tools/export_view_image.ts")).toContain(`"export_view_image"`);
  });

  it("applies the detail level in a transaction group that is always rolled back", () => {
    const handler = code(read(EXPORT_HANDLER));
    expect(handler).toMatch(/new TransactionGroup\(/);
    expect(handler).toMatch(/finally\s*\{\s*group\.RollBack\(\);/);
    expect(handler).not.toMatch(/\.Assimilate\(|group\.Commit\(/);
  });

  it("writes a new file under images\\, never an existing one", () => {
    expect(code(read(EXPORT_HANDLER))).toMatch(/CemTempFolder\.NewArtifactFile\(\s*McpLocalFiles\.App,\s*"images",/);
  });

  it("is tagged read in command.json, and every access tag is read or write", () => {
    const commands: { commandName: string; access?: string }[] = JSON.parse(read("command.json")).commands;
    expect(commands.find((c) => c.commandName === "export_view_image")?.access).toBe("read");
    const bad = commands.filter((c) => c.access !== undefined && !["read", "write"].includes(c.access));
    expect(bad.map((c) => c.commandName)).toEqual([]);
  });
});

// The fix batch after the QA and security quick reviews (2026-11-01).
describe("review fixes: family sessions", () => {
  it("QA-2: a family opened by name is resolved against the host (the open project by default), not the active document", () => {
    const open = between(code(read(SESSION)), "class FamilyOpenSessionEventHandler", "class FamilySaveSessionEventHandler");
    expect(open).toMatch(/var host = FamilySessionHelpers\.ResolveHost\(\s*uiApp,\s*Request\.HostDocument\s*\)/);
    expect(open).toMatch(/new FilteredElementCollector\(host\)/);
    expect(open).toMatch(/host\.EditFamily\(/);
    expect(open).not.toMatch(/doc\.EditFamily\(|new FilteredElementCollector\(doc\)/);
  });

  it("QA-2: the default host is an open, non-linked project; an explicit hostDocument wins", () => {
    const helpers = between(code(read(SESSION)), "internal static class FamilySessionHelpers", "class FamilyOpenSessionEventHandler");
    expect(helpers).toMatch(/!d\.IsFamilyDocument && !d\.IsLinked/);
    expect(between(helpers, "public static Document ResolveHost(")).toMatch(
      /IsNullOrWhiteSpace\(hostTitle\)[\s\S]*?FindProjectDoc\(/
    );
  });

  it("QA-7: with no hostDocument the ACTIVE project wins over the first project in Application.Documents", () => {
    const resolve = between(code(read(SESSION)), "public static Document ResolveHost(", "public static string NewFamilyCopyPath(");
    expect(resolve).toMatch(
      /IsNullOrWhiteSpace\(hostTitle\)\)\s*\{\s*var active = uiApp\.ActiveUIDocument\?\.Document;\s*if \(active != null && !active\.IsFamilyDocument\) return active;\s*return FindProjectDoc\(app\) \?\? active;/
    );
  });

  it("QA-7: the tool description gives the default host order", () => {
    const tool = read("server/src/tools/family_open_session.ts");
    expect(tool).toContain("the active document if it is a project, else the open project, else the active document");
  });

  it("QA-5: a pathless family's temp copy never overwrites: a new folder per copy keeps the family's name", () => {
    const session = code(read(SESSION));
    expect(session).not.toMatch(/OverwriteExistingFile\s*=\s*true/);
    expect(between(session, "public static string NewFamilyCopyPath(")).toMatch(
      /CemTempFolder\.NewArtifactFile\(\s*McpLocalFiles\.App,\s*"Families",/
    );
    expect(session.match(/NewFamilyCopyPath\(/g)?.length ?? 0).toBeGreaterThanOrEqual(3); // defined, open, save
  });

  it("a family opened by path reuses an open document only when it is the same file, not one with the same name", () => {
    const open = between(code(read(SESSION)), "class FamilyOpenSessionEventHandler", "class FamilySaveSessionEventHandler");
    expect(open).toMatch(/FamilySessionHelpers\.FindOpenFamilyByPath\(\s*app,\s*Request\.FamilyPath\s*\)/);
    expect(between(code(read(SESSION)), "public static Document FindOpenFamilyByPath(")).toMatch(/Path\.GetFullPath\(d\.PathName\)/);
  });
});

describe("review fixes: export_view_image", () => {
  it("QA-3: waiting never resets the completion signal; the command resets it before raising", () => {
    const handler = code(read(EXPORT_HANDLER));
    expect(between(handler, "public bool WaitForCompletion(", "}")).not.toMatch(/\.Reset\(\)/);
    expect(handler).toMatch(/set\s*\{\s*_request = value;\s*_resetEvent\.Reset\(\);/);
  });

  it("SEC-2: an unsafe view name is replaced inside the group, and the predicted file must stay in images\\ before Revit writes", () => {
    const handler = code(read(EXPORT_HANDLER));
    expect(handler).toMatch(
      /group\.Start\(\);[\s\S]*?MakeExportNameSafe\(view[\s\S]*?ImageExportOptions\.GetFileName\(doc,\s*view\.Id\)[\s\S]*?IsDirectlyIn\([\s\S]*?doc\.ExportImage\(options\)/
    );
    expect(between(handler, "private static bool IsDirectlyIn(")).toMatch(/Path\.GetFullPath[\s\S]*?Path\.GetDirectoryName/);
  });

  it("QA-6: the detail level (and the safe name) change only inside the always-rolled-back group", () => {
    const handler = code(read(EXPORT_HANDLER));
    const group = between(handler, "group.Start();", "finally { group.RollBack(); }");
    expect(group).toMatch(/ApplyDetailLevel\(/);
    expect(group).toMatch(/MakeExportNameSafe\(/);
    const outside = handler.replace(group, "");
    expect(outside.match(/ApplyDetailLevel\(/g)?.length).toBe(1); // only its definition
    expect(outside.match(/MakeExportNameSafe\(/g)?.length).toBe(1);
    expect(between(handler, "private void ApplyDetailLevel(")).toMatch(/\.DetailLevel = level/);
    expect(handler.match(/(?<!res)\.DetailLevel\s*=(?!=)/g)?.length).toBe(1); // the result DTO's res.DetailLevel is not the view
  });
});

describe("review fixes: send_code_to_revit", () => {
  it("SEC-6: the trace id is generated here, never taken from the caller", () => {
    const command = code(read(SEND_CODE_COMMAND));
    expect(command).toMatch(/string runId = Guid\.NewGuid\(\)\.ToString\("N"\)/);
    expect(command).toMatch(/TraceCode\(code,\s*runId\)/);
    expect(command).not.toMatch(/parameters\["(runId|id|codeFile|requestId)"\]/);
    expect(between(command, "private static string TraceCode(", "public override object Execute(")).not.toMatch(/requestId/);
  });

  it("0027: a run abandoned before it started gets its 'never ran' line even when a newer call replaces it", () => {
    const handler = code(read(SEND_CODE_HANDLER));
    expect(between(handler, "public ExecutionRun SetExecutionParameters(", "public bool WaitForCompletion(")).toMatch(
      /previous\.TryClaimNeverRan\(\)[\s\S]*?LogLateCompletion\(previous,/
    );
    expect(between(handler, "public void Execute(", "var info = run.Result;")).toMatch(/run\.TryClaimNeverRan\(\)/);
    expect(between(handler, "public bool TryClaimNeverRan(")).toMatch(/_started \|\| !_abandoned \|\| _neverRanClaimed/);
  });
});

describe("review fixes: the skills' counts match the registry", () => {
  it("both skills give the TS tool count and the registry's command count", () => {
    const commands = JSON.parse(read("command.json")).commands.length;
    const tools = readdirSync(join(repo, "server", "src", "tools")).filter(
      (f) => f.endsWith(".ts") && !["index.ts", "register.ts", "register.generated.ts"].includes(f)
    ).length;
    for (const skill of [".claude/skills/cem-aimodeler/SKILL.md", ".claude/skills/cem-revitmcp-dev/SKILL.md"]) {
      const text = read(skill);
      expect(text, skill).toContain(`${tools} MCP tools`);
      expect(text, skill).toContain(`${commands} commands`);
    }
  });
});

// SEC-8 consumer follow-up (2026-11-01): CEM_CSharpExtended's CemTempFolder refuses reparse points in the folders it
// creates; the paths this repo derives below them (a family copy folder, the fixed shared-parameter file) are checked here.
describe("SEC-8 follow-up: no write through a junction or link below the temp folder", () => {
  const LOCAL = "commandset/Utils/McpLocalFiles.cs";
  const SHARED = "commandset/Services/Core/CreateSharedParameterEventHandler.cs";

  it("McpLocalFiles tells a reparse point by its attributes and refuses it with CemTempFolderUnsafeException", () => {
    const local = code(read(LOCAL));
    expect(between(local, "public static bool IsReparsePoint(")).toMatch(/FileAttributes\.ReparsePoint/);
    expect(between(local, "public static void RefuseReparsePoint(")).toMatch(
      /IsReparsePoint\(path\)\)\s*throw new CemTempFolderUnsafeException\(path\)/
    );
  });

  it("NewFamilyCopyPath checks the copy folder before and after creating it, and the file", () => {
    const copy = between(code(read(SESSION)), "public static string NewFamilyCopyPath(", "private static void RemoveEmptyCopyFolders(");
    expect(copy).toMatch(
      /McpLocalFiles\.RefuseReparsePoint\(folder\);\s*System\.IO\.Directory\.CreateDirectory\(folder\);\s*McpLocalFiles\.RefuseReparsePoint\(folder,\s*file\);/
    );
  });

  it("Activate turns a refusal into a clear error before anything is saved", () => {
    const activate = between(code(read(SESSION)), "public static Document Activate(", "public static string FileName(");
    expect(activate).toMatch(/catch \(CemTempFolderUnsafeException ex\)\s*\{\s*throw new InvalidOperationException\(/);
    expect(activate).toMatch(/try\s*\{\s*path = NewFamilyCopyPath\(famDoc\.Title\);\s*\}/);
  });

  it("the fixed shared-parameter file is refused when it or its folder is a reparse point, before and after the write", () => {
    const shared = code(read(SHARED));
    expect(shared).toMatch(
      /McpLocalFiles\.RefuseReparsePoint\(folder,\s*tempPath\);\s*if \(!File\.Exists\(tempPath\)\)\s*File\.WriteAllText\(tempPath, string\.Empty\);\s*McpLocalFiles\.RefuseReparsePoint\(folder,\s*tempPath\);/
    );
  });
});
