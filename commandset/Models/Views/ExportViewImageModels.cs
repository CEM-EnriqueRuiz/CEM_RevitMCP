using Newtonsoft.Json;

namespace CEM_IAModeler_CommandSet.Models.Views;

// ─────────────────────────────────────────────────────────────────────────────
//  export_view_image  (Document.ExportImage of one view; read-only: the detail level is rolled back)
// ─────────────────────────────────────────────────────────────────────────────
public class ExportViewImageRequest
{
    /// <summary>The view's element id. 0 or absent = use viewName, or the active view when both are absent.</summary>
    [JsonProperty("viewId")] public long ViewId { get; set; }

    /// <summary>The view's name (case-insensitive). Used when viewId is absent.</summary>
    [JsonProperty("viewName")] public string ViewName { get; set; } = "";

    /// <summary>Width of the image in pixels (fit to it; the height follows the view's aspect).</summary>
    [JsonProperty("pixelSize")] public int PixelSize { get; set; } = 1600;

    /// <summary>"Coarse" | "Medium" | "Fine", applied only for the export. Empty = the view's own.</summary>
    [JsonProperty("detailLevel")] public string DetailLevel { get; set; } = "";
}

public class ExportViewImageResult
{
    /// <summary>The PNG, a new file under %TEMP%\Cemengal\RevitMCP\images.</summary>
    [JsonProperty("filePath")] public string FilePath { get; set; }

    [JsonProperty("bytes")] public long Bytes { get; set; }

    /// <summary>The image's size in pixels, read from the PNG header.</summary>
    [JsonProperty("width")] public int Width { get; set; }
    [JsonProperty("height")] public int Height { get; set; }

    [JsonProperty("viewId")] public long ViewId { get; set; }
    [JsonProperty("viewName")] public string ViewName { get; set; }
    [JsonProperty("viewType")] public string ViewType { get; set; }

    /// <summary>The detail level the image shows.</summary>
    [JsonProperty("detailLevel")] public string DetailLevel { get; set; }

    [JsonProperty("documentTitle")] public string DocumentTitle { get; set; }
    [JsonProperty("isFamilyDocument")] public bool IsFamilyDocument { get; set; }

    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}
