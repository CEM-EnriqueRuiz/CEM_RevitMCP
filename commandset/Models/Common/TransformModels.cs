using Newtonsoft.Json;

namespace CEM_IAModeler_CommandSet.Models.Common;

/// <summary>
///     One generic transform operation over a batch of elements. Operations:
///     move, rotate, mirror, copy, array. All distances mm, angles degrees.
/// </summary>
public class TransformRequest
{
    /// <summary>Elements to transform.</summary>
    [JsonProperty("elementIds")]
    public List<long> ElementIds { get; set; } = new();

    /// <summary>"move" | "rotate" | "mirror" | "copy" | "array".</summary>
    [JsonProperty("operation")]
    public string Operation { get; set; } = "move";

    /// <summary>Translation vector in mm (move, copy, array step).</summary>
    [JsonProperty("translation")]
    public JZPoint Translation { get; set; }

    /// <summary>Rotation axis point in mm (rotate). Defaults to origin.</summary>
    [JsonProperty("axisPoint")]
    public JZPoint AxisPoint { get; set; }

    /// <summary>Rotation axis direction (rotate). Defaults to Z (0,0,1).</summary>
    [JsonProperty("axisDirection")]
    public JZPoint AxisDirection { get; set; }

    /// <summary>Rotation angle in degrees (rotate).</summary>
    [JsonProperty("angle")]
    public double Angle { get; set; }

    /// <summary>Mirror plane origin in mm (mirror).</summary>
    [JsonProperty("mirrorPlaneOrigin")]
    public JZPoint MirrorPlaneOrigin { get; set; }

    /// <summary>Mirror plane normal (mirror). Defaults to X (1,0,0).</summary>
    [JsonProperty("mirrorPlaneNormal")]
    public JZPoint MirrorPlaneNormal { get; set; }

    /// <summary>Keep the originals when mirroring/copying. Default true for copy/mirror.</summary>
    [JsonProperty("copy")]
    public bool Copy { get; set; } = true;

    /// <summary>Number of copies for "array" (excludes original).</summary>
    [JsonProperty("count")]
    public int Count { get; set; } = 1;
}

/// <summary>Result of a transform operation.</summary>
public class TransformResult
{
    [JsonProperty("operation")]
    public string Operation { get; set; }

    [JsonProperty("affectedCount")]
    public int AffectedCount { get; set; }

    [JsonProperty("newElementIds")]
    public List<long> NewElementIds { get; set; } = new();

    [JsonProperty("warnings")]
    public List<string> Warnings { get; set; } = new();
}
