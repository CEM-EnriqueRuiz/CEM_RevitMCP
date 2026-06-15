using Newtonsoft.Json;
using CEM_IAModeler_CommandSet.Models.Common;

namespace CEM_IAModeler_CommandSet.Models.Views;

/// <summary>
///     Generic view creation request. One tool, many view kinds.
/// </summary>
public class ViewCreationRequest
{
    /// <summary>
    ///     "FloorPlan" | "CeilingPlan" | "Section" | "Elevation" | "3D" | "Drafting" | "AreaPlan".
    /// </summary>
    [JsonProperty("viewType")]
    public string ViewType { get; set; } = "FloorPlan";

    /// <summary>Optional view name.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    /// <summary>Level name or id for plan views. Empty = lowest level.</summary>
    [JsonProperty("level")]
    public string Level { get; set; } = "";

    /// <summary>View family type name or id (e.g. a specific floor plan type). Optional.</summary>
    [JsonProperty("viewFamilyTypeName")]
    public string ViewFamilyTypeName { get; set; } = "";

    /// <summary>View scale denominator (e.g. 100 for 1:100). Optional.</summary>
    [JsonProperty("scale")]
    public int? Scale { get; set; }

    /// <summary>View template name or id to apply. Optional.</summary>
    [JsonProperty("templateName")]
    public string TemplateName { get; set; } = "";

    /// <summary>For Section/Elevation: start point of the cut line, mm.</summary>
    [JsonProperty("start")]
    public JZPoint Start { get; set; }

    /// <summary>For Section/Elevation: end point of the cut line, mm.</summary>
    [JsonProperty("end")]
    public JZPoint End { get; set; }

    /// <summary>For Section: view depth in mm. Default 5000.</summary>
    [JsonProperty("depth")]
    public double Depth { get; set; } = 5000;

    /// <summary>For Section: height of the crop in mm. Default 4000.</summary>
    [JsonProperty("height")]
    public double Height { get; set; } = 4000;
}

public class ViewCreationResult
{
    [JsonProperty("viewId")] public long ViewId { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("viewType")] public string ViewType { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

/// <summary>Create a sheet and optionally place views on it.</summary>
public class SheetCreationRequest
{
    /// <summary>Sheet number, e.g. "A-101".</summary>
    [JsonProperty("number")]
    public string Number { get; set; } = "";

    /// <summary>Sheet name/title.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    /// <summary>Title block family type name or id. Empty = first available.</summary>
    [JsonProperty("titleBlockName")]
    public string TitleBlockName { get; set; } = "";

    /// <summary>Optional view ids to place as viewports.</summary>
    [JsonProperty("viewIds")]
    public List<long> ViewIds { get; set; } = new();
}

public class SheetCreationResult
{
    [JsonProperty("sheetId")] public long SheetId { get; set; }
    [JsonProperty("number")] public string Number { get; set; }
    [JsonProperty("placedViewports")] public List<long> PlacedViewports { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

/// <summary>Place one or more views as viewports on an existing sheet.</summary>
public class PlaceViewportRequest
{
    [JsonProperty("sheetId")] public long SheetId { get; set; }
    [JsonProperty("viewIds")] public List<long> ViewIds { get; set; } = new();

    /// <summary>Optional placement center on the sheet, mm (sheet coordinates). Auto-grid when omitted.</summary>
    [JsonProperty("center")] public JZPoint Center { get; set; }
}

public class PlaceViewportResult
{
    [JsonProperty("placedViewports")] public List<long> PlacedViewports { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}
