using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Common;

/// <summary>
///     Generic request to tag elements in a view. Either tag explicit elementIds,
///     or all elements of given categories in the view.
/// </summary>
public class TagElementsRequest
{
    /// <summary>Explicit elements to tag. If empty, uses Categories.</summary>
    [JsonProperty("elementIds")]
    public List<long> ElementIds { get; set; } = new();

    /// <summary>BuiltInCategory names to tag (when elementIds is empty), e.g. "OST_Walls".</summary>
    [JsonProperty("categories")]
    public List<string> Categories { get; set; } = new();

    /// <summary>View to tag in. -1 = active view.</summary>
    [JsonProperty("viewId")]
    public long ViewId { get; set; } = -1;

    /// <summary>Optional tag family type name or id. Defaults to the first loaded tag for the category.</summary>
    [JsonProperty("tagTypeName")]
    public string TagTypeName { get; set; } = "";

    /// <summary>Add a leader line to the tag.</summary>
    [JsonProperty("addLeader")]
    public bool AddLeader { get; set; } = false;

    /// <summary>Tag orientation: "Horizontal" | "Vertical".</summary>
    [JsonProperty("orientation")]
    public string Orientation { get; set; } = "Horizontal";
}

public class TagElementsResult
{
    [JsonProperty("taggedCount")] public int TaggedCount { get; set; }
    [JsonProperty("newTagIds")] public List<long> NewTagIds { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}
