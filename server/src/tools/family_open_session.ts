import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerFamilyOpenSessionTool(server: McpServer) {
  server.tool(
    "family_open_session",
    "Open a family for editing — the entry point for authoring family geometry. Give familyName to EditFamily a family loaded in a project (the host defaults to the active document if it is a project, else the open project, else the active document; the result's hostTitle names it) (or, with hostDocument, in another open document such as a container family, for a nested family), or familyPath to open a standalone .rfa from disk; omit both to use the already-active family document. The family becomes Revit's active document, which family_create_extrusion and the other family editor tools act on. Revit can only activate a document that has a file, so a family opened by familyName (it has none) is first saved to a local temp copy, in a new folder, %TEMP%\\Cemengal\\RevitMCP\\Families\\<name>_<stamp>\\<name>.rfa (never overwritten), and activated there; the result's warnings name the copy. Finish with family_save_session.",
    {
      familyName: z.string().optional().describe("Name of a family loaded in the project (opened with EditFamily)"),
      familyPath: z.string().optional().describe("Absolute path to an .rfa file (opened from disk). Used when familyName is omitted"),
      hostDocument: z
        .string()
        .optional()
        .describe("With familyName: the title of the open document the family is loaded in (a family, to edit a nested one). Default: the active document if it is a project, else the open project, else the active document. With two projects open, make the right one active or name it"),
    },
    async (args) => {
      const params = {
        familyName: args.familyName ?? "",
        familyPath: args.familyPath ?? "",
        hostDocument: args.hostDocument ?? "",
      };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("family_open_session", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `family_open_session failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
