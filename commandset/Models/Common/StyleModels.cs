using Newtonsoft.Json;

namespace CEM_IAModeler_CommandSet.Models.Common;

// ─────────────────────────────────────────────────────────────────────────────
//  manage_line_pattern  (LinePatternElement)
// ─────────────────────────────────────────────────────────────────────────────
public class LinePatternRequest
{
    /// <summary>"create" | "list". Default "list".</summary>
    [JsonProperty("action")] public string Action { get; set; } = "list";

    /// <summary>Pattern name (required for create).</summary>
    [JsonProperty("name")] public string Name { get; set; } = "";

    /// <summary>
    ///     For create: dash/space segment lengths in mm, alternating dash, space, dash, space…
    ///     e.g. [5, 2, 1, 2] = 5mm dash, 2mm gap, 1mm dash, 2mm gap.
    /// </summary>
    [JsonProperty("segmentsMm")] public List<double> SegmentsMm { get; set; } = new();
}

public class LinePatternResult
{
    [JsonProperty("action")] public string Action { get; set; }
    [JsonProperty("patternId")] public long PatternId { get; set; }
    [JsonProperty("patterns")] public List<NamedIdInfo> Patterns { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  manage_fill_pattern  (FillPatternElement)
// ─────────────────────────────────────────────────────────────────────────────
public class FillPatternRequest
{
    /// <summary>"create" | "list". Default "list".</summary>
    [JsonProperty("action")] public string Action { get; set; } = "list";

    /// <summary>Pattern name (required for create).</summary>
    [JsonProperty("name")] public string Name { get; set; } = "";

    /// <summary>For create: simple parallel-line pattern angle in degrees. Default 45.</summary>
    [JsonProperty("angle")] public double Angle { get; set; } = 45;

    /// <summary>For create: spacing between lines in mm. Default 5.</summary>
    [JsonProperty("spacingMm")] public double SpacingMm { get; set; } = 5;

    /// <summary>For create: target "Drafting" (default) or "Model".</summary>
    [JsonProperty("target")] public string Target { get; set; } = "Drafting";
}

public class FillPatternResult
{
    [JsonProperty("action")] public string Action { get; set; }
    [JsonProperty("patternId")] public long PatternId { get; set; }
    [JsonProperty("patterns")] public List<NamedIdInfo> Patterns { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  set_object_styles  (Category / GraphicsStyle line weight, color, pattern)
// ─────────────────────────────────────────────────────────────────────────────
public class ObjectStylesRequest
{
    /// <summary>BuiltInCategory name or display name, e.g. "OST_Walls" / "Walls".</summary>
    [JsonProperty("category")] public string Category { get; set; } = "";

    /// <summary>Projection line weight 1-16. Optional.</summary>
    [JsonProperty("projectionLineWeight")] public int? ProjectionLineWeight { get; set; }

    /// <summary>Cut line weight 1-16. Optional (ignored for categories that can't be cut).</summary>
    [JsonProperty("cutLineWeight")] public int? CutLineWeight { get; set; }

    /// <summary>Line color as [r,g,b] 0-255. Optional.</summary>
    [JsonProperty("colorRgb")] public List<int> ColorRgb { get; set; }

    /// <summary>Line pattern name or id to assign. Optional.</summary>
    [JsonProperty("linePatternName")] public string LinePatternName { get; set; } = "";
}

public class ObjectStylesResult
{
    [JsonProperty("category")] public string Category { get; set; }
    [JsonProperty("applied")] public List<string> Applied { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

/// <summary>Generic name+id pair for list results.</summary>
public class NamedIdInfo
{
    public NamedIdInfo() { }
    public NamedIdInfo(string name, long id) { Name = name; Id = id; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("id")] public long Id { get; set; }
}
