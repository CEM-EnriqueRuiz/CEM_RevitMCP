using Newtonsoft.Json;
using RevitMCPCommandSet.Models.Common;

namespace RevitMCPCommandSet.Models.MEP;

/// <summary>
///     One straight MEP run (duct / pipe / conduit / cable tray). Endpoints in mm.
/// </summary>
public class MepCurveSegment
{
    /// <summary>Start point in mm.</summary>
    [JsonProperty("start")]
    public JZPoint Start { get; set; }

    /// <summary>End point in mm.</summary>
    [JsonProperty("end")]
    public JZPoint End { get; set; }

    /// <summary>Diameter in mm (round duct/pipe/conduit). Optional.</summary>
    [JsonProperty("diameter")]
    public double Diameter { get; set; }

    /// <summary>Width in mm (rectangular duct / cable tray). Optional.</summary>
    [JsonProperty("width")]
    public double Width { get; set; }

    /// <summary>Height in mm (rectangular duct / cable tray). Optional.</summary>
    [JsonProperty("height")]
    public double Height { get; set; }
}

/// <summary>Request to create one or more MEP runs of a single kind.</summary>
public class MepCurveRequest
{
    /// <summary>Runs to create.</summary>
    [JsonProperty("segments")]
    public List<MepCurveSegment> Segments { get; set; } = new();

    /// <summary>Curve type name/id (DuctType, PipeType, ConduitType, CableTrayType). Optional.</summary>
    [JsonProperty("typeName")]
    public string TypeName { get; set; } = "";

    /// <summary>System type name/id (Mechanical/Piping). Optional — duct/pipe only.</summary>
    [JsonProperty("systemTypeName")]
    public string SystemTypeName { get; set; } = "";

    /// <summary>Level name/id. Optional — defaults to lowest level.</summary>
    [JsonProperty("level")]
    public string Level { get; set; } = "";
}

public class MepCreateResult
{
    [JsonProperty("createdIds")] public List<long> CreatedIds { get; set; } = new();
    [JsonProperty("createdCount")] public int CreatedCount { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

/// <summary>Place a MEP family instance (equipment/fixture/device) at points on a level.</summary>
public class MepEquipmentRequest
{
    /// <summary>Family type name/id to place.</summary>
    [JsonProperty("typeName")]
    public string TypeName { get; set; } = "";

    /// <summary>Placement points in mm.</summary>
    [JsonProperty("points")]
    public List<JZPoint> Points { get; set; } = new();

    /// <summary>Level name/id. Optional — defaults to lowest.</summary>
    [JsonProperty("level")]
    public string Level { get; set; } = "";

    /// <summary>Rotation about Z in degrees. Optional.</summary>
    [JsonProperty("rotation")]
    public double Rotation { get; set; }
}

/// <summary>Create an MEP space (and optionally tag it) in a level/phase.</summary>
public class MepSpaceRequest
{
    /// <summary>Points (mm) at which to place spaces. Empty = auto-place in all enclosed regions.</summary>
    [JsonProperty("points")]
    public List<JZPoint> Points { get; set; } = new();

    /// <summary>Level name/id. Optional — defaults to lowest.</summary>
    [JsonProperty("level")]
    public string Level { get; set; } = "";

    /// <summary>Tag each created space.</summary>
    [JsonProperty("tag")]
    public bool Tag { get; set; } = false;
}

public class MepInfoResult
{
    [JsonProperty("createdIds")] public List<long> CreatedIds { get; set; } = new();
    [JsonProperty("count")] public int Count { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

/// <summary>Read an MEP system's topology (elements, flow) for review.</summary>
public class MepSystemQuery
{
    /// <summary>An element id that belongs to the system, or a system element id.</summary>
    [JsonProperty("elementId")]
    public long ElementId { get; set; }
}

public class MepSystemInfo
{
    [JsonProperty("systemId")] public long SystemId { get; set; }
    [JsonProperty("systemName")] public string SystemName { get; set; }
    [JsonProperty("systemType")] public string SystemType { get; set; }
    [JsonProperty("elementCount")] public int ElementCount { get; set; }
    [JsonProperty("elementIds")] public List<long> ElementIds { get; set; } = new();
    [JsonProperty("flow")] public string Flow { get; set; }
}
