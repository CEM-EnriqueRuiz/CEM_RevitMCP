using Newtonsoft.Json;
using CEM_IAModeler_CommandSet.Models.Common;

namespace CEM_IAModeler_CommandSet.Models.Architecture;

/// <summary>Footprint roof from a closed boundary (mm).</summary>
public class CreateRoofRequest
{
    [JsonProperty("boundary")] public List<JZPoint> Boundary { get; set; } = new();
    [JsonProperty("typeName")] public string TypeName { get; set; } = "";
    [JsonProperty("level")] public string Level { get; set; } = "";
}

/// <summary>Curtain wall along a line (mm). Uses a curtain wall type.</summary>
public class CreateCurtainWallRequest
{
    [JsonProperty("start")] public JZPoint Start { get; set; }
    [JsonProperty("end")] public JZPoint End { get; set; }
    [JsonProperty("height")] public double Height { get; set; } = 3000;
    [JsonProperty("typeName")] public string TypeName { get; set; } = "";
    [JsonProperty("level")] public string Level { get; set; } = "";
}

/// <summary>Create an area plan for a scheme + level.</summary>
public class CreateAreaPlanRequest
{
    [JsonProperty("level")] public string Level { get; set; } = "";
    [JsonProperty("schemeName")] public string SchemeName { get; set; } = "";
    [JsonProperty("name")] public string Name { get; set; } = "";
}

/// <summary>Place areas at points (mm) in an area plan view.</summary>
public class CreateAreaRequest
{
    [JsonProperty("areaViewId")] public long AreaViewId { get; set; }
    [JsonProperty("points")] public List<JZPoint> Points { get; set; } = new();
}

/// <summary>Create room/area/space separation lines from a polyline (mm) in a view.</summary>
public class CreateSeparatorRequest
{
    /// <summary>"room" | "area" | "space".</summary>
    [JsonProperty("kind")] public string Kind { get; set; } = "room";
    [JsonProperty("points")] public List<JZPoint> Points { get; set; } = new();
    [JsonProperty("viewId")] public long ViewId { get; set; } = -1;
    /// <summary>For area separators, the area view id.</summary>
    [JsonProperty("closed")] public bool Closed { get; set; } = true;
}

/// <summary>Join or unjoin geometry between two elements.</summary>
public class JoinRequest
{
    [JsonProperty("firstId")] public long FirstId { get; set; }
    [JsonProperty("secondId")] public long SecondId { get; set; }
}

/// <summary>Attach wall tops/bases to a target element (roof/floor/ceiling).</summary>
public class AttachWallRequest
{
    [JsonProperty("wallIds")] public List<long> WallIds { get; set; } = new();
    [JsonProperty("targetId")] public long TargetId { get; set; }
    /// <summary>"top" | "base".</summary>
    [JsonProperty("end")] public string End { get; set; } = "top";
}

/// <summary>Straight-run stair between two levels at a point (mm).</summary>
public class CreateStairsRequest
{
    [JsonProperty("baseLevel")] public string BaseLevel { get; set; } = "";
    [JsonProperty("topLevel")] public string TopLevel { get; set; } = "";
    /// <summary>Run start point (mm) — bottom of the run.</summary>
    [JsonProperty("runStart")] public JZPoint RunStart { get; set; }
    /// <summary>Run direction end point (mm) — defines run direction & length.</summary>
    [JsonProperty("runEnd")] public JZPoint RunEnd { get; set; }
    [JsonProperty("width")] public double Width { get; set; } = 1000;
    [JsonProperty("typeName")] public string TypeName { get; set; } = "";
}

/// <summary>Create railings on an existing stairs/ramp.</summary>
public class CreateRailingRequest
{
    [JsonProperty("hostId")] public long HostId { get; set; }
    [JsonProperty("typeName")] public string TypeName { get; set; } = "";
    /// <summary>"Treads" | "Stringer" (RailingPlacementPosition).</summary>
    [JsonProperty("position")] public string Position { get; set; } = "Treads";
}
