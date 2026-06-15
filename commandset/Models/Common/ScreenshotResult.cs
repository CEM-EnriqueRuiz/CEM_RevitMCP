using Newtonsoft.Json;

namespace CEM_IAModeler_CommandSet.Models.Common
{
    /// <summary>
    /// 截图命令的返回结果：把Revit主窗口的实时截图以base64编码的PNG返回，
    /// 供AI模型直接"看到"当前结果并评估/改进。
    /// Result of the screenshot command: a live grab of the Revit main window
    /// returned as a base64-encoded PNG so the AI model can visually evaluate the result.
    /// </summary>
    public class ScreenshotResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>Base64-encoded PNG image data (no data-uri prefix).</summary>
        [JsonProperty("imageBase64")]
        public string ImageBase64 { get; set; } = string.Empty;

        [JsonProperty("mimeType")]
        public string MimeType { get; set; } = "image/png";

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        /// <summary>Optional path where a copy of the screenshot was saved (audit trail).</summary>
        [JsonProperty("savedPath")]
        public string SavedPath { get; set; } = string.Empty;

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
