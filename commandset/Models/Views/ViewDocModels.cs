using Newtonsoft.Json;
using CEM_IAModeler_CommandSet.Models.Common;

namespace CEM_IAModeler_CommandSet.Models.Views;

// ─────────────────────────────────────────────────────────────────────────────
//  create_schedule  (ViewSchedule.CreateSchedule + ScheduleDefinition/Field)
// ─────────────────────────────────────────────────────────────────────────────
public class ScheduleCreationRequest
{
    /// <summary>BuiltInCategory name to schedule, e.g. "OST_Walls", "OST_Doors", "OST_Rooms".</summary>
    [JsonProperty("category")] public string Category { get; set; } = "";

    /// <summary>Optional schedule name. Empty = Revit default.</summary>
    [JsonProperty("name")] public string Name { get; set; } = "";

    /// <summary>
    ///     Field (parameter) names to add as columns, in order. Accepts BuiltInParameter names or
    ///     display names. Empty = add a sensible default set when available.
    /// </summary>
    [JsonProperty("fields")] public List<string> Fields { get; set; } = new();

    /// <summary>Optional field name to sort/group by.</summary>
    [JsonProperty("sortBy")] public string SortBy { get; set; } = "";

    /// <summary>Sort order when sortBy is set: "Ascending" | "Descending".</summary>
    [JsonProperty("sortOrder")] public string SortOrder { get; set; } = "Ascending";

    /// <summary>When true, create an itemized schedule (one row per element). Default true.</summary>
    [JsonProperty("itemized")] public bool Itemized { get; set; } = true;
}

