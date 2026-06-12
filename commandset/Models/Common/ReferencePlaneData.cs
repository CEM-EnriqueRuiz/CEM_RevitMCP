using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Common;

/// <summary>
///     Generic information for reference plane creation.
///     All coordinates are in millimeters.
/// </summary>
public class ReferencePlaneData
{
    /// <summary>
    ///     Creation method: "ByLine" (bubbleEnd + freeEnd),
    ///     "ByPoints" (bubbleEnd + freeEnd + thirdPoint),
    ///     "ByNormal" (origin + normal + length)
    /// </summary>
    [JsonProperty("creationMethod")]
    public string CreationMethod { get; set; } = "ByLine";

    /// <summary>
    ///     Optional name for the reference plane
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Bubble end point (mm) - for ByLine / ByPoints
    /// </summary>
    [JsonProperty("bubbleEnd")]
    public JZPoint BubbleEnd { get; set; }

    /// <summary>
    ///     Free end point (mm) - for ByLine / ByPoints
    /// </summary>
    [JsonProperty("freeEnd")]
    public JZPoint FreeEnd { get; set; }

    /// <summary>
    ///     Third point defining the plane (mm) - for ByPoints
    /// </summary>
    [JsonProperty("thirdPoint")]
    public JZPoint ThirdPoint { get; set; }

    /// <summary>
    ///     Origin point (mm) - for ByNormal
    /// </summary>
    [JsonProperty("origin")]
    public JZPoint Origin { get; set; }

    /// <summary>
    ///     Normal vector (unitless direction) - for ByNormal
    /// </summary>
    [JsonProperty("normal")]
    public JZPoint Normal { get; set; }

    /// <summary>
    ///     Length of the reference plane line (mm) - for ByNormal. Default 3000mm.
    /// </summary>
    [JsonProperty("length")]
    public double Length { get; set; } = 3000;

    /// <summary>
    ///     Cut vector (direction the plane extends). Defaults to Z axis,
    ///     which is correct for plan views.
    /// </summary>
    [JsonProperty("cutVector")]
    public JZPoint CutVector { get; set; }

    /// <summary>
    ///     View to create the plane in. -1 = active view.
    /// </summary>
    [JsonProperty("viewId")]
    public int ViewId { get; set; } = -1;
}
