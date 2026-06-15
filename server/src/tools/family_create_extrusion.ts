import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number().default(0) });

// A profile segment: a straight line (start→end) or a 3-point arc (isArc + mid).
const segment = z.object({
  start: point.describe("Segment start (mm)"),
  end: point.describe("Segment end (mm)"),
  isArc: z.boolean().default(false).describe("True = arc through mid; false = straight line"),
  mid: point.optional().describe("Arc mid/through point (mm), required when isArc is true"),
});

// One closed loop: points (polyline shorthand) OR segments (full control incl. arcs).
const loop = z.object({
  points: z.array(point).optional().describe("Polyline vertices (mm), auto-closed with straight segments"),
  segments: z.array(segment).optional().describe("Explicit segments (lines + arcs); takes precedence over points"),
});

const planeDef = z.object({
  normal: z.array(z.number()).length(3).describe("Plane normal [x,y,z] (need not be unit length)"),
  origin: point.describe("A point on the plane (mm)"),
});

export function registerFamilyCreateExtrusionTool(server: McpServer) {
  server.tool(
    "family_create_extrusion",
    "FAMILY EDITOR ONLY (open one with family_open_session first): create a solid or void extrusion in the active family, extruded by height (mm). Profile may have multiple loops (first = outer boundary, the rest = holes) built from straight lines and/or arcs; the sketch plane can be XY/XZ/YZ or an arbitrary {normal,origin} (use planeDef for sloped panels); an optional material (name or id) is assigned to the form. The backbone of modeling loadable family geometry.",
    {
      // New, preferred: multi-loop profile with holes + arcs.
      loops: z.array(loop).optional().describe("Profile loops: first = outer, rest = holes. Each loop uses points or segments"),
      // Legacy back-compat: single straight-sided loop.
      profile: z.array(point).optional().describe("LEGACY single closed polygon (mm, >=3). Prefer loops for holes/arcs"),
      height: z.number().describe("Extrusion height in mm (signed: direction along the plane normal)"),
      // Plane: string OR object.
      plane: z.enum(["XY", "XZ", "YZ"]).optional().describe("LEGACY sketch-plane normal through the first point. Default XY"),
      planeDef: planeDef.optional().describe("Arbitrary sketch plane (normal + origin, mm). Takes precedence over plane"),
      isSolid: z.boolean().default(true).describe("Solid (true) or void cut (false)"),
      material: z.string().optional().describe("Material name or ElementId (as a string) to assign to the form. Optional"),
    },
    async (args) => {
      const params = {
        loops: args.loops ?? [],
        profile: args.profile ?? [],
        height: args.height,
        plane: args.plane ?? "XY",
        planeDef: args.planeDef ?? null,
        isSolid: args.isSolid,
        material: args.material ?? "",
      };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("family_create_extrusion", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `family_create_extrusion failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
