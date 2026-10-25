// Verifies root decision 0031 (docs/decisions/0026-logs-and-artefacts-in-local-temp.md): nothing of ours writes under
// ACC or next to its own DLL. On colleagues' machines the plugin loads from the ACC menu, so a write beside it, or into
// a hard-coded ACCDocs path, is synced to the whole team. Logs and temporary artefacts go to %TEMP%\Cemengal\RevitMCP
// through CEM_CSharpExtended's CemTempFolder, which prunes itself. The C# runs only inside Revit (exempt, 0016), so
// this guard reads its source: every file write in plugin/ and commandset/ is inventoried here.
import { describe, expect, it } from "vitest";
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const repo = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..", "..");
const SKIP = new Set(["bin", "obj", "node_modules", "build", "dist"]);

function files(folder: string, extensions: string[]): string[] {
  const found: string[] = [];
  for (const name of readdirSync(folder)) {
    const path = join(folder, name);
    if (statSync(path).isDirectory()) {
      if (!SKIP.has(name)) found.push(...files(path, extensions));
    } else if (extensions.some((ext) => name.endsWith(ext))) {
      found.push(path);
    }
  }
  return found;
}

const rel = (path: string) => relative(repo, path).split("\\").join("/");
const read = (path: string) => readFileSync(join(repo, path), "utf8");
// Comments may name what is forbidden; only code counts. "http://" keeps its text (no space before "//").
const code = (text: string) => text.replace(/\/\*[\s\S]*?\*\//g, "").replace(/(^|\s)\/\/.*$/gm, "$1");

const csharp = [...files(join(repo, "plugin"), [".cs"]), ...files(join(repo, "commandset"), [".cs"])];
const projects = [...files(join(repo, "plugin"), [".csproj", ".props", ".targets"]),
  ...files(join(repo, "commandset"), [".csproj", ".props", ".targets"])];
const typescript = files(join(repo, "server", "src"), [".ts"]);

// A file write: create, append, copy, move, replace or delete a file or folder, a Revit save, or pointing Revit at a
// shared parameter file it will write. Bitmap.Save(stream, …) has arguments and is not matched.
const WRITE_API =
  /\bFile\.(WriteAll\w*|AppendAll\w*|AppendText|Create\w*|Copy|Move|Replace|Delete|Open(?!Read))\s*\(|new\s+(StreamWriter|FileStream)\s*\(|\bDirectory\.(CreateDirectory|Move|Delete)\s*\(|\.SaveAs\s*\(|\.Save\s*\(\s*\)|SharedParametersFilename\s*=(?!=)/;
// A path derived from where this code was loaded: the add-in folder, which is the ACC menu for colleagues.
const OWN_FOLDER =
  /\.Assembly\.Location|GetExecutingAssembly\(\)\.Location|\.CodeBase\b|AppDomain\.CurrentDomain\.BaseDirectory|\bPathManager\./;

// Every C# file allowed to write, and why. Each writes only a path CemTempFolder gave it, or the user's own file.
const WRITERS: Record<string, string> = {
  "commandset/Services/AnnotationComponents/TakeScreenshotEventHandler.cs": "take_screenshot's PNG copy, Screenshots kind",
  "commandset/Services/Core/CoreParamQueryEventHandlers.cs": "create_project_parameter's one-call shared parameter file",
  "commandset/Services/Core/CreateSharedParameterEventHandler.cs": "the fallback shared parameter file Revit keeps for the session",
  "commandset/Services/Family/FamilySessionEventHandlers.cs": "family_save_session: in place on the user's own path, else a copy in Families",
};

describe("0031: no write under ACC", () => {
  it("no source or project file holds an ACC path", () => {
    const accPath = /ACCDocs[\\/]|05-CEMAIModeler|C:\\{1,2}DC\\{1,2}/i;
    const offenders = [...csharp, ...typescript, ...projects].filter((f) => accPath.test(code(readFileSync(f, "utf8"))));
    expect(offenders.map(rel)).toEqual([]);
  });

  it("nothing writes to the raw temp folder, the Desktop or Documents, which nothing prunes", () => {
    const unpruned = /\bGetTemp(Path|FileName)\s*\(|SpecialFolder\.(Desktop|DesktopDirectory|MyDocuments|Personal)\b/;
    const offenders = csharp.filter((f) => unpruned.test(code(readFileSync(f, "utf8"))));
    expect(offenders.map(rel)).toEqual([]);
  });

  it("the Node server writes no file", () => {
    const fsWrite =
      /\b(writeFile|writeFileSync|appendFile|appendFileSync|createWriteStream|mkdir|mkdirSync|mkdtemp|rm|rmSync|unlink|unlinkSync|rename|renameSync|copyFile|copyFileSync|cp|cpSync)\s*\(/;
    const offenders = typescript.filter((f) => fsWrite.test(code(readFileSync(f, "utf8"))));
    expect(offenders.map(rel)).toEqual([]);
  });
});

describe("0031: no write beside the DLL", () => {
  it("no file that derives a path from the add-in folder writes a file", () => {
    const offenders = csharp.filter((f) => {
      const text = code(readFileSync(f, "utf8"));
      return OWN_FOLDER.test(text) && WRITE_API.test(text);
    });
    expect(offenders.map(rel)).toEqual([]);
  });

  it("PathManager only computes paths: no folder created, no default registry written", () => {
    const pathManager = code(read("plugin/Utils/PathManager.cs"));
    expect(pathManager).not.toMatch(WRITE_API);
    expect(pathManager).not.toMatch(/GetLogsDirectoryPath|createIfNotExists/);
  });

  it("a missing registry loads an empty configuration instead of being created", () => {
    expect(code(read("plugin/Configuration/ConfigurationManager.cs"))).toMatch(/Config\s*=\s*new FrameworkConfig\(\)/);
  });

  it("the command-set settings page is read-only (it used to rewrite the shipped registry, empty, on load)", () => {
    const page = code(read("plugin/UI/CommandSetSettingsPage.xaml.cs"));
    expect(page).not.toMatch(WRITE_API);
    expect(page).not.toMatch(/JsonConvert\.Serialize/);
  });
});

describe("0031: every write goes through CemTempFolder", () => {
  it("only the inventoried files write, and each takes its path from CemTempFolder", () => {
    const writers = csharp.filter((f) => WRITE_API.test(code(readFileSync(f, "utf8")))).map(rel).sort();
    expect(writers).toEqual(Object.keys(WRITERS).sort());
    for (const file of writers) expect(code(read(file)), file).toMatch(/CemTempFolder\.(NewArtifactFile|ArtifactFolder)\(\s*McpLocalFiles\.App,/);
  });

  it("both projects name the same app folder, RevitMCP", () => {
    for (const file of ["plugin/Utils/McpLocalFiles.cs", "commandset/Utils/McpLocalFiles.cs"])
      expect(code(read(file)), file).toMatch(/const string App = "RevitMCP";/);
  });

  it("the action log is today's actions-yyyyMMdd.jsonl, in the same {ts, tool, input, ok, result} lines", () => {
    const logger = code(read("plugin/Utils/ActionLogger.cs"));
    expect(logger).toMatch(/CemTempFolder\.AppendLog\(\s*McpLocalFiles\.App,\s*"actions",[\s\S]*?"\.jsonl"\s*\)/);
    for (const key of ["ts", "tool", "input", "ok", "result"]) expect(logger).toContain(`["${key}"]`);
  });

  it("the plugin log is today's mcp-yyyyMMdd.log", () => {
    expect(code(read("plugin/Utils/Logger.cs"))).toMatch(/CemTempFolder\.AppendLog\(\s*McpLocalFiles\.App,\s*"mcp",/);
  });

  it("take_screenshot saves its copy in the Screenshots kind", () => {
    expect(code(read("commandset/Services/AnnotationComponents/TakeScreenshotEventHandler.cs")))
      .toMatch(/CemTempFolder\.NewArtifactFile\(\s*McpLocalFiles\.App,\s*"Screenshots",\s*"screenshot",\s*"\.png"\s*\)/);
  });

  it("create_project_parameter restores the user's shared parameter file in finally", () => {
    expect(code(read("commandset/Services/Core/CoreParamQueryEventHandlers.cs")))
      .toMatch(/finally\s*\{\s*app\.SharedParametersFilename\s*=\s*original/);
  });

  it("create_shared_parameter never adds definitions to a team file on ACC: it works on a copy and restores it", () => {
    const shared = code(read("commandset/Services/Core/CreateSharedParameterEventHandler.cs"));
    expect(shared).toMatch(/McpLocalFiles\.IsUnderAcc\(current\)\)\s*\{\s*string copy = CemTempFolder\.NewArtifactFile\(/);
    expect(shared).toMatch(/finally\s*\{\s*if \(accFile != null\)\s*\{\s*try \{ app\.SharedParametersFilename = accFile;/);
  });

  it("family_save_session never saves in place under ACC: the copy goes to Families", () => {
    const session = code(read("commandset/Services/Family/FamilySessionEventHandlers.cs"));
    expect(session).toMatch(/!McpLocalFiles\.IsUnderAcc\(savedPath\)\)\s*\{?\s*famDoc\.Save\(\)/);
    expect(session).toMatch(/CemTempFolder\.ArtifactFolder\(\s*McpLocalFiles\.App,\s*"Families"\s*\)/);
  });
});

describe("0031: the helper ships with the add-in", () => {
  it("the plugin and the command set reference CEM_CSharpExtended", () => {
    for (const project of ["plugin/CEM_IAModeler.csproj", "commandset/CEM_IAModeler_CommandSet.csproj"]) {
      const match = read(project).match(/<ProjectReference Include="([^"]*\\CEM_CSharpExtended\\CEM_CSharpExtended\.csproj)"/);
      expect(match, project).not.toBeNull();
      expect(existsSync(resolve(repo, dirname(project), match![1].split("\\").join("/"))), project).toBe(true);
    }
  });
});
