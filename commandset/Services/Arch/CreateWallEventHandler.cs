using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Arch
{
    /// <summary>
    ///     Creates straight walls via
    ///     Wall.Create(doc, curve, wallTypeId, levelId, height, offset, flip, structural).
    /// </summary>
    public class CreateWallEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public CreateWallRequest Request { get; set; }
        public AIResult<ArchCreateResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new ArchCreateResult();
            try
            {
                ElementType wallType = McpResolveUtils.ResolveType(doc, Request.TypeName, typeof(WallType))
                                       ?? McpResolveUtils.FirstTypeOfClass(doc, typeof(WallType));
                Level level = McpResolveUtils.ResolveLevel(doc, Request.Level);
                if (wallType == null) { Fail("No WallType available."); return; }
                if (level == null) { Fail("No level available."); return; }

                double offset = McpResolveUtils.MmToFt(Request.BaseOffset);

                using (var tx = new Transaction(doc, "Create Wall"))
                {
                    tx.Start();
                    foreach (var seg in Request.Segments)
                    {
                        if (seg.Start == null || seg.End == null) { res.Warnings.Add("Segment missing start/end; skipped."); continue; }
                        try
                        {
                            XYZ p1 = JZPoint.ToXYZ(seg.Start);
                            XYZ p2 = JZPoint.ToXYZ(seg.End);
                            if (p1.DistanceTo(p2) < 1e-6) { res.Warnings.Add("Zero-length segment skipped."); continue; }

                            double heightMm = seg.Height > 0 ? seg.Height : Request.Height;
                            double height = McpResolveUtils.MmToFt(heightMm);
                            Curve curve = Line.CreateBound(p1, p2);

                            var wall = Wall.Create(doc, curve, wallType.Id, level.Id, height, offset, Request.Flip, Request.Structural);
                            if (wall != null) res.CreatedIds.Add(wall.Id.GetValue());
                        }
                        catch (Exception ex) { res.Warnings.Add($"Segment failed: {ex.Message}"); }
                    }
                    tx.Commit();
                }

                res.CreatedCount = res.CreatedIds.Count;
                Result = new AIResult<ArchCreateResult>
                {
                    Success = res.CreatedCount > 0,
                    Message = $"Created {res.CreatedCount} wall(s)" +
                              (res.Warnings.Count > 0 ? $". ⚠ {res.Warnings.Count} note(s): {res.Warnings[0]}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<ArchCreateResult> { Success = false, Message = $"Error creating walls: {ex.Message}", Response = res };
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

        public string GetName() => "Create Wall";
    }
}
