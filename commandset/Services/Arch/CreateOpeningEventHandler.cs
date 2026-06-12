using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Arch
{
    /// <summary>
    ///     Creates an opening in a host. For a wall host: rectangular opening between the two
    ///     extreme boundary corners (NewOpening(Wall, XYZ, XYZ)). For other hosts (floor/roof/
    ///     ceiling): a vertical shaft-style opening from the host's level using the boundary
    ///     profile (NewOpening(Level, Level, CurveArray)).
    /// </summary>
    public class CreateOpeningEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public CreateOpeningRequest Request { get; set; }
        public AIResult<ArchCreateResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new ArchCreateResult();
            try
            {
                Element host = McpResolveUtils.GetElement(doc, Request.HostId);
                if (host == null) { Fail($"Host {Request.HostId} not found."); return; }

                var pts = Request.Boundary.Select(JZPoint.ToXYZ).ToList();

                using (var tx = new Transaction(doc, "Create Opening"))
                {
                    tx.Start();
                    Opening opening = null;

                    if (host is Wall wall)
                    {
                        // Rectangular opening from bounding corners of the supplied points
                        XYZ min = new XYZ(pts.Min(p => p.X), pts.Min(p => p.Y), pts.Min(p => p.Z));
                        XYZ max = new XYZ(pts.Max(p => p.X), pts.Max(p => p.Y), pts.Max(p => p.Z));
                        opening = doc.Create.NewOpening(wall, min, max);
                    }
                    else
                    {
                        // Shaft / vertical opening through the host using its level
                        var profile = new CurveArray();
                        var loopPts = new List<XYZ>(pts);
                        if (loopPts.First().DistanceTo(loopPts.Last()) > 1e-6) loopPts.Add(loopPts.First());
                        for (int i = 0; i < loopPts.Count - 1; i++)
                        {
                            if (loopPts[i].DistanceTo(loopPts[i + 1]) < 1e-6) continue;
                            profile.Append(Line.CreateBound(loopPts[i], loopPts[i + 1]));
                        }

                        ElementId levelId = host.LevelId;
                        Level level = levelId != ElementId.InvalidElementId ? doc.GetElement(levelId) as Level : null;
                        level ??= McpResolveUtils.ResolveLevel(doc, "");
                        if (level == null) { res.Warnings.Add("No level for shaft opening."); }
                        else opening = doc.Create.NewOpening(level, level, profile);
                    }

                    if (opening != null) res.CreatedIds.Add(opening.Id.GetValue());
                    tx.Commit();
                }

                res.CreatedCount = res.CreatedIds.Count;
                Result = new AIResult<ArchCreateResult>
                {
                    Success = res.CreatedCount > 0,
                    Message = res.CreatedCount > 0
                        ? $"Created opening (id {res.CreatedIds[0]}) in {host.GetType().Name}."
                        : "Opening creation returned nothing." + (res.Warnings.Count > 0 ? $" ⚠ {res.Warnings[0]}" : ""),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<ArchCreateResult> { Success = false, Message = $"Error creating opening: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }

            void Fail(string msg)
            {
                Result = new AIResult<ArchCreateResult> { Success = false, Message = msg, Response = res };
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Create Opening";
    }
}
