using Newtonsoft.Json;

namespace CEM_IAModeler_CommandSet.Models.Common;

// ─────────────────────────────────────────────────────────────────────────────
//  create_model_lines  (Document.Create.NewModelCurve + SketchPlane)
// ─────────────────────────────────────────────────────────────────────────────
public class CreateModelLinesRequest
{
    /// <summary>Polyline vertices (mm). Each consecutive pair becomes a model line.</summary>
    [JsonProperty("points")] public List<JZPoint> Points { get; set; } = new();

    /// <summary>Close the loop (connect last point back to first). Default false.</summary>
    [JsonProperty("closed")] public bool Closed { get; set; } = false;

    /// <summary>
    ///     Work plane: "XY" (default, z from first point), "XZ", "YZ". Model lines need a sketch plane;
    ///     a plane through the first point with this normal is created.
    /// </summary>
    [JsonProperty("plane")] public string Plane { get; set; } = "XY";
}

public class CreatedIdsResult
{
    [JsonProperty("createdIds")] public List<long> CreatedIds { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  create_detail_lines  (Document.Create.NewDetailCurve in a view)
// ─────────────────────────────────────────────────────────────────────────────
public class CreateDetailLinesRequest
{
    /// <summary>View id or name to draw in (-1/empty = active view).</summary>
    [JsonProperty("viewId")] public string ViewId { get; set; } = "";

    /// <summary>Polyline vertices (mm) in the view plane.</summary>
    [JsonProperty("points")] public List<JZPoint> Points { get; set; } = new();

    [JsonProperty("closed")] public bool Closed { get; set; } = false;

    /// <summary>Optional line style (GraphicsStyle) name to assign.</summary>
    [JsonProperty("lineStyleName")] public string LineStyleName { get; set; } = "";
}

// ─────────────────────────────────────────────────────────────────────────────
//  create_direct_shape  (DirectShape.CreateElement + SetShape)
// ─────────────────────────────────────────────────────────────────────────────
public class CreateDirectShapeRequest
{
    /// <summary>BuiltInCategory for the DirectShape, e.g. "OST_GenericModel", "OST_Walls".</summary>
    [JsonProperty("category")] public string Category { get; set; } = "OST_GenericModel";

    /// <summary>
    ///     Geometry kind: "box" (min/max corners) or "extrusion" (profile + height).
    /// </summary>
    [JsonProperty("shape")] public string Shape { get; set; } = "box";

    /// <summary>For box: opposite corners (mm).</summary>
    [JsonProperty("min")] public JZPoint Min { get; set; }
    [JsonProperty("max")] public JZPoint Max { get; set; }

    /// <summary>For extrusion: base profile vertices (mm, >=3, closed automatically).</summary>
    [JsonProperty("profile")] public List<JZPoint> Profile { get; set; } = new();

    /// <summary>For extrusion: extrusion height in mm (along +Z).</summary>
    [JsonProperty("height")] public double Height { get; set; } = 1000;

    /// <summary>Optional name for the DirectShape.</summary>
    [JsonProperty("name")] public string Name { get; set; } = "";
}

public class CreateDirectShapeResult
{
    [JsonProperty("elementId")] public long ElementId { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  create_spot_dimension  (NewSpotElevation / NewSpotCoordinate)
// ─────────────────────────────────────────────────────────────────────────────
public class CreateSpotDimensionRequest
{
    /// <summary>View id or name (-1/empty = active view).</summary>
    [JsonProperty("viewId")] public string ViewId { get; set; } = "";

    /// <summary>Element id to dimension (a reference is taken from its geometry).</summary>
    [JsonProperty("elementId")] public long ElementId { get; set; }

    /// <summary>Point on the element to dimension (mm).</summary>
    [JsonProperty("point")] public JZPoint Point { get; set; }

    /// <summary>Leader bend point (mm). Optional; offset from point when omitted.</summary>
    [JsonProperty("bend")] public JZPoint Bend { get; set; }

    /// <summary>Text/end point (mm). Optional.</summary>
    [JsonProperty("end")] public JZPoint End { get; set; }

    /// <summary>"elevation" (default) or "coordinate".</summary>
    [JsonProperty("kind")] public string Kind { get; set; } = "elevation";

    [JsonProperty("hasLeader")] public bool HasLeader { get; set; } = true;
}

public class CreateSpotDimensionResult
{
    [JsonProperty("spotDimensionId")] public long SpotDimensionId { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  create_assembly  (AssemblyInstance.Create)
// ─────────────────────────────────────────────────────────────────────────────
public class CreateAssemblyRequest
{
    /// <summary>Member element ids to include in the assembly (>=1).</summary>
    [JsonProperty("elementIds")] public List<long> ElementIds { get; set; } = new();

    /// <summary>Optional assembly type name (the naming category is taken from the first element).</summary>
    [JsonProperty("name")] public string Name { get; set; } = "";
}

public class CreateAssemblyResult
{
    [JsonProperty("assemblyId")] public long AssemblyId { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("memberCount")] public int MemberCount { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// (create_scope_box dropped — no public Revit API; see CEM_RevitMCP.md §6.)

// ─────────────────────────────────────────────────────────────────────────────
//  create_multi_segment_grid  (MultiSegmentGrid.Create)
// ─────────────────────────────────────────────────────────────────────────────
public class CreateMultiSegmentGridRequest
{
    /// <summary>Polyline vertices (mm) forming the grid chain (>=2 points).</summary>
    [JsonProperty("points")] public List<JZPoint> Points { get; set; } = new();

    /// <summary>Optional grid type name or id.</summary>
    [JsonProperty("gridTypeName")] public string GridTypeName { get; set; } = "";
}

public class CreateMultiSegmentGridResult
{
    [JsonProperty("gridId")] public long GridId { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}
