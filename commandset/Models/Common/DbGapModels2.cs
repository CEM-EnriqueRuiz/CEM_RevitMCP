using Newtonsoft.Json;

namespace CEM_IAModeler_CommandSet.Models.Common;

// ─────────────────────────────────────────────────────────────────────────────
//  edit_curtain_grid  (CurtainGrid.AddGridLine)
// ─────────────────────────────────────────────────────────────────────────────
public class EditCurtainGridRequest
{
    /// <summary>Curtain wall / curtain system element id whose grid is edited.</summary>
    [JsonProperty("hostId")] public long HostId { get; set; }

    /// <summary>U grid line positions (points on the surface, mm) to add.</summary>
    [JsonProperty("uGridPoints")] public List<JZPoint> UGridPoints { get; set; } = new();

    /// <summary>V grid line positions (points on the surface, mm) to add.</summary>
    [JsonProperty("vGridPoints")] public List<JZPoint> VGridPoints { get; set; } = new();

    /// <summary>Add only one segment at the picked point (true) or a full grid line (false, default).</summary>
    [JsonProperty("oneSegmentOnly")] public bool OneSegmentOnly { get; set; } = false;
}

public class EditCurtainGridResult
{
    [JsonProperty("hostId")] public long HostId { get; set; }
    [JsonProperty("addedU")] public int AddedU { get; set; }
    [JsonProperty("addedV")] public int AddedV { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  place_image  (ImageType.Create + ImageInstance.Create)
// ─────────────────────────────────────────────────────────────────────────────
public class PlaceImageRequest
{
    /// <summary>Absolute path to the image file (.png/.jpg/.bmp/.tif).</summary>
    [JsonProperty("path")] public string Path { get; set; } = "";

    /// <summary>View id or name to place the image in (-1/empty = active view).</summary>
    [JsonProperty("viewId")] public string ViewId { get; set; } = "";

    /// <summary>Placement center in the view (mm). Optional; view center when omitted.</summary>
    [JsonProperty("center")] public JZPoint Center { get; set; }
}

public class PlaceImageResult
{
    [JsonProperty("imageTypeId")] public long ImageTypeId { get; set; }
    [JsonProperty("imageInstanceId")] public long ImageInstanceId { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  create_parts  (PartUtils.CreateParts)
// ─────────────────────────────────────────────────────────────────────────────
public class CreatePartsRequest
{
    /// <summary>Element ids (walls/floors/etc.) to split into parts.</summary>
    [JsonProperty("elementIds")] public List<long> ElementIds { get; set; } = new();
}

public class CreatePartsResult
{
    [JsonProperty("sourceCount")] public int SourceCount { get; set; }
    [JsonProperty("partCount")] public int PartCount { get; set; }
    [JsonProperty("partIds")] public List<long> PartIds { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}
