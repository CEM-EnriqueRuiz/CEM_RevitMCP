using Newtonsoft.Json;
using RevitMCPCommandSet.Models.Common;

namespace RevitMCPCommandSet.Models.Architecture;

/// <summary>One straight wall segment. Endpoints in mm.</summary>
public class WallSegment
{
    [JsonProperty("start")] public JZPoint Start { get; set; }
    [JsonProperty("end")] public JZPoint End { get; set; }
    /// <summary>Wall height in mm. Optional — defaults to request height.</summary>
    [JsonProperty("height")] public double Height { get; set; }
}

public class CreateWallRequest
{
    [JsonProperty("segments")] public List<WallSegment> Segments { get; set; } = new();
    [JsonProperty("typeName")] public string TypeName { get; set; } = "";
    [JsonProperty("level")] public string Level { get; set; } = "";
    /// <summary>Default height in mm for segments that don't specify one.</summary>
    [JsonProperty("height")] public double Height { get; set; } = 3000;
    /// <summary>Base offset from the level in mm.</summary>
    [JsonProperty("baseOffset")] public double BaseOffset { get; set; } = 0;
    [JsonProperty("flip")] public bool Flip { get; set; } = false;
    [JsonProperty("structural")] public bool Structural { get; set; } = false;
}

/// <summary>Slab-like creation (floor or ceiling) from a closed boundary of points (mm).</summary>
public class CreateSlabRequest
{
    /// <summary>Closed boundary points in mm (auto-closed).</summary>
    [JsonProperty("boundary")] public List<JZPoint> Boundary { get; set; } = new();
    [JsonProperty("typeName")] public string TypeName { get; set; } = "";
    [JsonProperty("level")] public string Level { get; set; } = "";
    [JsonProperty("structural")] public bool Structural { get; set; } = false;
}

/// <summary>Create rooms at points (mm) or auto-place in all enclosed regions.</summary>
public class CreateRoomRequest
{
    [JsonProperty("points")] public List<JZPoint> Points { get; set; } = new();
    [JsonProperty("level")] public string Level { get; set; } = "";
    [JsonProperty("tag")] public bool Tag { get; set; } = false;
    /// <summary>Optional names to assign, parallel to points.</summary>
    [JsonProperty("names")] public List<string> Names { get; set; } = new();
}

/// <summary>Create an opening in a host element from a closed boundary (mm).</summary>
public class CreateOpeningRequest
{
    [JsonProperty("hostId")] public long HostId { get; set; }
    [JsonProperty("boundary")] public List<JZPoint> Boundary { get; set; } = new();
}

public class ArchCreateResult
{
    [JsonProperty("createdIds")] public List<long> CreatedIds { get; set; } = new();
    [JsonProperty("createdCount")] public int CreatedCount { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}
