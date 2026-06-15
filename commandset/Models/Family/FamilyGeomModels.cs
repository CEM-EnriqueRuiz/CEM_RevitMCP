using Newtonsoft.Json;
using CEM_IAModeler_CommandSet.Models.Common;

namespace CEM_IAModeler_CommandSet.Models.Family;

// All of these are FAMILY-EDITOR-ONLY (doc.IsFamilyDocument). Profiles are mm.

// ─────────────────────────────────────────────────────────────────────────────
//  Shared profile/plane DTOs for family geometry (support holes, arcs, sloped planes).
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>One segment of a profile loop: a straight line (start→end) or a 3-point arc.</summary>
public class ProfileSegment
{
    [JsonProperty("start")] public JZPoint Start { get; set; }
    [JsonProperty("end")] public JZPoint End { get; set; }

    /// <summary>When true this segment is an arc through <see cref="Mid"/>. Default false = line.</summary>
    [JsonProperty("isArc")] public bool IsArc { get; set; } = false;

    /// <summary>Arc mid/through point (mm). Required when <see cref="IsArc"/> is true.</summary>
    [JsonProperty("mid")] public JZPoint Mid { get; set; }
}

/// <summary>
///     A closed loop. Provide EITHER <see cref="Points"/> (polyline shorthand, auto-closed straight
///     segments) OR <see cref="Segments"/> (full control incl. arcs). If both, Segments wins.
/// </summary>
public class ProfileLoop
{
    [JsonProperty("points")] public List<JZPoint> Points { get; set; } = new();
    [JsonProperty("segments")] public List<ProfileSegment> Segments { get; set; } = new();
}

/// <summary>A sketch plane defined by a normal and an origin (mm). Used for arbitrary/sloped planes.</summary>
public class PlaneDef
{
    /// <summary>Plane normal vector (need not be unit length). Default +Z.</summary>
    [JsonProperty("normal")] public List<double> Normal { get; set; } = new() { 0, 0, 1 };

