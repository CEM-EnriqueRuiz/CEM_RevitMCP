using Newtonsoft.Json;

namespace CEM_IAModeler_CommandSet.Models.Common;

// ─────────────────────────────────────────────────────────────────────────────
//  ParentTools — high-level "parent" operations that collapse the IFC/link →
//  native reconstruction pipeline the AI was hand-writing as send_code_to_revit
//  recipes (read link geometry → measure → rebuild as native → validate).
//  Domain prefix: parent_*. Units mm in / mm out.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>parent_link_extract_geometry — inspect a linked model and (optionally) pull per-element geometry.</summary>
public class LinkExtractRequest
{
    /// <summary>Revit link name or id. Empty = first loaded link.</summary>
    [JsonProperty("linkName")] public string LinkName { get; set; } = "";

    /// <summary>Restrict to these categories (BuiltInCategory names or display names). Empty = all model categories.</summary>
    [JsonProperty("categories")] public List<string> Categories { get; set; } = new();

    /// <summary>When true, also extract per-element geometry summaries (bbox/volume/centerline). Heavier.</summary>
    [JsonProperty("includeGeometry")] public bool IncludeGeometry { get; set; } = false;

    /// <summary>Cap on elements returned in the geometry list (protects payload size). Default 500.</summary>
    [JsonProperty("maxElements")] public int MaxElements { get; set; } = 500;
}

public class LinkElementGeometry
{
    [JsonProperty("linkElementId")] public long LinkElementId { get; set; }
    [JsonProperty("uniqueId")] public string UniqueId { get; set; }
    [JsonProperty("category")] public string Category { get; set; }
    [JsonProperty("typeName")] public string TypeName { get; set; }
    /// <summary>Bounding box min/max in host coordinates (mm), link transform applied.</summary>
    [JsonProperty("bboxMin")] public JZPoint BBoxMin { get; set; }
    [JsonProperty("bboxMax")] public JZPoint BBoxMax { get; set; }
    [JsonProperty("volumeMm3")] public double VolumeMm3 { get; set; }
}

