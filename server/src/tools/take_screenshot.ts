import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

interface ScreenshotResponse {
  success: boolean;
  imageBase64?: string;
  mimeType?: string;
  width?: number;
  height?: number;
  savedPath?: string;
  errorMessage?: string;
}

export function registerTakeScreenshotTool(server: McpServer) {
  server.tool(
    "take_screenshot",
    "Capture a live screenshot of the current Revit window (ribbon, drawing area, selection highlights and all) and return it as an image. Use this to visually verify the result of a previous operation — to confirm it produced the expected outcome, or to see what to improve. Pairs well with send_code_to_revit: act, then screenshot to evaluate.",
    {
      saveToDisk: z
        .boolean()
        .optional()
        .default(true)
        .describe(
          "Also save a timestamped PNG copy on the Revit machine as an audit/debug record. The image is always returned inline regardless of this flag."
        ),
    },
    async (args, extra) => {
      try {
        const response = (await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("take_screenshot", {
            saveToDisk: args.saveToDisk,
          });
        })) as ScreenshotResponse;

        if (!response || !response.success || !response.imageBase64) {
          const reason =
            response?.errorMessage || "Revit returned no image data.";
          return {
            content: [
              {
                type: "text",
                text: `Screenshot failed: ${reason}`,
              },
            ],
          };
        }

        const savedNote = response.savedPath
          ? `\nSaved copy: ${response.savedPath}`
          : "";

        return {
          content: [
            {
              type: "text",
              text: `Revit screenshot captured (${response.width ?? "?"}x${
                response.height ?? "?"
              }).${savedNote}`,
            },
            {
              type: "image",
              data: response.imageBase64,
              mimeType: response.mimeType || "image/png",
            },
          ],
        };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `Screenshot failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