public class ScheduleCreationResult
{
    [JsonProperty("scheduleId")] public long ScheduleId { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("fields")] public List<string> Fields { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  duplicate_view  (View.Duplicate)
// ─────────────────────────────────────────────────────────────────────────────
public class DuplicateViewRequest
{
    /// <summary>Source view id or name. -1/empty = active view.</summary>
    [JsonProperty("viewId")] public string ViewId { get; set; } = "";

    /// <summary>
    ///     Duplicate option: "Duplicate" (geometry only) | "WithDetailing" | "AsDependent".
    /// </summary>
    [JsonProperty("option")] public string Option { get; set; } = "Duplicate";

    /// <summary>Optional new name for the duplicated view.</summary>
    [JsonProperty("newName")] public string NewName { get; set; } = "";
}

public class DuplicateViewResult
{
    [JsonProperty("newViewId")] public long NewViewId { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  set_view_properties  (template / scale / crop / detail level / discipline)
// ─────────────────────────────────────────────────────────────────────────────
public class SetViewPropertiesRequest
{
    /// <summary>View id or name. -1/empty = active view.</summary>
    [JsonProperty("viewId")] public string ViewId { get; set; } = "";

    /// <summary>View template name or id to apply. Optional.</summary>
    [JsonProperty("templateName")] public string TemplateName { get; set; } = "";

    /// <summary>Scale denominator (e.g. 100 for 1:100). Optional.</summary>
    [JsonProperty("scale")] public int? Scale { get; set; }

    /// <summary>Toggle crop box on/off. Optional.</summary>
    [JsonProperty("cropVisible")] public bool? CropVisible { get; set; }

    /// <summary>Toggle crop region active. Optional.</summary>
    [JsonProperty("cropActive")] public bool? CropActive { get; set; }

    /// <summary>Detail level: "Coarse" | "Medium" | "Fine". Optional.</summary>
    [JsonProperty("detailLevel")] public string DetailLevel { get; set; } = "";

    /// <summary>Discipline: "Architectural" | "Structural" | "Mechanical" | "Electrical" | "Plumbing" | "Coordination". Optional.</summary>
    [JsonProperty("discipline")] public string Discipline { get; set; } = "";
}

public class SetViewPropertiesResult
{
    [JsonProperty("viewId")] public long ViewId { get; set; }
    [JsonProperty("applied")] public List<string> Applied { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  apply_filter_to_view  (View.AddFilter + OverrideGraphicSettings)
// ─────────────────────────────────────────────────────────────────────────────
public class ApplyFilterToViewRequest
{
    /// <summary>View id or name. -1/empty = active view.</summary>
    [JsonProperty("viewId")] public string ViewId { get; set; } = "";

    /// <summary>Existing ParameterFilterElement name or id to apply.</summary>
    [JsonProperty("filterName")] public string FilterName { get; set; } = "";

    /// <summary>Whether elements matching the filter are visible. Default true.</summary>
    [JsonProperty("visible")] public bool Visible { get; set; } = true;

    /// <summary>Optional projection/cut line + surface color as [r,g,b] 0-255.</summary>
    [JsonProperty("colorRgb")] public List<int> ColorRgb { get; set; }

    /// <summary>Optional projection line weight (1-16).</summary>
    [JsonProperty("lineWeight")] public int? LineWeight { get; set; }

    /// <summary>Optional surface transparency 0-100.</summary>
    [JsonProperty("transparency")] public int? Transparency { get; set; }
}

public class ApplyFilterToViewResult
{
    [JsonProperty("viewId")] public long ViewId { get; set; }
    [JsonProperty("filterId")] public long FilterId { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  place_text  (TextNote.Create)
// ─────────────────────────────────────────────────────────────────────────────
public class PlaceTextRequest
{
    /// <summary>View id or name to place the note in. -1/empty = active view.</summary>
    [JsonProperty("viewId")] public string ViewId { get; set; } = "";

    /// <summary>Text content.</summary>
    [JsonProperty("text")] public string Text { get; set; } = "";

    /// <summary>Placement point in the view, mm.</summary>
    [JsonProperty("position")] public JZPoint Position { get; set; }

    /// <summary>Optional TextNoteType name or id. Empty = default.</summary>
    [JsonProperty("textTypeName")] public string TextTypeName { get; set; } = "";

    /// <summary>Optional text width in mm (wraps). 0 = unwrapped.</summary>
    [JsonProperty("width")] public double Width { get; set; } = 0;
}

public class PlaceTextResult
{
    [JsonProperty("textNoteId")] public long TextNoteId { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  create_filled_region  (FilledRegion.Create)
// ─────────────────────────────────────────────────────────────────────────────
public class FilledRegionRequest
{
    /// <summary>View id or name to draw in (must be a view that supports detail items). -1/empty = active.</summary>
    [JsonProperty("viewId")] public string ViewId { get; set; } = "";

    /// <summary>Boundary points (mm), closed automatically. >= 3 required.</summary>
    [JsonProperty("boundary")] public List<JZPoint> Boundary { get; set; } = new();

    /// <summary>Optional FilledRegionType name or id. Empty = first available.</summary>
    [JsonProperty("filledRegionTypeName")] public string FilledRegionTypeName { get; set; } = "";
}

public class FilledRegionResult
{
    [JsonProperty("filledRegionId")] public long FilledRegionId { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  create_revision (+ optional cloud)  (Revision / RevisionCloud)
// ─────────────────────────────────────────────────────────────────────────────
public class RevisionRequest
{
    /// <summary>Revision description.</summary>
    [JsonProperty("description")] public string Description { get; set; } = "";

    /// <summary>Optional issued-to / issued-by.</summary>
    [JsonProperty("issuedTo")] public string IssuedTo { get; set; } = "";
    [JsonProperty("issuedBy")] public string IssuedBy { get; set; } = "";

    /// <summary>Mark as issued. Default false.</summary>
    [JsonProperty("issued")] public bool Issued { get; set; } = false;

    /// <summary>Optional view id/name to draw a revision cloud in. Empty = no cloud.</summary>
    [JsonProperty("cloudViewId")] public string CloudViewId { get; set; } = "";

    /// <summary>Cloud boundary points (mm) when cloudViewId is set. >= 3 required for a cloud.</summary>
    [JsonProperty("cloudBoundary")] public List<JZPoint> CloudBoundary { get; set; } = new();
}

public class RevisionResult
{
    [JsonProperty("revisionId")] public long RevisionId { get; set; }
    [JsonProperty("cloudId")] public long CloudId { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  create_legend  (duplicate an existing legend view; optionally place on sheet)
// ─────────────────────────────────────────────────────────────────────────────
public class CreateLegendRequest
{
    /// <summary>New legend view name.</summary>
    [JsonProperty("name")] public string Name { get; set; } = "";

    /// <summary>Scale denominator. Optional.</summary>
    [JsonProperty("scale")] public int? Scale { get; set; }

    /// <summary>Optional sheet id to place the new legend on as a viewport.</summary>
    [JsonProperty("sheetId")] public long SheetId { get; set; } = 0;
}

public class CreateLegendResult
{
    [JsonProperty("legendViewId")] public long LegendViewId { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("viewportId")] public long ViewportId { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}
