using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.ParentTools
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Shared helpers for the parent_* pipeline (link resolution + geometry).
    //  These distill the blocks that were copy-pasted across ~74 recipes.
    // ─────────────────────────────────────────────────────────────────────────
    internal static class ParentHelpers
    {
        public const double FT = 304.8; // mm per foot

        /// <summary>Resolve a Revit link instance by name/id, or the first loaded one.</summary>
        public static RevitLinkInstance ResolveLink(Document doc, string nameOrId, List<string> warnings)
        {
            var links = new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .Where(li => li.GetLinkDocument() != null)
                .ToList();

            if (links.Count == 0) return null;

            RevitLinkInstance link = null;
            if (!string.IsNullOrWhiteSpace(nameOrId))
            {
                if (long.TryParse(nameOrId, out long id))
                    link = links.FirstOrDefault(l => l.Id.GetValue() == id);
                link ??= links.FirstOrDefault(l => l.Name.IndexOf(nameOrId.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);
                if (link == null) warnings?.Add($"Link '{nameOrId}' not found; using first loaded link.");
            }
            return link ?? links.First();
        }

        /// <summary>Collect transformed solids (link transform applied) from a link element. Mirrors the recipe "Grab" helper.</summary>
        public static List<Solid> GrabSolids(Element e, Transform tf, Options opt)
        {
            var solids = new List<Solid>();
            var ge = e.get_Geometry(opt);
            if (ge != null) Recurse(ge, tf, solids);
            return solids;
        }

        private static void Recurse(GeometryElement ge, Transform tf, List<Solid> acc)
        {
            foreach (GeometryObject g in ge)
            {
                if (g is Solid s && s.Volume > 1e-9)
                    acc.Add(tf == null || tf.IsIdentity ? s : SolidUtils.CreateTransformed(s, tf));
                else if (g is GeometryInstance gi)
                    Recurse(gi.GetInstanceGeometry(), tf, acc);
            }
        }

        /// <summary>World-space bbox (min/max, feet) from a solid's triangulated faces. Robust to transformed solids.</summary>
        public static bool TriangulatedBounds(IEnumerable<Solid> solids, out XYZ min, out XYZ max)
        {
            double mnx = 1e9, mny = 1e9, mnz = 1e9, mxx = -1e9, mxy = -1e9, mxz = -1e9;
            bool any = false;
            foreach (var s in solids)
            {
                foreach (Face f in s.Faces)
                {
                    var m = f.Triangulate();
                    if (m == null) continue;
                    foreach (var v in m.Vertices)
                    {
                        any = true;
                        mnx = Math.Min(mnx, v.X); mny = Math.Min(mny, v.Y); mnz = Math.Min(mnz, v.Z);
                        mxx = Math.Max(mxx, v.X); mxy = Math.Max(mxy, v.Y); mxz = Math.Max(mxz, v.Z);
                    }
                }
            }
            min = any ? new XYZ(mnx, mny, mnz) : null;
            max = any ? new XYZ(mxx, mxy, mxz) : null;
            return any;
        }

        public static double Round(double v, double stepMm)
            => stepMm <= 0 ? v : Math.Round(v / stepMm) * stepMm;

        public static JZPoint ToJZmm(XYZ ft) => new JZPoint(ft.X * FT, ft.Y * FT, ft.Z * FT);

        public static List<BuiltInCategory> ResolveCategories(IEnumerable<string> names, List<string> warnings)
        {
            var result = new List<BuiltInCategory>();
            foreach (var n in names ?? Enumerable.Empty<string>())
            {
                if (McpResolveUtils.TryResolveBuiltInCategory(n, out var bic)) result.Add(bic);
                else warnings?.Add($"Category '{n}' not recognized; skipped.");
            }
            return result;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  parent_link_extract_geometry  (read-only)
    // ─────────────────────────────────────────────────────────────────────────
    public class LinkExtractEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public LinkExtractRequest Request { get; set; }
        public AIResult<LinkExtractResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new LinkExtractResult();
            try
            {
                var link = ParentHelpers.ResolveLink(doc, Request.LinkName, res.Warnings);
                if (link == null) throw new InvalidOperationException("No loaded Revit link found.");
                var ldoc = link.GetLinkDocument();
                var tf = link.GetTotalTransform();
                res.LinkName = link.Name;
                res.LinkTitle = ldoc.Title;

                var wantCats = ParentHelpers.ResolveCategories(Request.Categories, res.Warnings);

                IEnumerable<Element> elems = new FilteredElementCollector(ldoc)
                    .WhereElementIsNotElementType()
                    .Where(e => e.Category != null && e.Category.CategoryType == CategoryType.Model);
                if (wantCats.Count > 0)
                {
                    var idSet = wantCats.Select(c => (long)(int)c).ToHashSet();
                    elems = elems.Where(e => idSet.Contains(e.Category.Id.GetValue()));
                }
                var list = elems.ToList();
                res.TotalElements = list.Count;

                foreach (var e in list)
                {
                    var cat = e.Category.Name;
                    res.CountsByCategory[cat] = res.CountsByCategory.TryGetValue(cat, out var c) ? c + 1 : 1;
                }

                if (Request.IncludeGeometry)
                {
                    var opt = new Options { ComputeReferences = false, DetailLevel = ViewDetailLevel.Medium };
                    int taken = 0;
                    foreach (var e in list)
                    {
                        if (taken >= Request.MaxElements) { res.Warnings.Add($"Geometry capped at {Request.MaxElements} elements."); break; }
                        var solids = ParentHelpers.GrabSolids(e, tf, opt);
                        if (solids.Count == 0) continue;
                        if (!ParentHelpers.TriangulatedBounds(solids, out var min, out var max)) continue;
                        var t = ldoc.GetElement(e.GetTypeId());
                        res.Geometry.Add(new LinkElementGeometry
                        {
                            LinkElementId = e.Id.GetValue(),
                            UniqueId = e.UniqueId,
                            Category = e.Category.Name,
                            TypeName = t?.Name ?? e.Name,
                            BBoxMin = ParentHelpers.ToJZmm(min),
                            BBoxMax = ParentHelpers.ToJZmm(max),
                            VolumeMm3 = solids.Sum(s => s.Volume) * ParentHelpers.FT * ParentHelpers.FT * ParentHelpers.FT
                        });
                        taken++;
                    }
                }

                Result = new AIResult<LinkExtractResult>
                {
                    Success = true,
                    Message = $"Link '{res.LinkName}' ({res.LinkTitle}): {res.TotalElements} model elements across {res.CountsByCategory.Count} categories" +
                              (Request.IncludeGeometry ? $", {res.Geometry.Count} geometry summaries" : "") +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<LinkExtractResult> { Success = false, Message = $"Error: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Parent: Link Extract Geometry";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  parent_solid_to_member_params  (read-only)
    // ─────────────────────────────────────────────────────────────────────────
    public class MemberParamsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public MemberParamsRequest Request { get; set; }
        public AIResult<MemberParamsResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new MemberParamsResult();
            try
            {
                var link = ParentHelpers.ResolveLink(doc, Request.LinkName, res.Warnings);
                if (link == null) throw new InvalidOperationException("No loaded Revit link found.");
                var ldoc = link.GetLinkDocument();
                var tf = link.GetTotalTransform();

                var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();
                var opt = new Options { ComputeReferences = false, DetailLevel = ViewDetailLevel.Fine };

                // Source elements: explicit ids first, else by category.
                var elems = new List<Element>();
                if (Request.LinkElementIds != null && Request.LinkElementIds.Count > 0)
                {
                    foreach (var id in Request.LinkElementIds)
                    {
                        var e = ldoc.GetElement(McpResolveUtils.ToElementId(id));
                        if (e != null) elems.Add(e);
                        else res.Warnings.Add($"Link element id {id} not found.");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(Request.Category) &&
                         McpResolveUtils.TryResolveBuiltInCategory(Request.Category, out var bic))
                {
                    elems = new FilteredElementCollector(ldoc).OfCategory(bic)
                        .WhereElementIsNotElementType().ToElements().ToList();
                }
                else throw new ArgumentException("Provide linkElementIds or a recognized category.");

                int taken = 0;
                foreach (var e in elems)
                {
                    if (taken >= Request.MaxElements) { res.Warnings.Add($"Capped at {Request.MaxElements} elements."); break; }
                    var solids = ParentHelpers.GrabSolids(e, tf, opt);
                    if (solids.Count == 0) continue;
                    var member = DeriveLinearMember(e, solids, levels, Request.RoundMm);
                    if (member != null) { res.Members.Add(member); taken++; }
                }

                Result = new AIResult<MemberParamsResult>
                {
                    Success = true,
                    Message = $"Derived parameters for {res.Members.Count} member(s)" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<MemberParamsResult> { Success = false, Message = $"Error: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        /// <summary>
        ///     Best-fit a linear member: longest bbox axis = centerline, the two smaller dims = section.
        ///     This is the recipe measurement block distilled into one method.
        /// </summary>
        private static DerivedMember DeriveLinearMember(Element e, List<Solid> solids, List<Level> levels, double roundMm)
        {
            if (!ParentHelpers.TriangulatedBounds(solids, out var min, out var max)) return null;
            double F = ParentHelpers.FT;
            double dx = (max.X - min.X) * F, dy = (max.Y - min.Y) * F, dz = (max.Z - min.Z) * F;
            var dims = new[] { dx, dy, dz };
            var c = (min + max) * 0.5;

            // longest axis = length
            int li = 0; for (int i = 1; i < 3; i++) if (dims[i] > dims[li]) li = i;
            double lengthMm = dims[li];
            var sec = dims.Where((_, i) => i != li).OrderBy(v => v).ToArray(); // [b, h]

            XYZ axis = li == 0 ? XYZ.BasisX : li == 1 ? XYZ.BasisY : XYZ.BasisZ;
            XYZ e0 = c - axis * (lengthMm / F / 2.0);
            XYZ e1 = c + axis * (lengthMm / F / 2.0);

            var lvl = levels.Count == 0 ? null : levels.OrderBy(l => Math.Abs(l.Elevation - c.Z)).First();

            return new DerivedMember
            {
                LinkElementId = e.Id.GetValue(),
                UniqueId = e.UniqueId,
                Category = e.Category?.Name,
                Start = ParentHelpers.ToJZmm(e0),
                End = ParentHelpers.ToJZmm(e1),
                LengthMm = ParentHelpers.Round(lengthMm, roundMm),
                SectionBMm = ParentHelpers.Round(sec[0], roundMm),
                SectionHMm = ParentHelpers.Round(sec[1], roundMm),
                NearestLevel = lvl?.Name
            };
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Parent: Solid To Member Params";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  parent_reconstruct_native  (write — idempotent via CEMAI app-id tag)
    // ─────────────────────────────────────────────────────────────────────────
    public class ReconstructEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public const string AppId = "CEMAI";

        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public ReconstructRequest Request { get; set; }
        public AIResult<ReconstructResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new ReconstructResult { Category = Request.Category, Mode = Request.Mode };
            try
            {
                var link = ParentHelpers.ResolveLink(doc, Request.LinkName, res.Warnings);
                if (link == null) throw new InvalidOperationException("No loaded Revit link found.");
                var ldoc = link.GetLinkDocument();
                var tf = link.GetTotalTransform();

                if (!McpResolveUtils.TryResolveBuiltInCategory(Request.Category, out var bic))
                    throw new ArgumentException($"Category '{Request.Category}' not recognized.");

                var opt = new Options { ComputeReferences = false, DetailLevel = ViewDetailLevel.Medium };
                var srcCatId = new ElementId(bic);

                var sources = new FilteredElementCollector(ldoc).OfCategory(bic)
                    .WhereElementIsNotElementType().ToElements().ToList();

                using (var tx = new Transaction(doc, "Reconstruct native from link"))
                {
                    tx.Start();

                    // Idempotent: clear prior reconstruction tagged with our app-id, same category.
                    if (Request.ClearPrevious)
                    {
                        var old = new FilteredElementCollector(doc).OfClass(typeof(DirectShape))
                            .Cast<DirectShape>()
                            .Where(d => d.ApplicationId == AppId && d.Category != null &&
                                        d.Category.Id.GetValue() == (long)(int)bic)
                            .Select(d => d.Id).ToList();
                        foreach (var id in old) { try { doc.Delete(id); res.Cleared++; } catch { } }
                    }

                    int taken = 0;
                    foreach (var e in sources)
                    {
                        if (taken >= Request.MaxElements) { res.Warnings.Add($"Capped at {Request.MaxElements} source elements."); break; }
                        taken++;
                        try
                        {
                            var solids = ParentHelpers.GrabSolids(e, tf, opt);
                            if (solids.Count == 0) { res.Failed++; continue; }

                            // Robust generic path: copy the source solids into a tagged DirectShape.
                            var geom = solids.Cast<GeometryObject>().ToList();
                            var ds = DirectShape.CreateElement(doc, srcCatId);
                            ds.ApplicationId = AppId;
                            ds.ApplicationDataId = e.UniqueId; // back-pointer to the source for validation
                            ds.SetShape(geom);
                            try { ds.Name = "NAT-" + Trunc(e.Name, 200); } catch { }
                            res.Created++;
                            res.CreatedElementIds.Add(ds.Id.GetValue());
                        }
                        catch { res.Failed++; }
                    }
                    tx.Commit();
                }

                if (!string.Equals(Request.Mode, "directshape", StringComparison.OrdinalIgnoreCase))
                    res.Warnings.Add($"Mode '{Request.Mode}' not yet specialized; used robust DirectShape copy. Use parent_solid_to_member_params + struct_/arch_ create tools for fully typed native elements.");

                Result = new AIResult<ReconstructResult>
                {
                    Success = res.Created > 0 || sources.Count == 0,
                    Message = $"Reconstructed {res.Created} native element(s) for {Request.Category} " +
                              $"(cleared {res.Cleared}, failed {res.Failed})" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<ReconstructResult> { Success = false, Message = $"Error: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        private static string Trunc(string s, int n) => string.IsNullOrEmpty(s) ? s : (s.Length <= n ? s : s.Substring(0, n));

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Parent: Reconstruct Native";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  parent_validate_deviation  (read-only)
    // ─────────────────────────────────────────────────────────────────────────
    public class ValidateDeviationEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public ValidateDeviationRequest Request { get; set; }
        public AIResult<ValidateDeviationResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new ValidateDeviationResult();
            try
            {
                var link = ParentHelpers.ResolveLink(doc, Request.LinkName, res.Warnings);
                if (link == null) throw new InvalidOperationException("No loaded Revit link found.");
                var ldoc = link.GetLinkDocument();
                var tf = link.GetTotalTransform();
                var opt = new Options { ComputeReferences = false, DetailLevel = ViewDetailLevel.Medium };
                double F = ParentHelpers.FT;

                var natives = new FilteredElementCollector(doc).OfClass(typeof(DirectShape))
                    .Cast<DirectShape>()
                    .Where(d => d.ApplicationId == Request.AppId && !string.IsNullOrEmpty(d.ApplicationDataId))
                    .ToList();

                double sumAvg = 0, worstAll = 0;
                int n = 0;
                foreach (var d in natives)
                {
                    if (n >= Request.MaxElements) { res.Warnings.Add($"Capped at {Request.MaxElements} items."); break; }
                    var src = ldoc.GetElement(d.ApplicationDataId);
                    if (src == null) continue;

                    var nativeSolids = ParentHelpers.GrabSolids(d, null, opt); // native already in host coords
                    var srcSolids = ParentHelpers.GrabSolids(src, tf, opt);
                    if (nativeSolids.Count == 0 || srcSolids.Count == 0) continue;

                    // Sample source surface points, measure distance to nearest native face.
                    var pts = new List<XYZ>();
                    foreach (var s in srcSolids)
                        foreach (Face f in s.Faces)
                        {
                            var m = f.Triangulate(0.1);
                            if (m == null) continue;
                            int step = Math.Max(1, m.Vertices.Count / 6);
                            for (int i = 0; i < m.Vertices.Count; i += step) pts.Add(m.Vertices[i]);
                        }
                    if (pts.Count == 0) continue;

                    double sum = 0, worst = 0; int k = 0;
                    foreach (var p in pts)
                    {
                        double dist = NearestFaceDistance(p, nativeSolids) * F;
                        sum += dist; if (dist > worst) worst = dist; k++;
                    }
                    if (k == 0) continue;
                    double avg = sum / k;
                    n++;
                    sumAvg += avg; if (worst > worstAll) worstAll = worst;
                    bool ok = worst <= Request.ToleranceMm;
                    if (!ok) res.ItemsOutOfTolerance++;
                    res.Items.Add(new DeviationItem
                    {
                        NativeElementId = d.Id.GetValue(),
                        SourceUniqueId = d.ApplicationDataId,
                        AvgMm = Math.Round(avg, 1),
                        WorstMm = Math.Round(worst, 1),
                        WithinTolerance = ok
                    });
                }

                res.ItemsChecked = n;
                res.OverallAvgMm = n > 0 ? Math.Round(sumAvg / n, 1) : 0;
                res.OverallWorstMm = Math.Round(worstAll, 1);

                Result = new AIResult<ValidateDeviationResult>
                {
                    Success = true,
                    Message = $"Validated {n} native item(s): avg {res.OverallAvgMm}mm, worst {res.OverallWorstMm}mm, " +
                              $"{res.ItemsOutOfTolerance} over {Request.ToleranceMm}mm" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<ValidateDeviationResult> { Success = false, Message = $"Error: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        private static double NearestFaceDistance(XYZ p, List<Solid> solids)
        {
            double d = double.MaxValue;
            foreach (var s in solids)
                foreach (Face f in s.Faces)
                {
                    var pr = f.Project(p);
                    if (pr != null && pr.Distance < d) d = pr.Distance;
                }
            return d == double.MaxValue ? 0 : d;
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Parent: Validate Deviation";
    }
}
