using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Core
{
    /// <summary>Shared material-graphics helpers for the viz_* handlers.</summary>
    internal static class VizHelpers
    {
        public static Material ResolveMaterial(Document doc, string nameOrId)
        {
            var mats = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>().ToList();
            if (long.TryParse(nameOrId, out long id))
            {
                var byId = mats.FirstOrDefault(m => m.Id.GetValue() == id);
                if (byId != null) return byId;
            }
            return mats.FirstOrDefault(m => m.Name.Equals(nameOrId?.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public static FillPatternElement ResolveFillPattern(Document doc, string nameOrId)
        {
            if (string.IsNullOrWhiteSpace(nameOrId)) return null;
            var all = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement)).Cast<FillPatternElement>().ToList();
            if (long.TryParse(nameOrId, out long id))
            {
                var byId = all.FirstOrDefault(p => p.Id.GetValue() == id);
                if (byId != null) return byId;
            }
            return all.FirstOrDefault(p => p.Name.Equals(nameOrId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Apply the shading graphics fields shared by create + set. Records what changed in `applied`.</summary>
        public static void ApplyGraphics(Document doc, Material mat,
            List<int> colorRgb, int? transparency, int? shininess, int? smoothness,
            string surfacePatternName, string cutPatternName, string appearanceAssetName,
            List<string> applied, List<string> warnings)
        {
            if (colorRgb != null && colorRgb.Count == 3)
            {
                mat.Color = new Color((byte)colorRgb[0], (byte)colorRgb[1], (byte)colorRgb[2]);
                applied.Add("color");
            }
            if (transparency.HasValue)
            {
                mat.Transparency = Math.Max(0, Math.Min(100, transparency.Value));
                applied.Add($"transparency={mat.Transparency}");
            }
            if (shininess.HasValue)
            {
                try { mat.Shininess = Math.Max(0, Math.Min(128, shininess.Value)); applied.Add($"shininess={mat.Shininess}"); }
                catch { warnings.Add("Shininess not settable."); }
            }
            if (smoothness.HasValue)
            {
                try { mat.Smoothness = Math.Max(0, Math.Min(100, smoothness.Value)); applied.Add($"smoothness={mat.Smoothness}"); }
                catch { warnings.Add("Smoothness not settable."); }
            }

            if (!string.IsNullOrWhiteSpace(surfacePatternName))
            {
                var fp = ResolveFillPattern(doc, surfacePatternName);
                if (fp != null)
                {
                    try { mat.SurfaceForegroundPatternId = fp.Id; applied.Add($"surfacePattern={fp.Name}"); }
                    catch { warnings.Add("Surface pattern not settable."); }
                }
                else warnings.Add($"Surface pattern '{surfacePatternName}' not found.");
            }
            if (!string.IsNullOrWhiteSpace(cutPatternName))
            {
                var fp = ResolveFillPattern(doc, cutPatternName);
                if (fp != null)
                {
                    try { mat.CutForegroundPatternId = fp.Id; applied.Add($"cutPattern={fp.Name}"); }
                    catch { warnings.Add("Cut pattern not settable."); }
                }
                else warnings.Add($"Cut pattern '{cutPatternName}' not found.");
            }

            if (!string.IsNullOrWhiteSpace(appearanceAssetName))
            {
                var asset = new FilteredElementCollector(doc).OfClass(typeof(AppearanceAssetElement))
                    .Cast<AppearanceAssetElement>()
                    .FirstOrDefault(a => a.Name.Equals(appearanceAssetName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (asset != null)
                {
                    try { mat.AppearanceAssetId = asset.Id; applied.Add($"appearanceAsset={asset.Name}"); }
                    catch { warnings.Add("Appearance asset not assignable."); }
                }
                else warnings.Add($"Appearance asset '{appearanceAssetName}' not found.");
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  viz_create_material
    // ─────────────────────────────────────────────────────────────────────────
    public class CreateMaterialEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public CreateMaterialRequest Request { get; set; }
        public AIResult<MaterialResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new MaterialResult();
            try
            {
                if (string.IsNullOrWhiteSpace(Request.Name)) throw new ArgumentException("name is required.");

                Material mat;
                using (var tx = new Transaction(doc, "Create Material"))
                {
                    tx.Start();
                    var existing = VizHelpers.ResolveMaterial(doc, Request.Name);
                    if (existing != null)
                    {
                        mat = existing;
                        res.Warnings.Add($"Material '{Request.Name}' already exists; updating it.");
                    }
                    else
                    {
                        var newId = Material.Create(doc, Request.Name);
                        mat = doc.GetElement(newId) as Material;
                    }

                    VizHelpers.ApplyGraphics(doc, mat, Request.ColorRgb, Request.Transparency, Request.Shininess,
                        Request.Smoothness, Request.SurfacePatternName, Request.CutPatternName,
                        Request.AppearanceAssetName, res.Applied, res.Warnings);
                    tx.Commit();
                }

                res.MaterialId = mat.Id.GetValue();
                res.Name = mat.Name;
                Result = new AIResult<MaterialResult>
                {
                    Success = true,
                    Message = $"Material '{mat.Name}' (id {res.MaterialId}) ready: {(res.Applied.Count > 0 ? string.Join(", ", res.Applied) : "defaults")}" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<MaterialResult>
                    { Success = false, Message = $"Error creating material: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Create Material";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  viz_set_material_appearance
    // ─────────────────────────────────────────────────────────────────────────
    public class SetMaterialAppearanceEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public SetMaterialAppearanceRequest Request { get; set; }
        public AIResult<MaterialResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new MaterialResult();
            try
            {
                var mat = VizHelpers.ResolveMaterial(doc, Request.Material);
                if (mat == null) throw new ArgumentException($"Material '{Request.Material}' not found.");

                using (var tx = new Transaction(doc, "Set Material Appearance"))
                {
                    tx.Start();
                    VizHelpers.ApplyGraphics(doc, mat, Request.ColorRgb, Request.Transparency, Request.Shininess,
                        Request.Smoothness, Request.SurfacePatternName, Request.CutPatternName,
                        Request.AppearanceAssetName, res.Applied, res.Warnings);
                    tx.Commit();
                }

                res.MaterialId = mat.Id.GetValue();
                res.Name = mat.Name;
                Result = new AIResult<MaterialResult>
                {
                    Success = res.Applied.Count > 0,
                    Message = $"Material '{mat.Name}': {(res.Applied.Count > 0 ? string.Join(", ", res.Applied) : "nothing changed")}" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<MaterialResult>
                    { Success = false, Message = $"Error setting material appearance: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Set Material Appearance";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  viz_list_materials
    // ─────────────────────────────────────────────────────────────────────────
    public class ListMaterialsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public ListMaterialsRequest Request { get; set; }
        public AIResult<ListMaterialsResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new ListMaterialsResult();
            try
            {
                var mats = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>();
                string filter = Request?.NameFilter?.Trim() ?? "";
                foreach (var m in mats)
                {
                    if (filter.Length > 0 && m.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    res.Materials.Add(new MaterialInfo
                    {
                        Id = m.Id.GetValue(),
                        Name = m.Name,
                        ColorRgb = m.Color != null ? new List<int> { m.Color.Red, m.Color.Green, m.Color.Blue } : null,
                        Transparency = m.Transparency,
                        MaterialClass = m.MaterialClass
                    });
                }
                res.Count = res.Materials.Count;
                Result = new AIResult<ListMaterialsResult>
                    { Success = true, Message = $"Found {res.Count} material(s).", Response = res };
            }
            catch (Exception ex)
            {
                Result = new AIResult<ListMaterialsResult>
                    { Success = false, Message = $"Error listing materials: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "List Materials";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  viz_assign_material
    // ─────────────────────────────────────────────────────────────────────────
    public class AssignMaterialEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public AssignMaterialRequest Request { get; set; }
        public AIResult<AssignMaterialResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new AssignMaterialResult();
            try
            {
                var mat = VizHelpers.ResolveMaterial(doc, Request.Material);
                if (mat == null) throw new ArgumentException($"Material '{Request.Material}' not found.");
                res.MaterialId = mat.Id.GetValue();

                if (Request.ElementIds == null || Request.ElementIds.Count == 0)
                    throw new ArgumentException("elementIds is required.");

                bool paint = Request.Mode?.Trim().Equals("paint", StringComparison.OrdinalIgnoreCase) == true;

                using (var tx = new Transaction(doc, "Assign Material"))
                {
                    tx.Start();
                    foreach (var idVal in Request.ElementIds)
                    {
                        var el = McpResolveUtils.GetElement(doc, idVal);
                        if (el == null) { res.Warnings.Add($"Element {idVal} not found; skipped."); continue; }

                        try
                        {
                            if (paint)
                            {
                                var opt = new Options { ComputeReferences = true, IncludeNonVisibleObjects = false };
                                var geom = el.get_Geometry(opt);
                                int painted = PaintFaces(el, geom, mat.Id);
                                if (painted > 0) res.AssignedCount++;
                                else res.Warnings.Add($"Element {idVal}: no paintable faces found; skipped.");
                            }
                            else
                            {
                                if (SetMaterialParameter(el, mat.Id)) res.AssignedCount++;
                                else res.Warnings.Add($"Element {idVal}: no writable Material parameter; try mode=\"paint\".");
                            }
                        }
                        catch (Exception exItem) { res.Warnings.Add($"Element {idVal}: {exItem.Message}"); }
                    }
                    tx.Commit();
                }

                Result = new AIResult<AssignMaterialResult>
                {
                    Success = res.AssignedCount > 0,
                    Message = $"Assigned material '{mat.Name}' to {res.AssignedCount}/{Request.ElementIds.Count} element(s) via {(paint ? "paint" : "parameter")}" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<AssignMaterialResult>
                    { Success = false, Message = $"Error assigning material: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        private int PaintFaces(Element el, GeometryElement geom, ElementId matId)
        {
            int n = 0;
            if (geom == null) return 0;
            foreach (var go in geom)
            {
                if (go is Solid solid)
                {
                    foreach (Face f in solid.Faces)
                    {
                        try { doc.Paint(el.Id, f, matId); n++; } catch { }
                    }
                }
                else if (go is GeometryInstance gi)
                {
                    n += PaintFaces(el, gi.GetInstanceGeometry(), matId);
                }
            }
            return n;
        }

        private bool SetMaterialParameter(Element el, ElementId matId)
        {
            // instance "Structural Material"/"Material" first, then the element type's material param
            var p = el.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)
                    ?? el.LookupParameter("Material");
            if (p != null && !p.IsReadOnly && p.StorageType == StorageType.ElementId)
            {
                p.Set(matId); return true;
            }

            var typeId = el.GetTypeId();
            if (typeId != ElementId.InvalidElementId)
            {
                var type = doc.GetElement(typeId);
                var tp = type?.LookupParameter("Material")
                         ?? type?.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                if (tp != null && !tp.IsReadOnly && tp.StorageType == StorageType.ElementId)
                {
                    tp.Set(matId); return true;
                }
            }
            return false;
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Assign Material";
    }
}
