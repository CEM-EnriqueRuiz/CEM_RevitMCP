using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Arch
{
    // ----- arch_create_roof -----
    public class CreateRoofEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public CreateRoofRequest Request { get; set; }
        public AIResult<ArchCreateResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new ArchCreateResult();
            try
            {
                var roofType = McpResolveUtils.ResolveType(doc, Request.TypeName, typeof(RoofType)) as RoofType
                               ?? McpResolveUtils.FirstTypeOfClass(doc, typeof(RoofType)) as RoofType;
                var level = McpResolveUtils.ResolveLevel(doc, Request.Level);
                if (roofType == null) { Fail(res, "No RoofType available."); return; }
                if (level == null) { Fail(res, "No level available."); return; }

                var ca = new CurveArray();
                var pts = Request.Boundary.Select(JZPoint.ToXYZ).ToList();
                if (pts.First().DistanceTo(pts.Last()) > 1e-6) pts.Add(pts.First());
                for (int i = 0; i < pts.Count - 1; i++) { if (pts[i].DistanceTo(pts[i + 1]) > 1e-6) ca.Append(Line.CreateBound(pts[i], pts[i + 1])); }

                using (var tx = new Transaction(doc, "Create Roof"))
                {
                    tx.Start();
                    var roof = doc.Create.NewFootPrintRoof(ca, level, roofType, out _);
                    if (roof != null) res.CreatedIds.Add(roof.Id.GetValue());
                    tx.Commit();
                }
                res.CreatedCount = res.CreatedIds.Count;
                Result = new AIResult<ArchCreateResult> { Success = res.CreatedCount > 0, Message = res.CreatedCount > 0 ? $"Created roof (id {res.CreatedIds[0]})." : "Roof not created.", Response = res };
            }
            catch (Exception ex) { Fail(res, ex.Message); }
            finally { _e.Set(); }
        }
        private void Fail(ArchCreateResult r, string m) { Result = new AIResult<ArchCreateResult> { Success = false, Message = m, Response = r }; _e.Set(); }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Create Roof";
    }

    // ----- arch_create_curtain_wall ----- (same as a wall, but with a curtain wall type)
    public class CreateCurtainWallEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public CreateCurtainWallRequest Request { get; set; }
        public AIResult<ArchCreateResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new ArchCreateResult();
            try
            {
                // Prefer a wall type whose Kind is Curtain
                var curtain = new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>()
                    .FirstOrDefault(w => !string.IsNullOrWhiteSpace(Request.TypeName) && w.Name.Equals(Request.TypeName, StringComparison.OrdinalIgnoreCase))
                    ?? new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().FirstOrDefault(w => w.Kind == WallKind.Curtain);
                if (curtain == null) { Fail(res, "No curtain WallType available."); return; }
                var level = McpResolveUtils.ResolveLevel(doc, Request.Level);
                if (level == null) { Fail(res, "No level available."); return; }

                using (var tx = new Transaction(doc, "Create Curtain Wall"))
                {
                    tx.Start();
                    var curve = Line.CreateBound(JZPoint.ToXYZ(Request.Start), JZPoint.ToXYZ(Request.End));
                    var wall = Wall.Create(doc, curve, curtain.Id, level.Id, McpResolveUtils.MmToFt(Request.Height), 0, false, false);
                    if (wall != null) res.CreatedIds.Add(wall.Id.GetValue());
                    tx.Commit();
                }
                res.CreatedCount = res.CreatedIds.Count;
                Result = new AIResult<ArchCreateResult> { Success = res.CreatedCount > 0, Message = res.CreatedCount > 0 ? $"Created curtain wall (id {res.CreatedIds[0]})." : "Curtain wall not created.", Response = res };
            }
            catch (Exception ex) { Fail(res, ex.Message); }
            finally { _e.Set(); }
        }
        private void Fail(ArchCreateResult r, string m) { Result = new AIResult<ArchCreateResult> { Success = false, Message = m, Response = r }; _e.Set(); }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Create Curtain Wall";
    }

    // ----- arch_join_geometry / unjoin -----
    public class JoinGeometryEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public JoinRequest Request { get; set; }
        public bool Join { get; set; } = true;
        public AIResult<ArchCreateResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new ArchCreateResult();
            try
            {
                var a = McpResolveUtils.GetElement(doc, Request.FirstId);
                var b = McpResolveUtils.GetElement(doc, Request.SecondId);
                if (a == null || b == null) { Fail(res, "One or both elements not found."); return; }
                using (var tx = new Transaction(doc, Join ? "Join Geometry" : "Unjoin Geometry"))
                {
                    tx.Start();
                    if (Join)
                    {
                        if (!JoinGeometryUtils.AreElementsJoined(doc, a, b)) JoinGeometryUtils.JoinGeometry(doc, a, b);
                    }
                    else
                    {
                        if (JoinGeometryUtils.AreElementsJoined(doc, a, b)) JoinGeometryUtils.UnjoinGeometry(doc, a, b);
                    }
                    tx.Commit();
                }
                res.CreatedCount = 1;
                Result = new AIResult<ArchCreateResult> { Success = true, Message = Join ? "Joined geometry." : "Unjoined geometry.", Response = res };
            }
            catch (Exception ex) { Fail(res, ex.Message); }
            finally { _e.Set(); }
        }
        private void Fail(ArchCreateResult r, string m) { Result = new AIResult<ArchCreateResult> { Success = false, Message = m, Response = r }; _e.Set(); }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Join Geometry";
    }

    // ----- arch_create_separator (room/space/area separation lines) -----
    public class CreateSeparatorEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public CreateSeparatorRequest Request { get; set; }
        public AIResult<ArchCreateResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new ArchCreateResult();
            try
            {
                View view = McpResolveUtils.ResolveView(doc, uiDoc, Request.ViewId.ToString()) ?? doc.ActiveView;
                var pts = Request.Points.Select(JZPoint.ToXYZ).ToList();
                if (Request.Closed && pts.First().DistanceTo(pts.Last()) > 1e-6) pts.Add(pts.First());
                var curves = new CurveArray();
                for (int i = 0; i < pts.Count - 1; i++) { if (pts[i].DistanceTo(pts[i + 1]) > 1e-6) curves.Append(Line.CreateBound(pts[i], pts[i + 1])); }

                using (var tx = new Transaction(doc, "Create Separator"))
                {
                    tx.Start();
                    var sp = SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, pts.First()));
                    string kind = (Request.Kind ?? "room").Trim().ToLowerInvariant();
                    if (kind == "space")
                    {
                        var created = doc.Create.NewSpaceBoundaryLines(sp, curves, view);
                        foreach (ModelCurve c in created) res.CreatedIds.Add(c.Id.GetValue());
                    }
                    else if (kind == "area")
                    {
                        if (view is ViewPlan vp)
                            foreach (Curve cu in curves)
                            {
                                var mc = doc.Create.NewAreaBoundaryLine(sp, cu, vp);
                                if (mc != null) res.CreatedIds.Add(mc.Id.GetValue());
                            }
                        else res.Warnings.Add("Area separators require an area plan view.");
                    }
                    else
                    {
                        var created = doc.Create.NewRoomBoundaryLines(sp, curves, view);
                        foreach (ModelCurve c in created) res.CreatedIds.Add(c.Id.GetValue());
                    }
                    tx.Commit();
                }
                res.CreatedCount = res.CreatedIds.Count;
                Result = new AIResult<ArchCreateResult> { Success = res.CreatedCount > 0, Message = $"Created {res.CreatedCount} separation line(s).", Response = res };
            }
            catch (Exception ex) { Fail(res, ex.Message); }
            finally { _e.Set(); }
        }
        private void Fail(ArchCreateResult r, string m) { Result = new AIResult<ArchCreateResult> { Success = false, Message = m, Response = r }; _e.Set(); }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Create Separator";
    }

    // ----- arch_create_area_plan -----
    public class CreateAreaPlanEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public CreateAreaPlanRequest Request { get; set; }
        public AIResult<ArchCreateResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new ArchCreateResult();
            try
            {
                var level = McpResolveUtils.ResolveLevel(doc, Request.Level);
                if (level == null) { Fail(res, "No level available."); return; }
                var schemes = new FilteredElementCollector(doc).OfClass(typeof(AreaScheme)).Cast<AreaScheme>().ToList();
                var scheme = schemes.FirstOrDefault(s => !string.IsNullOrWhiteSpace(Request.SchemeName) && s.Name.Equals(Request.SchemeName, StringComparison.OrdinalIgnoreCase)) ?? schemes.FirstOrDefault();
                if (scheme == null) { Fail(res, "No AreaScheme available."); return; }

                using (var tx = new Transaction(doc, "Create Area Plan"))
                {
                    tx.Start();
                    var vp = ViewPlan.CreateAreaPlan(doc, scheme.Id, level.Id);
                    if (vp != null)
                    {
                        if (!string.IsNullOrWhiteSpace(Request.Name)) { try { vp.Name = Request.Name; } catch { } }
                        res.CreatedIds.Add(vp.Id.GetValue());
                    }
                    tx.Commit();
                }
                res.CreatedCount = res.CreatedIds.Count;
                Result = new AIResult<ArchCreateResult> { Success = res.CreatedCount > 0, Message = res.CreatedCount > 0 ? $"Created area plan (id {res.CreatedIds[0]})." : "Area plan not created.", Response = res };
            }
            catch (Exception ex) { Fail(res, ex.Message); }
            finally { _e.Set(); }
        }
        private void Fail(ArchCreateResult r, string m) { Result = new AIResult<ArchCreateResult> { Success = false, Message = m, Response = r }; _e.Set(); }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Create Area Plan";
    }

    // ----- arch_create_area -----
    public class CreateAreaEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public CreateAreaRequest Request { get; set; }
        public AIResult<ArchCreateResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new ArchCreateResult();
            try
            {
                if (!(McpResolveUtils.GetElement(doc, Request.AreaViewId) is ViewPlan areaView) || areaView.ViewType != ViewType.AreaPlan)
                { Fail(res, "areaViewId must be an area plan view."); return; }

                using (var tx = new Transaction(doc, "Create Area"))
                {
                    tx.Start();
                    foreach (var jp in Request.Points)
                    {
                        try
                        {
                            var uv = new UV(McpResolveUtils.MmToFt(jp.X), McpResolveUtils.MmToFt(jp.Y));
                            var area = doc.Create.NewArea(areaView, uv);
                            if (area != null) res.CreatedIds.Add(area.Id.GetValue());
                        }
                        catch (Exception ex) { res.Warnings.Add($"Point failed: {ex.Message}"); }
                    }
                    tx.Commit();
                }
                res.CreatedCount = res.CreatedIds.Count;
                Result = new AIResult<ArchCreateResult> { Success = res.CreatedCount > 0, Message = $"Created {res.CreatedCount} area(s).", Response = res };
            }
            catch (Exception ex) { Fail(res, ex.Message); }
            finally { _e.Set(); }
        }
        private void Fail(ArchCreateResult r, string m) { Result = new AIResult<ArchCreateResult> { Success = false, Message = m, Response = r }; _e.Set(); }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Create Area";
    }

    // ----- arch_create_stairs (straight run between two levels, inside a StairsEditScope) -----
    public class CreateStairsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public CreateStairsRequest Request { get; set; }
        public AIResult<ArchCreateResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new ArchCreateResult();
            try
            {
                var baseLevel = McpResolveUtils.ResolveLevel(doc, Request.BaseLevel);
                var topLevel = McpResolveUtils.ResolveLevel(doc, Request.TopLevel)
                               ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
                                    .Where(l => baseLevel != null && l.Elevation > baseLevel.Elevation).OrderBy(l => l.Elevation).FirstOrDefault();
                if (baseLevel == null || topLevel == null) { Fail(res, "Need a base level and a higher top level."); return; }

                var stairsType = McpResolveUtils.ResolveType(doc, Request.TypeName, typeof(StairsType)) as StairsType
                                 ?? McpResolveUtils.FirstTypeOfClass(doc, typeof(StairsType)) as StairsType;
                if (stairsType == null) { Fail(res, "No StairsType available."); return; }

                var editScope = new StairsEditScope(doc, "MCP Create Stairs");
                ElementId stairsId = editScope.Start(baseLevel.Id, topLevel.Id);
                using (var tx = new Transaction(doc, "Create Stairs Run"))
                {
                    tx.Start();
                    var stairs = doc.GetElement(stairsId) as Stairs;
                    if (stairsType != null && stairs != null)
                    {
                        try { stairs.ChangeTypeId(stairsType.Id); } catch { }
                    }

                    XYZ p1 = JZPoint.ToXYZ(Request.RunStart);
                    XYZ p2 = JZPoint.ToXYZ(Request.RunEnd);
                    Line path = Line.CreateBound(new XYZ(p1.X, p1.Y, baseLevel.Elevation), new XYZ(p2.X, p2.Y, baseLevel.Elevation));
                    var run = StairsRun.CreateStraightRun(doc, stairsId, path, StairsRunJustification.Center);
                    run?.get_Parameter(BuiltInParameter.STAIRS_RUN_ACTUAL_RUN_WIDTH)?.Set(McpResolveUtils.MmToFt(Request.Width));
                    tx.Commit();
                }
                editScope.Commit(new StairsFailurePreprocessor());
                res.CreatedIds.Add(stairsId.GetValue()); res.CreatedCount = 1;
                Result = new AIResult<ArchCreateResult> { Success = true, Message = $"Created stairs (id {stairsId.GetValue()}) from {baseLevel.Name} to {topLevel.Name}.", Response = res };
            }
            catch (Exception ex) { Fail(res, ex.Message); }
            finally { _e.Set(); }
        }
        private void Fail(ArchCreateResult r, string m) { Result = new AIResult<ArchCreateResult> { Success = false, Message = m, Response = r }; _e.Set(); }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Create Stairs";
    }

    // ----- arch_create_railing (on existing stairs/ramp) -----
    public class CreateRailingEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public CreateRailingRequest Request { get; set; }
        public AIResult<ArchCreateResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new ArchCreateResult();
            try
            {
                var host = McpResolveUtils.GetElement(doc, Request.HostId);
                if (host == null) { Fail(res, "Host (stairs/ramp) not found."); return; }
                var railingType = McpResolveUtils.ResolveType(doc, Request.TypeName, typeof(RailingType)) as RailingType
                                  ?? McpResolveUtils.FirstTypeOfClass(doc, typeof(RailingType)) as RailingType;
                if (railingType == null) { Fail(res, "No RailingType available."); return; }
                var pos = (Request.Position ?? "Treads").Trim().Equals("Stringer", StringComparison.OrdinalIgnoreCase)
                    ? RailingPlacementPosition.Stringer : RailingPlacementPosition.Treads;

                using (var tx = new Transaction(doc, "Create Railing"))
                {
                    tx.Start();
                    var ids = Railing.Create(doc, host.Id, railingType.Id, pos);
                    foreach (var id in ids) res.CreatedIds.Add(id.GetValue());
                    tx.Commit();
                }
                res.CreatedCount = res.CreatedIds.Count;
                Result = new AIResult<ArchCreateResult> { Success = res.CreatedCount > 0, Message = $"Created {res.CreatedCount} railing(s) on host {Request.HostId}.", Response = res };
            }
            catch (Exception ex) { Fail(res, ex.Message); }
            finally { _e.Set(); }
        }
        private void Fail(ArchCreateResult r, string m) { Result = new AIResult<ArchCreateResult> { Success = false, Message = m, Response = r }; _e.Set(); }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Create Railing";
    }

    /// <summary>Minimal failure preprocessor for stairs edit scope commit.</summary>
    public class StairsFailurePreprocessor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor a) => FailureProcessingResult.Continue;
    }
}
