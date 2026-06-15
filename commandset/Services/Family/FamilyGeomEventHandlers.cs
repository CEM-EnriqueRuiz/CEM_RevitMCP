using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Models.Family;
using CEM_IAModeler_CommandSet.Services.Core;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Family
{
    internal static class FamilyGeomHelpers
    {
        public static bool RequireFamilyDoc(Document doc, string tool, out AIResult<FamilyOpResult> fail)
        {
            if (doc.IsFamilyDocument) { fail = null; return true; }
            fail = new AIResult<FamilyOpResult>
            {
                Success = false,
                Message = $"{tool} only works inside the Family Editor (open an .rfa first).",
                Response = new FamilyOpResult()
            };
            return false;
        }

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

        /// <summary>Build a sketch plane from an arbitrary normal + origin (mm). Used for sloped panels.</summary>
        public static SketchPlane MakeSketchPlane(Document doc, PlaneDef def)
        {
            XYZ o = def?.Origin != null ? JZPoint.ToXYZ(def.Origin) : XYZ.Zero;
            XYZ n = (def?.Normal != null && def.Normal.Count >= 3)
                ? new XYZ(def.Normal[0], def.Normal[1], def.Normal[2])
                : XYZ.BasisZ;
            if (n.GetLength() < 1e-9) n = XYZ.BasisZ;
            n = n.Normalize();
            return SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(n, o));
        }

        /// <summary>First point of a loop (segment start or first polyline point), for an XY/XZ/YZ plane origin.</summary>
        public static JZPoint FirstPointOf(ProfileLoop loop)
        {
            if (loop == null) return null;
            if (loop.Segments != null && loop.Segments.Count > 0 && loop.Segments[0]?.Start != null)
                return loop.Segments[0].Start;
            if (loop.Points != null && loop.Points.Count > 0) return loop.Points[0];
            return null;
        }

        /// <summary>Build one closed CurveArray loop from a ProfileLoop (segments win over points; supports arcs).</summary>
        public static CurveArray BuildLoop(ProfileLoop loop, out string warning)
        {
            warning = null;
            if (loop == null) { warning = "Null loop."; return null; }

            // Full-control path: explicit segments (lines + arcs).
            if (loop.Segments != null && loop.Segments.Count > 0)
            {
                var ca = new CurveArray();
                int n = 0;
                foreach (var seg in loop.Segments)
                {
                    if (seg?.Start == null || seg.End == null) continue;
                    XYZ s = JZPoint.ToXYZ(seg.Start), e = JZPoint.ToXYZ(seg.End);
                    if (s.DistanceTo(e) < 1e-6) continue;
                    try
                    {
                        if (seg.IsArc && seg.Mid != null)
                            ca.Append(Arc.Create(s, e, JZPoint.ToXYZ(seg.Mid)));
                        else
                            ca.Append(Line.CreateBound(s, e));
                        n++;
                    }
                    catch { /* skip a degenerate segment, keep building */ }
                }
                if (n < 2) { warning = "Loop collapsed to <2 valid segments."; return null; }
                return ca;
            }

            // Shorthand path: a polyline of points, auto-closed with straight segments.
            return BuildLoopFromPoints(loop.Points, out warning);
        }

        private static CurveArray BuildLoopFromPoints(IList<JZPoint> pts, out string warning)
        {
            warning = null;
            if (pts == null || pts.Count < 3) { warning = "Loop needs >=3 points."; return null; }
            var xyz = pts.Select(JZPoint.ToXYZ).ToList();
            if (xyz.First().DistanceTo(xyz.Last()) > 1e-6) xyz.Add(xyz.First());
            var ca = new CurveArray();
            int n = 0;
            for (int i = 0; i < xyz.Count - 1; i++)
            {
                if (xyz[i].DistanceTo(xyz[i + 1]) < 1e-6) continue;
                ca.Append(Line.CreateBound(xyz[i], xyz[i + 1]));
                n++;
            }
            if (n < 3) { warning = "Loop collapsed to <3 valid segments."; return null; }
            return ca;
        }

        /// <summary>Build a CurveArrArray (one closed loop) from mm points. LEGACY single-loop path.</summary>
        public static CurveArrArray BuildProfile(IList<JZPoint> pts, out string warning)
        {
            var ca = BuildLoopFromPoints(pts, out warning);
            if (ca == null) return null;
            var caa = new CurveArrArray();
            caa.Append(ca);
            return caa;
        }

        /// <summary>
        ///     Build a CurveArrArray from multiple loops (first = outer, rest = holes). Falls back to the
        ///     legacy single profile when no loops given. Returns null with a warning if nothing valid.
        /// </summary>
        public static CurveArrArray BuildProfile(IList<ProfileLoop> loops, IList<JZPoint> legacyProfile, out string warning)
        {
            warning = null;
            if (loops != null && loops.Count > 0)
            {
                var caa = new CurveArrArray();
                int added = 0;
                var loopWarnings = new List<string>();
                foreach (var loop in loops)
                {
                    var ca = BuildLoop(loop, out string w);
                    if (ca == null) { if (w != null) loopWarnings.Add(w); continue; }
                    caa.Append(ca);
                    added++;
                }
                if (added == 0) { warning = "No valid loops. " + string.Join("; ", loopWarnings); return null; }
                if (loopWarnings.Count > 0) warning = string.Join("; ", loopWarnings);
                return caa;
            }
            return BuildProfile(legacyProfile, out warning);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  family_create_extrusion
    // ─────────────────────────────────────────────────────────────────────────
    public class FamilyExtrusionEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public FamilyExtrusionRequest Request { get; set; }
        public AIResult<FamilyOpResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new FamilyOpResult();
            try
            {
                if (!FamilyGeomHelpers.RequireFamilyDoc(doc, "family_create_extrusion", out var fail)) { Result = fail; _resetEvent.Set(); return; }

                var profile = FamilyGeomHelpers.BuildProfile(Request.Loops, Request.Profile, out string w);
                if (profile == null) throw new ArgumentException(w ?? "invalid profile (provide loops or a >=3-point profile).");
                if (!string.IsNullOrWhiteSpace(w)) res.Warnings.Add(w);

                // Resolve the material id once (before the transaction) so we can set it on the form.
                ElementId matId = ElementId.InvalidElementId;
                if (!string.IsNullOrWhiteSpace(Request.Material))
                {
                    var mat = VizHelpers.ResolveMaterial(doc, Request.Material);
                    if (mat != null) matId = mat.Id;
                    else res.Warnings.Add($"Material '{Request.Material}' not found in family; left unset.");
                }

                using (var tx = new Transaction(doc, "Family Extrusion"))
                {
                    tx.Start();

                    // Plane: arbitrary normal+origin wins; else legacy XY/XZ/YZ through the first point.
                    SketchPlane sp;
                    if (Request.PlaneDef != null)
                        sp = FamilyGeomHelpers.MakeSketchPlane(doc, Request.PlaneDef);
                    else
                    {
                        var planeOrigin = (Request.Loops != null && Request.Loops.Count > 0)
                            ? FamilyGeomHelpers.FirstPointOf(Request.Loops[0])
                            : (Request.Profile != null && Request.Profile.Count > 0 ? Request.Profile[0] : null);
                        sp = FamilyGeomHelpers.MakeSketchPlane(doc, planeOrigin, Request.Plane);
                    }

                    double end = McpResolveUtils.MmToFt(Request.Height);
                    var ext = doc.FamilyCreate.NewExtrusion(Request.IsSolid, profile, sp, end);
                    if (ext != null)
                    {
                        res.Ids.Add(ext.Id.GetValue());
                        if (matId != ElementId.InvalidElementId)
                        {
                            var mp = ext.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                            if (mp != null && !mp.IsReadOnly) mp.Set(matId);
                            else res.Warnings.Add("Material parameter not writable on the created extrusion.");
                        }
                    }
                    tx.Commit();
                }
                res.Count = res.Ids.Count;
                Result = new AIResult<FamilyOpResult>
                {
                    Success = res.Count > 0,
                    Message = (res.Count > 0 ? $"Created family {(Request.IsSolid ? "solid" : "void")} extrusion (id {res.Ids[0]})." : "Extrusion not created.")
                              + (res.Warnings.Count > 0 ? $" ⚠ {string.Join("; ", res.Warnings)}" : ""),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<FamilyOpResult> { Success = false, Message = $"Error creating extrusion: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Family Extrusion";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  family_create_revolution
    // ─────────────────────────────────────────────────────────────────────────
    public class FamilyRevolutionEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public FamilyRevolutionRequest Request { get; set; }
        public AIResult<FamilyOpResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new FamilyOpResult();
            try
            {
                if (!FamilyGeomHelpers.RequireFamilyDoc(doc, "family_create_revolution", out var fail)) { Result = fail; _resetEvent.Set(); return; }
                if (Request.AxisStart == null || Request.AxisEnd == null) throw new ArgumentException("axisStart and axisEnd are required.");

                var profile = FamilyGeomHelpers.BuildProfile(Request.Profile, out string w);
                if (profile == null) throw new ArgumentException(w ?? "invalid profile.");

                using (var tx = new Transaction(doc, "Family Revolution"))
                {
                    tx.Start();
                    var sp = FamilyGeomHelpers.MakeSketchPlane(doc, Request.Profile[0], Request.Plane);
                    var axis = Line.CreateBound(JZPoint.ToXYZ(Request.AxisStart), JZPoint.ToXYZ(Request.AxisEnd));
                    double a0 = Request.StartAngle * Math.PI / 180.0;
                    double a1 = Request.EndAngle * Math.PI / 180.0;
                    var rev = doc.FamilyCreate.NewRevolution(Request.IsSolid, profile, sp, axis, a0, a1);
                    if (rev != null) res.Ids.Add(rev.Id.GetValue());
                    tx.Commit();
                }
                res.Count = res.Ids.Count;
                Result = new AIResult<FamilyOpResult>
                {
                    Success = res.Count > 0,
                    Message = res.Count > 0 ? $"Created family {(Request.IsSolid ? "solid" : "void")} revolution (id {res.Ids[0]})." : "Revolution not created.",
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<FamilyOpResult> { Success = false, Message = $"Error creating revolution: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Family Revolution";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  family_associate_parameter
    // ─────────────────────────────────────────────────────────────────────────
    public class FamilyAssociateParameterEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public FamilyAssociateParameterRequest Request { get; set; }
        public AIResult<FamilyOpResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new FamilyOpResult();
            try
            {
                if (!FamilyGeomHelpers.RequireFamilyDoc(doc, "family_associate_parameter", out var fail)) { Result = fail; _resetEvent.Set(); return; }

                var el = McpResolveUtils.GetElement(doc, Request.ElementId);
                if (el == null) throw new ArgumentException($"Element {Request.ElementId} not found in this family.");

                var elemParam = el.LookupParameter(Request.ElementParameter);
                if (elemParam == null) throw new ArgumentException($"Element parameter '{Request.ElementParameter}' not found on element {Request.ElementId}.");

                FamilyManager fm = doc.FamilyManager;
                FamilyParameter famParam = fm.get_Parameter(Request.FamilyParameter);
                if (famParam == null) throw new ArgumentException($"Family parameter '{Request.FamilyParameter}' not found. Create it with family_add_parameter first.");

                using (var tx = new Transaction(doc, "Associate Family Parameter"))
                {
                    tx.Start();
                    fm.AssociateElementParameterToFamilyParameter(elemParam, famParam);
                    tx.Commit();
                }

                res.Count = 1;
                Result = new AIResult<FamilyOpResult>
                {
                    Success = true,
                    Message = $"Associated element {Request.ElementId}.'{Request.ElementParameter}' → family parameter '{Request.FamilyParameter}'.",
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<FamilyOpResult> { Success = false, Message = $"Error associating parameter: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Associate Family Parameter";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  family_add_reference_plane
    // ─────────────────────────────────────────────────────────────────────────
    public class FamilyAddReferencePlaneEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public FamilyAddReferencePlaneRequest Request { get; set; }
        public AIResult<FamilyOpResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new FamilyOpResult();
            try
            {
                if (!FamilyGeomHelpers.RequireFamilyDoc(doc, "family_add_reference_plane", out var fail)) { Result = fail; _resetEvent.Set(); return; }
                if (Request.BubbleEnd == null || Request.FreeEnd == null || Request.CutVectorPoint == null)
                    throw new ArgumentException("bubbleEnd, freeEnd and cutVectorPoint are required.");

                using (var tx = new Transaction(doc, "Add Reference Plane"))
                {
                    tx.Start();
                    var view = uiDoc.ActiveView;
                    var rp = doc.FamilyCreate.NewReferencePlane(
                        JZPoint.ToXYZ(Request.BubbleEnd),
                        JZPoint.ToXYZ(Request.FreeEnd),
                        JZPoint.ToXYZ(Request.CutVectorPoint),
                        view);
                    if (rp != null)
                    {
                        if (!string.IsNullOrWhiteSpace(Request.Name)) { try { rp.Name = Request.Name; } catch { res.Warnings.Add($"Could not set name '{Request.Name}'."); } }
                        res.Ids.Add(rp.Id.GetValue());
                    }
                    tx.Commit();
                }
                res.Count = res.Ids.Count;
                Result = new AIResult<FamilyOpResult>
                {
                    Success = res.Count > 0,
                    Message = res.Count > 0 ? $"Added family reference plane (id {res.Ids[0]})" + (res.Warnings.Count > 0 ? $". ⚠ {res.Warnings[0]}" : ".") : "Reference plane not created.",
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<FamilyOpResult> { Success = false, Message = $"Error adding reference plane: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Add Family Reference Plane";
    }
}
