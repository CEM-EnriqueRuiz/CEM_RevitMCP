using Newtonsoft.Json;

namespace CEM_IAModeler_CommandSet.Models.Common;

// Structural members placed as FamilyInstance with a StructuralType.
// Domain: Autodesk.Revit.DB.Structure (+ DB NewFamilyInstance overloads). Units mm.

// ─────────────────────────────────────────────────────────────────────────────
//  struct_create_beam  (line-based, StructuralType.Beam)
//  struct_create_brace (line-based, StructuralType.Brace)  — share this request
// ─────────────────────────────────────────────────────────────────────────────
public class StructLineMemberRequest
{
    /// <summary>Member centerline start point (mm).</summary>
    [JsonProperty("start")] public JZPoint Start { get; set; }

    /// <summary>Member centerline end point (mm).</summary>
    [JsonProperty("end")] public JZPoint End { get; set; }

    /// <summary>Family type name or id (a Structural Framing type). Empty = first available.</summary>
    [JsonProperty("typeName")] public string TypeName { get; set; } = "";

    /// <summary>Level name or id the member is hosted on. Empty = lowest level.</summary>
    [JsonProperty("level")] public string Level { get; set; } = "";
}

// ─────────────────────────────────────────────────────────────────────────────
//  struct_create_column  (point + level, StructuralType.Column)
// ─────────────────────────────────────────────────────────────────────────────
public class StructColumnRequest
{
    /// <summary>Column location point (mm). Z is taken from the base level unless set.</summary>
    [JsonProperty("location")] public JZPoint Location { get; set; }

    /// <summary>Family type name or id (a Structural Columns type). Empty = first available.</summary>
    [JsonProperty("typeName")] public string TypeName { get; set; } = "";

    /// <summary>Base level name or id. Empty = lowest level.</summary>
    [JsonProperty("baseLevel")] public string BaseLevel { get; set; } = "";

    /// <summary>Top level name or id. Optional; sets the column's Top Level when found.</summary>
    [JsonProperty("topLevel")] public string TopLevel { get; set; } = "";
}

// ─────────────────────────────────────────────────────────────────────────────
//  struct_create_foundation  (point + level, StructuralType.Footing)
// ─────────────────────────────────────────────────────────────────────────────
public class StructFoundationRequest
{
    /// <summary>Footing location point (mm).</summary>
    [JsonProperty("location")] public JZPoint Location { get; set; }

    /// <summary>Family type name or id (a Structural Foundation / isolated footing type).</summary>
    [JsonProperty("typeName")] public string TypeName { get; set; } = "";

    /// <summary>Level name or id. Empty = lowest level.</summary>
    [JsonProperty("level")] public string Level { get; set; } = "";
}

public class StructMemberResult
{
    [JsonProperty("elementId")] public long ElementId { get; set; }
    [JsonProperty("typeName")] public string TypeName { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}