    /// <summary>A point on the plane (mm). Default origin.</summary>
    [JsonProperty("origin")] public JZPoint Origin { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
//  family_create_extrusion  (FamilyItemFactory.NewExtrusion)
//  Back-compat: the legacy `profile` (single loop) + `plane` string still work.
//  New: `loops` (outer + holes, lines + arcs), `planeDef` (normal+origin), `material`.
// ─────────────────────────────────────────────────────────────────────────────
public class FamilyExtrusionRequest
{
    /// <summary>LEGACY single closed profile polygon (mm, >=3, auto-closed). Use <see cref="Loops"/> for holes/arcs.</summary>
    [JsonProperty("profile")] public List<JZPoint> Profile { get; set; } = new();

    /// <summary>
    ///     Profile loops: the first is the outer boundary, any additional loops are holes/voids in the
    ///     profile. Each loop may use points (polyline) or segments (lines + arcs). Takes precedence over
    ///     <see cref="Profile"/> when non-empty.
    /// </summary>
    [JsonProperty("loops")] public List<ProfileLoop> Loops { get; set; } = new();

    /// <summary>Extrusion end distance in mm (start is 0). Sign sets direction along the plane normal.</summary>
    [JsonProperty("height")] public double Height { get; set; } = 1000;

    /// <summary>LEGACY work plane: "XY" (default), "XZ", "YZ" — through the first profile point.</summary>
    [JsonProperty("plane")] public string Plane { get; set; } = "XY";

    /// <summary>Arbitrary sketch plane (normal + origin, mm). Takes precedence over <see cref="Plane"/> when set.</summary>
    [JsonProperty("planeDef")] public PlaneDef PlaneDef { get; set; }

    /// <summary>Solid (true, default) or void (false).</summary>
    [JsonProperty("isSolid")] public bool IsSolid { get; set; } = true;

    /// <summary>Optional material name or ElementId (as a string). Resolved in the family doc; warns + skips if not found.</summary>
    [JsonProperty("material")] public string Material { get; set; } = "";
}

// ─────────────────────────────────────────────────────────────────────────────
//  family_create_revolution  (FamilyItemFactory.NewRevolution)
// ─────────────────────────────────────────────────────────────────────────────
public class FamilyRevolutionRequest
{
    [JsonProperty("profile")] public List<JZPoint> Profile { get; set; } = new();

    /// <summary>Revolve axis start/end points (mm). Must lie on the sketch plane.</summary>
    [JsonProperty("axisStart")] public JZPoint AxisStart { get; set; }
    [JsonProperty("axisEnd")] public JZPoint AxisEnd { get; set; }

    /// <summary>Start/end angle in degrees. Default 0..360.</summary>
    [JsonProperty("startAngle")] public double StartAngle { get; set; } = 0;
    [JsonProperty("endAngle")] public double EndAngle { get; set; } = 360;

    [JsonProperty("plane")] public string Plane { get; set; } = "XZ";
    [JsonProperty("isSolid")] public bool IsSolid { get; set; } = true;
}

// ─────────────────────────────────────────────────────────────────────────────
//  family_associate_parameter
//  (FamilyManager.AssociateElementParameterToFamilyParameter)
// ─────────────────────────────────────────────────────────────────────────────
public class FamilyAssociateParameterRequest
{
    /// <summary>Element id (in the family doc) whose instance parameter is driven.</summary>
    [JsonProperty("elementId")] public long ElementId { get; set; }

    /// <summary>The element's parameter name to associate (e.g. "Default Elevation").</summary>
    [JsonProperty("elementParameter")] public string ElementParameter { get; set; } = "";

    /// <summary>The family parameter name that should drive it (created via family_add_parameter).</summary>
    [JsonProperty("familyParameter")] public string FamilyParameter { get; set; } = "";
}

// ─────────────────────────────────────────────────────────────────────────────
//  family_add_reference_plane  (doc.FamilyCreate.NewReferencePlane)
// ─────────────────────────────────────────────────────────────────────────────
public class FamilyAddReferencePlaneRequest
{
    /// <summary>Bubble end (mm).</summary>
    [JsonProperty("bubbleEnd")] public JZPoint BubbleEnd { get; set; }
    /// <summary>Free end (mm).</summary>
    [JsonProperty("freeEnd")] public JZPoint FreeEnd { get; set; }
    /// <summary>Third point giving the cut vector / plane orientation (mm).</summary>
    [JsonProperty("cutVectorPoint")] public JZPoint CutVectorPoint { get; set; }
    /// <summary>Optional name for the reference plane.</summary>
    [JsonProperty("name")] public string Name { get; set; } = "";
}

// ─────────────────────────────────────────────────────────────────────────────
//  family_open_session  — enter a family-edit session (project or standalone .rfa)
// ─────────────────────────────────────────────────────────────────────────────
public class FamilyOpenSessionRequest
{
    /// <summary>Name of a family already loaded in the project → opened with Document.EditFamily.</summary>
    [JsonProperty("familyName")] public string FamilyName { get; set; } = "";

    /// <summary>Absolute path to an .rfa → opened with Application.OpenDocumentFile. Used when familyName is empty.</summary>
    [JsonProperty("familyPath")] public string FamilyPath { get; set; } = "";
}

// ─────────────────────────────────────────────────────────────────────────────
//  family_save_session  — save / close / load the edited family back into the project
// ─────────────────────────────────────────────────────────────────────────────
public class FamilySaveSessionRequest
{
    /// <summary>
    ///     Which open family document to act on, by Title (e.g. "X93-BF03"). Empty = the single open
    ///     family document, or the active document if it is a family document.
    /// </summary>
    [JsonProperty("familyTitle")] public string FamilyTitle { get; set; } = "";

    /// <summary>Save the family before loading/closing. Default true. Uses Save() when it has a path, else SaveAs to %TEMP%.</summary>
    [JsonProperty("save")] public bool Save { get; set; } = true;

    /// <summary>Load the (saved) family back into the active project, overwriting an existing one. Default false.</summary>
    [JsonProperty("loadIntoProject")] public bool LoadIntoProject { get; set; } = false;

    /// <summary>Close the family document after saving/loading. Default false so the AI can keep editing.</summary>
    [JsonProperty("close")] public bool Close { get; set; } = false;
}

public class FamilySessionResult
{
    /// <summary>Title of the family document acted on.</summary>
    [JsonProperty("familyTitle")] public string FamilyTitle { get; set; }

    /// <summary>How it was opened: "editfamily" | "path" | "active".</summary>
    [JsonProperty("opened")] public string Opened { get; set; }

    [JsonProperty("isFamilyDocument")] public bool IsFamilyDocument { get; set; }

    /// <summary>Path the family was saved to (when saved).</summary>
    [JsonProperty("savedPath")] public string SavedPath { get; set; }
    [JsonProperty("saved")] public bool Saved { get; set; }

    /// <summary>Loaded-into-project family id followed by its type ids (when loadIntoProject).</summary>
    [JsonProperty("loadedIds")] public List<long> LoadedIds { get; set; } = new();
    [JsonProperty("loadedIntoProject")] public bool LoadedIntoProject { get; set; }

    [JsonProperty("closed")] public bool Closed { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}
