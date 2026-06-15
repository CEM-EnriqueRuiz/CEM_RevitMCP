using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Core
{
    internal static class DbGapHelpers
    {
        /// <summary>Build a SketchPlane through the first point with the requested normal.</summary>
        public static SketchPlane MakeSketchPlane(Document doc, JZPoint origin, string plane)
        {
            XYZ o = origin != null ? JZPoint.ToXYZ(origin) : XYZ.Zero;
            XYZ normal = (plane ?? "XY").Trim().ToUpperInvariant() switch
            {
                "XZ" => XYZ.BasisY,
                "YZ" => XYZ.BasisX,
                _ => XYZ.BasisZ
            };
            return SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(normal, o));
        }

        /// <summary>Turn a point list into bound line segments (mm → ft), optionally closed.</summary>
        public static List<Line> BuildSegments(IList<JZPoint> pts, bool closed, List<string> warnings)
        {
            var lines = new List<Line>();
            if (pts == null || pts.Count < 2) { warnings.Add("Need at least 2 points."); return lines; }
            var xyz = pts.Select(JZPoint.ToXYZ).ToList();
            if (closed && xyz.First().DistanceTo(xyz.Last()) > 1e-6) xyz.Add(xyz.First());
            for (int i = 0; i < xyz.Count - 1; i++)
            {
                if (xyz[i].DistanceTo(xyz[i + 1]) < 1e-6) continue;
                lines.Add(Line.CreateBound(xyz[i], xyz[i + 1]));
            }
            return lines;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  create_model_lines
    // ─────────────────────────────────────────────────────────────────────────
    public class CreateModelLinesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public CreateModelLinesRequest Request { get; set; }
        public AIResult<CreatedIdsResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new CreatedIdsResult();
            try
            {
                var segs = DbGapHelpers.BuildSegments(Request.Points, Request.Closed, res.Warnings);
                if (segs.Count == 0) throw new ArgumentException("No valid segments from points.");

                using (var tx = new Transaction(doc, "Create Model Lines"))
                {
                    tx.Start();
                    var sp = DbGapHelpers.MakeSketchPlane(doc, Request.Points[0], Request.Plane);
                    foreach (var line in segs)
                    {
                        try { var mc = doc.Create.NewModelCurve(line, sp); res.CreatedIds.Add(mc.Id.GetValue()); }
                        catch (Exception ex) { res.Warnings.Add($"Segment skipped: {ex.Message}"); }
                    }
                    tx.Commit();
                }

                Result = new AIResult<CreatedIdsResult>
                {
                    Success = res.CreatedIds.Count > 0,
                    Message = $"Created {res.CreatedIds.Count} model line(s)" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<CreatedIdsResult> { Success = false, Message = $"Error: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Create Model Lines";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  create_detail_lines
    // ─────────────────────────────────────────────────────────────────────────
    public class CreateDetailLinesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public CreateDetailLinesRequest Request { get; set; }
        public AIResult<CreatedIdsResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new CreatedIdsResult();
            try
            {
                var view = McpResolveUtils.ResolveView(doc, uiDoc, Request.ViewId);
                if (view == null) throw new ArgumentException($"View '{Request.ViewId}' not found.");

                var segs = DbGapHelpers.BuildSegments(Request.Points, Request.Closed, res.Warnings);
                if (segs.Count == 0) throw new ArgumentException("No valid segments from points.");

                GraphicsStyle style = null;
                if (!string.IsNullOrWhiteSpace(Request.LineStyleName))
                {
                    style = new FilteredElementCollector(doc).OfClass(typeof(GraphicsStyle)).Cast<GraphicsStyle>()
                        .FirstOrDefault(g => g.Name.Equals(Request.LineStyleName.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (style == null) res.Warnings.Add($"Line style '{Request.LineStyleName}' not found; using default.");
                }

                using (var tx = new Transaction(doc, "Create Detail Lines"))
                {
                    tx.Start();
                    foreach (var line in segs)
                    {
                        try
                        {
                            var dc = doc.Create.NewDetailCurve(view, line);
                            if (style != null) dc.LineStyle = style;
                            res.CreatedIds.Add(dc.Id.GetValue());
                        }
                        catch (Exception ex) { res.Warnings.Add($"Segment skipped: {ex.Message}"); }
                    }
                    tx.Commit();
                }

                Result = new AIResult<CreatedIdsResult>
                {
                    Success = res.CreatedIds.Count > 0,
                    Message = $"Created {res.CreatedIds.Count} detail line(s) in '{view.Name}'" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<CreatedIdsResult> { Success = false, Message = $"Error: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Create Detail Lines";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  create_direct_shape
    // ─────────────────────────────────────────────────────────────────────────
    public class CreateDirectShapeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public CreateDirectShapeRequest Request { get; set; }
        public AIResult<CreateDirectShapeResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new CreateDirectShapeResult();
            try
            {
                if (!McpResolveUtils.TryResolveBuiltInCategory(Request.Category, out var bic))
                    throw new ArgumentException($"Unknown category '{Request.Category}'.");
                var catId = new ElementId(bic);

                Solid solid = BuildSolid(res);
                if (solid == null) throw new ArgumentException("Could not build geometry; check shape/min/max/profile.");

                DirectShape ds;
                using (var tx = new Transaction(doc, "Create Direct Shape"))
                {
                    tx.Start();
                    ds = DirectShape.CreateElement(doc, catId);
                    ds.SetShape(new List<GeometryObject> { solid });
                    if (!string.IsNullOrWhiteSpace(Request.Name)) { try { ds.Name = Request.Name; } catch { } }
                    tx.Commit();
                }

                res.ElementId = ds.Id.GetValue();
                Result = new AIResult<CreateDirectShapeResult>
                {
                    Success = true,
                    Message = $"Created DirectShape (id {res.ElementId}) in category {bic}" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<CreateDirectShapeResult> { Success = false, Message = $"Error: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        private Solid BuildSolid(CreateDirectShapeResult res)
        {
            string kind = (Request.Shape ?? "box").Trim().ToLowerInvariant();
            if (kind == "box")
            {
                if (Request.Min == null || Request.Max == null) { res.Warnings.Add("box needs min and max."); return null; }
                XYZ a = JZPoint.ToXYZ(Request.Min), b = JZPoint.ToXYZ(Request.Max);
                double z0 = Math.Min(a.Z, b.Z), z1 = Math.Max(a.Z, b.Z);
                double x0 = Math.Min(a.X, b.X), x1 = Math.Max(a.X, b.X);
                double y0 = Math.Min(a.Y, b.Y), y1 = Math.Max(a.Y, b.Y);
                if (z1 - z0 < 1e-9 || x1 - x0 < 1e-9 || y1 - y0 < 1e-9) { res.Warnings.Add("box has zero extent."); return null; }

                var loop = new CurveLoop();
                var p1 = new XYZ(x0, y0, z0); var p2 = new XYZ(x1, y0, z0);
                var p3 = new XYZ(x1, y1, z0); var p4 = new XYZ(x0, y1, z0);
                loop.Append(Line.CreateBound(p1, p2)); loop.Append(Line.CreateBound(p2, p3));
                loop.Append(Line.CreateBound(p3, p4)); loop.Append(Line.CreateBound(p4, p1));
                return GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { loop }, XYZ.BasisZ, z1 - z0);
            }
            else // extrusion
            {
                var loop = McpResolveUtils.BuildCurveLoop(Request.Profile, out string w);
                if (loop == null) { res.Warnings.Add(w ?? "invalid profile."); return null; }
                double h = McpResolveUtils.MmToFt(Math.Abs(Request.Height));
                if (h < 1e-9) { res.Warnings.Add("height is zero."); return null; }
                return GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { loop }, XYZ.BasisZ, h);
            }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Create Direct Shape";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  create_spot_dimension
    // ─────────────────────────────────────────────────────────────────────────
    public class CreateSpotDimensionEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public CreateSpotDimensionRequest Request { get; set; }
        public AIResult<CreateSpotDimensionResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new CreateSpotDimensionResult();
            try
            {
                var view = McpResolveUtils.ResolveView(doc, uiDoc, Request.ViewId);
                if (view == null) throw new ArgumentException($"View '{Request.ViewId}' not found.");
                if (Request.Point == null) throw new ArgumentException("point is required.");

                var el = McpResolveUtils.GetElement(doc, Request.ElementId);
                if (el == null) throw new ArgumentException($"Element {Request.ElementId} not found.");

                Reference reference = GetReference(el);
                if (reference == null) throw new InvalidOperationException("Could not obtain a geometric reference from the element.");

                XYZ origin = JZPoint.ToXYZ(Request.Point);
                XYZ bend = Request.Bend != null ? JZPoint.ToXYZ(Request.Bend) : origin + new XYZ(2, 2, 0);
                XYZ end = Request.End != null ? JZPoint.ToXYZ(Request.End) : bend + new XYZ(2, 0, 0);

                SpotDimension sd;
                using (var tx = new Transaction(doc, "Create Spot Dimension"))
                {
                    tx.Start();
                    bool coord = (Request.Kind ?? "elevation").Trim().Equals("coordinate", StringComparison.OrdinalIgnoreCase);
                    sd = coord
                        ? doc.Create.NewSpotCoordinate(view, reference, origin, bend, end, origin, Request.HasLeader)
                        : doc.Create.NewSpotElevation(view, reference, origin, bend, end, origin, Request.HasLeader);
                    tx.Commit();
                }

                if (sd == null) throw new InvalidOperationException("Revit returned no spot dimension (reference may not be valid here).");
                res.SpotDimensionId = sd.Id.GetValue();
                Result = new AIResult<CreateSpotDimensionResult>
                {
                    Success = true,
                    Message = $"Created spot {Request.Kind} (id {res.SpotDimensionId}) in '{view.Name}'.",
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<CreateSpotDimensionResult> { Success = false, Message = $"Error: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        private Reference GetReference(Element el)
        {
            var opt = new Options { ComputeReferences = true, IncludeNonVisibleObjects = false, View = uiDoc.ActiveView };
            var geom = el.get_Geometry(opt);
            if (geom == null) return null;
            foreach (var go in geom)
            {
                if (go is Solid s && s.Faces.Size > 0)
                    foreach (Face f in s.Faces) if (f.Reference != null) return f.Reference;
                if (go is GeometryInstance gi)
                    foreach (var go2 in gi.GetInstanceGeometry())
                        if (go2 is Solid s2 && s2.Faces.Size > 0)
                            foreach (Face f in s2.Faces) if (f.Reference != null) return f.Reference;
            }
            return null;
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Create Spot Dimension";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  create_assembly
    // ─────────────────────────────────────────────────────────────────────────
    public class CreateAssemblyEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public CreateAssemblyRequest Request { get; set; }
        public AIResult<CreateAssemblyResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new CreateAssemblyResult();
            try
            {
                if (Request.ElementIds == null || Request.ElementIds.Count == 0)
                    throw new ArgumentException("elementIds is required.");

                var ids = new List<ElementId>();
                ElementId namingCat = ElementId.InvalidElementId;
                foreach (var v in Request.ElementIds)
                {
                    var el = McpResolveUtils.GetElement(doc, v);
                    if (el == null) { res.Warnings.Add($"Element {v} not found; skipped."); continue; }
                    ids.Add(el.Id);
                    if (namingCat == ElementId.InvalidElementId && el.Category != null) namingCat = el.Category.Id;
                }
                if (ids.Count == 0) throw new ArgumentException("No valid elements to assemble.");
                if (namingCat == ElementId.InvalidElementId) throw new InvalidOperationException("No category available for the assembly.");

                AssemblyInstance asm;
                using (var tx = new Transaction(doc, "Create Assembly"))
                {
                    tx.Start();
                    if (!AssemblyInstance.IsValidNamingCategory(doc, namingCat, ids))
                        res.Warnings.Add("Naming category may not be valid for all members; Revit will pick a default.");
                    asm = AssemblyInstance.Create(doc, ids, namingCat);
                    tx.Commit();
                }

                // Naming may require a separate transaction after creation regenerates.
                if (!string.IsNullOrWhiteSpace(Request.Name))
                {
                    using (var tx = new Transaction(doc, "Name Assembly"))
                    {
                        tx.Start();
                        try { asm.AssemblyTypeName = Request.Name; } catch { res.Warnings.Add($"Could not set name '{Request.Name}'."); }
                        tx.Commit();
                    }
                }

                res.AssemblyId = asm.Id.GetValue();
                res.MemberCount = ids.Count;
                res.Name = asm.AssemblyTypeName;
                Result = new AIResult<CreateAssemblyResult>
                {
                    Success = true,
                    Message = $"Created assembly '{res.Name}' (id {res.AssemblyId}) with {res.MemberCount} member(s)" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<CreateAssemblyResult> { Success = false, Message = $"Error: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Create Assembly";
    }

    // NOTE: create_scope_box was dropped — Revit 2024 exposes NO public API to create a scope box
    // (only VolumeOfInterest* properties exist, no factory). Use send_code_to_revit if needed.
    // See CEM_RevitMCP.md §6.

    // ─────────────────────────────────────────────────────────────────────────
    //  create_multi_segment_grid
    // ─────────────────────────────────────────────────────────────────────────
    public class CreateMultiSegmentGridEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public CreateMultiSegmentGridRequest Request { get; set; }
        public AIResult<CreateMultiSegmentGridResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new CreateMultiSegmentGridResult();
            try
            {
                if (Request.Points == null || Request.Points.Count < 2)
                    throw new ArgumentException("points (>=2) required.");

                var gridType = McpResolveUtils.ResolveType(doc, Request.GridTypeName, typeof(GridType))
                               ?? McpResolveUtils.FirstTypeOfClass(doc, typeof(GridType));
                if (gridType == null) throw new InvalidOperationException("No GridType available.");

                var xyz = Request.Points.Select(JZPoint.ToXYZ).ToList();
                var loop = new CurveLoop();
                int segs = 0;
                for (int i = 0; i < xyz.Count - 1; i++)
                {
                    if (xyz[i].DistanceTo(xyz[i + 1]) < 1e-6) continue;
                    loop.Append(Line.CreateBound(xyz[i], xyz[i + 1]));
                    segs++;
                }
                if (segs == 0) throw new ArgumentException("No valid segments.");

                ElementId gridId;
                using (var tx = new Transaction(doc, "Create Multi-Segment Grid"))
                {
                    tx.Start();
                    gridId = MultiSegmentGrid.Create(doc, gridType.Id, loop, doc.ActiveView?.SketchPlane?.Id ?? ElementId.InvalidElementId);
                    tx.Commit();
                }

                res.GridId = gridId.GetValue();
                Result = new AIResult<CreateMultiSegmentGridResult>
                {
                    Success = true,
                    Message = $"Created multi-segment grid (id {res.GridId}) with {segs} segment(s).",
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<CreateMultiSegmentGridResult> { Success = false, Message = $"Error: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Create Multi-Segment Grid";
    }
}