public class LinkExtractResult
{
    [JsonProperty("linkName")] public string LinkName { get; set; }
    [JsonProperty("linkTitle")] public string LinkTitle { get; set; }
    [JsonProperty("totalElements")] public int TotalElements { get; set; }
    [JsonProperty("countsByCategory")] public Dictionary<string, int> CountsByCategory { get; set; } = new();
    [JsonProperty("geometry")] public List<LinkElementGeometry> Geometry { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>parent_solid_to_member_params — derive linear-member parameters (centerline, section, height) from link geometry.</summary>
public class MemberParamsRequest
{
    /// <summary>Revit link name or id. Empty = first loaded link.</summary>
    [JsonProperty("linkName")] public string LinkName { get; set; } = "";

    /// <summary>Specific link element ids to measure. If empty, falls back to <see cref="Category"/>.</summary>
    [JsonProperty("linkElementIds")] public List<long> LinkElementIds { get; set; } = new();

    /// <summary>Category to measure when no explicit ids given (e.g. OST_StructuralFraming).</summary>
    [JsonProperty("category")] public string Category { get; set; } = "";

    /// <summary>Round derived section/height to this step in mm (0 = no rounding). Default 5.</summary>
    [JsonProperty("roundMm")] public double RoundMm { get; set; } = 5;

    [JsonProperty("maxElements")] public int MaxElements { get; set; } = 500;
}

public class DerivedMember
{
    [JsonProperty("linkElementId")] public long LinkElementId { get; set; }
    [JsonProperty("uniqueId")] public string UniqueId { get; set; }
    [JsonProperty("category")] public string Category { get; set; }
    /// <summary>Best-fit centerline start/end (mm). Null when the element isn't linear.</summary>
    [JsonProperty("start")] public JZPoint Start { get; set; }
    [JsonProperty("end")] public JZPoint End { get; set; }
    [JsonProperty("lengthMm")] public double LengthMm { get; set; }
    /// <summary>Cross-section breadth and height (mm), the two smaller bbox-aligned dims.</summary>
    [JsonProperty("sectionBMm")] public double SectionBMm { get; set; }
    [JsonProperty("sectionHMm")] public double SectionHMm { get; set; }
    [JsonProperty("nearestLevel")] public string NearestLevel { get; set; }
}

public class MemberParamsResult
{
    [JsonProperty("members")] public List<DerivedMember> Members { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>parent_reconstruct_native — rebuild link elements as native Revit elements (idempotent by source UniqueId).</summary>
public class ReconstructRequest
{
    /// <summary>Revit link name or id. Empty = first loaded link.</summary>
    [JsonProperty("linkName")] public string LinkName { get; set; } = "";

    /// <summary>Source category to reconstruct (e.g. OST_Walls, OST_StructuralFraming, OST_StructuralColumns).</summary>
    [JsonProperty("category")] public string Category { get; set; } = "";

    /// <summary>
    ///     Output mode: "directshape" (robust generic solid copy, default) or "native"
    ///     (typed Wall/Beam/Column — only for supported categories; falls back to directshape).
    /// </summary>
    [JsonProperty("mode")] public string Mode { get; set; } = "directshape";

    /// <summary>Delete prior reconstruction (tagged with the CEMAI app-id) before rebuilding. Default true = idempotent.</summary>
    [JsonProperty("clearPrevious")] public bool ClearPrevious { get; set; } = true;

    [JsonProperty("roundMm")] public double RoundMm { get; set; } = 5;
    [JsonProperty("maxElements")] public int MaxElements { get; set; } = 2000;
}

public class ReconstructResult
{
    [JsonProperty("category")] public string Category { get; set; }
    [JsonProperty("mode")] public string Mode { get; set; }
    [JsonProperty("created")] public int Created { get; set; }
    [JsonProperty("failed")] public int Failed { get; set; }
    [JsonProperty("cleared")] public int Cleared { get; set; }
    [JsonProperty("createdElementIds")] public List<long> CreatedElementIds { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>parent_validate_deviation — measure how far the native rebuild deviates from the source link surfaces (mm).</summary>
public class ValidateDeviationRequest
{
    /// <summary>Revit link name or id holding the source geometry. Empty = first loaded link.</summary>
    [JsonProperty("linkName")] public string LinkName { get; set; } = "";

    /// <summary>App-id tag used by parent_reconstruct_native on the native rebuild. Default "CEMAI".</summary>
    [JsonProperty("appId")] public string AppId { get; set; } = "CEMAI";

    /// <summary>Flag an item as out-of-tolerance when its worst surface distance exceeds this (mm). Default 300.</summary>
    [JsonProperty("toleranceMm")] public double ToleranceMm { get; set; } = 300;

    [JsonProperty("maxElements")] public int MaxElements { get; set; } = 1000;
}

public class DeviationItem
{
    [JsonProperty("nativeElementId")] public long NativeElementId { get; set; }
    [JsonProperty("sourceUniqueId")] public string SourceUniqueId { get; set; }
    [JsonProperty("avgMm")] public double AvgMm { get; set; }
    [JsonProperty("worstMm")] public double WorstMm { get; set; }
    [JsonProperty("withinTolerance")] public bool WithinTolerance { get; set; }
}

public class ValidateDeviationResult
{
    [JsonProperty("itemsChecked")] public int ItemsChecked { get; set; }
    [JsonProperty("overallAvgMm")] public double OverallAvgMm { get; set; }
    [JsonProperty("overallWorstMm")] public double OverallWorstMm { get; set; }
    [JsonProperty("itemsOutOfTolerance")] public int ItemsOutOfTolerance { get; set; }
    [JsonProperty("items")] public List<DeviationItem> Items { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}
