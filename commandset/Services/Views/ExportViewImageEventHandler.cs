using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_CSharpExtended.Hosting;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Models.Views;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Views
{
    // ─────────────────────────────────────────────────────────────────────────
    //  export_view_image — one view of the active document (family or project) to a PNG. Read-only: the detail level
    //  is set inside a transaction group that is always rolled back, so the document is left exactly as it was.
    // ─────────────────────────────────────────────────────────────────────────
    public class ExportViewImageEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        private ExportViewImageRequest _request;

        /// <summary>Setting the request re-arms the completion signal, before the event is raised (not in the wait).</summary>
        public ExportViewImageRequest Request
        {
            get => _request;
            set { _request = value; _resetEvent.Reset(); }
        }
        public AIResult<ExportViewImageResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new ExportViewImageResult();
            try
            {
                res.DocumentTitle = doc.Title;
                res.IsFamilyDocument = doc.IsFamilyDocument;

                string key = Request.ViewId > 0 ? Request.ViewId.ToString() : Request.ViewName;
                var view = McpResolveUtils.ResolveView(doc, uiApp.ActiveUIDocument, key);
                if (view == null)
                {
                    var names = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
                        .Where(v => !v.IsTemplate && v.CanBePrinted).Select(v => $"{v.Name} ({v.Id.GetValue()})").Take(30);
                    throw new ArgumentException($"View '{key}' not found in '{doc.Title}'. Views: {string.Join(", ", names)}");
                }
                if (!view.CanBePrinted)
                    throw new ArgumentException($"View '{view.Name}' ({view.ViewType}) cannot be exported as an image.");

                res.ViewId = view.Id.GetValue();
                res.ViewName = view.Name;
                res.ViewType = view.ViewType.ToString();

                ViewDetailLevel? level = null;
                if (!string.IsNullOrWhiteSpace(Request.DetailLevel))
                {
                    if (!Enum.TryParse(Request.DetailLevel.Trim(), true, out ViewDetailLevel parsed) || parsed == ViewDetailLevel.Undefined)
                        throw new ArgumentException($"detailLevel '{Request.DetailLevel}' is not Coarse, Medium or Fine.");
                    level = parsed;
                }

                // A new, never-existing name. Revit appends the view's type and name to FilePath, so it exports under a
                // unique stem and the file is renamed to the target.
                string target = CemTempFolder.NewArtifactFile(McpLocalFiles.App, "images", "view", ".png");
                string folder = Path.GetDirectoryName(target);
                string stem = Path.GetFileNameWithoutExtension(target) + "_export";

                var options = new ImageExportOptions
                {
                    ExportRange = ExportRange.SetOfViews,
                    FilePath = Path.Combine(folder, stem),
                    HLRandWFViewsFileType = ImageFileType.PNG,
                    ShadowViewsFileType = ImageFileType.PNG,
                    ImageResolution = ImageResolution.DPI_150,
                    ZoomType = ZoomFitType.FitToPage,
                    FitDirection = FitDirectionType.Horizontal,
                    PixelSize = Request.PixelSize
                };
                options.SetViewsAndSheets(new List<ElementId> { view.Id });

                ViewDetailLevel shown;
                using (var group = new TransactionGroup(doc, "export_view_image (rolled back)"))
                {
                    group.Start();
                    try
                    {
                        if (level.HasValue && view.DetailLevel != level.Value)
                            ApplyDetailLevel(view, level.Value, res.Warnings);
                        shown = view.DetailLevel;

                        // SEC-2: Revit appends " - <view type> - <view name>" to FilePath, and a view name with "/" or ".."
                        // could put the PNG outside images\. An unsafe name is replaced for the export (rolled back), and
                        // the file Revit will write must be directly in images\ before anything is written.
                        MakeExportNameSafe(view, stem, res.Warnings);
                        string predicted = ImageExportOptions.GetFileName(doc, view.Id);
                        bool inside = Path.IsPathRooted(predicted)
                            ? IsDirectlyIn(folder, predicted)
                            : IsDirectlyIn(folder, predicted) && IsDirectlyIn(folder, Path.Combine(folder, stem) + predicted);
                        if (!inside)
                            throw new InvalidOperationException($"Refused: the image would be written outside {folder} ({predicted}).");
                        doc.ExportImage(options);
                    }
                    finally { group.RollBack(); }
                }

                string exported = Directory.GetFiles(folder, stem + "*.png")
                    .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
                if (exported == null) throw new InvalidOperationException("Revit reported no error but wrote no image.");
                File.Move(exported, target);

                res.FilePath = target;
                res.Bytes = new FileInfo(target).Length;
                (res.Width, res.Height) = PngSize(target);
                res.DetailLevel = shown.ToString();

                Result = new AIResult<ExportViewImageResult>
                {
                    Success = true,
                    Message = $"Exported view '{res.ViewName}' ({res.ViewType}, {res.DetailLevel}) of '{res.DocumentTitle}' to {res.FilePath} " +
                              $"({res.Width}x{res.Height} px, {res.Bytes / 1024.0:0.#} KB). The document is unchanged." +
                              (res.Warnings.Count > 0 ? $" ⚠ {string.Join("; ", res.Warnings)}" : ""),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<ExportViewImageResult> { Success = false, Message = $"Error exporting the view image: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        /// <summary>
        /// Sets the detail level inside the caller's rolled-back group. When the view's template controls it, the
        /// template is changed instead: it is rolled back too, and the view keeps every other template setting.
        /// </summary>
        private void ApplyDetailLevel(View view, ViewDetailLevel level, List<string> warnings)
        {
            using (var t = new Transaction(doc, "Detail level for export"))
            {
                t.Start();
                View target = view;
                if (view.ViewTemplateId != ElementId.InvalidElementId && doc.GetElement(view.ViewTemplateId) is View template &&
                    !template.GetNonControlledTemplateParameterIds().Contains(new ElementId(BuiltInParameter.VIEW_DETAIL_LEVEL)))
                    target = template;
                if (target.HasDetailLevel() && target.CanModifyDetailLevel())
                    target.DetailLevel = level;
                else
                    warnings.Add($"View '{view.Name}' has no settable detail level; exported at its own ({view.DetailLevel}).");
                t.Commit();
            }
        }

        /// <summary>
        /// Renames the view, inside the caller's rolled-back group, when its name is not a safe file name part (a path
        /// separator, "..", or another character Windows refuses). The temporary name is the export's unique stem.
        /// </summary>
        private void MakeExportNameSafe(View view, string stem, List<string> warnings)
        {
            string name = view.Name ?? "";
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && !name.Contains("..")) return;
            using (var t = new Transaction(doc, "Safe view name for export"))
            {
                t.Start();
                view.Name = stem;
                t.Commit();
            }
            warnings.Add($"The view's name '{name}' is not safe in a file name, so it was exported under a temporary name (rolled back).");
        }

        /// <summary>True when <paramref name="path"/> (relative to <paramref name="folder"/>, or rooted) is a file directly in it.</summary>
        private static bool IsDirectlyIn(string folder, string path)
        {
            try
            {
                string full = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(folder, path));
                return string.Equals(Path.GetDirectoryName(full)?.TrimEnd('\\'), Path.GetFullPath(folder).TrimEnd('\\'),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        /// <summary>Width and height from a PNG's IHDR chunk (bytes 16-23, big-endian); 0 when unreadable.</summary>
        private static (int, int) PngSize(string path)
        {
            try
            {
                var header = new byte[24];
                using (var stream = File.OpenRead(path))
                    if (stream.Read(header, 0, header.Length) < header.Length) return (0, 0);
                int BigEndian(int i) => (header[i] << 24) | (header[i + 1] << 16) | (header[i + 2] << 8) | header[i + 3];
                return (BigEndian(16), BigEndian(20));
            }
            catch { return (0, 0); }
        }

        public bool WaitForCompletion(int t = 10000) => _resetEvent.WaitOne(t);
        public string GetName() => "Export View Image";
    }
}
