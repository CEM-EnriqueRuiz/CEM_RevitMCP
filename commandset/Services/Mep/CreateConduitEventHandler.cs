using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Mep
{
    /// <summary>
    ///     Creates conduits via Conduit.Create(Document, conduitTypeId, start, end, levelId).
    /// </summary>
    public class CreateConduitEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public MepCurveRequest Request { get; set; }
        public AIResult<MepCreateResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new MepCreateResult();
            try
            {
                ElementId typeId = McpMepUtils.ResolveConduitType(doc, Request.TypeName);
                Level level = McpResolveUtils.ResolveLevel(doc, Request.Level);
                if (typeId == ElementId.InvalidElementId) { Fail("No ConduitType available."); return; }
                if (level == null) { Fail("No level available."); return; }

                using (var tx = new Transaction(doc, "Create Conduit"))
                {
                    tx.Start();
                    foreach (var seg in Request.Segments)
                    {
                        if (seg.Start == null || seg.End == null) { res.Warnings.Add("Segment missing start/end; skipped."); continue; }
                        try
                        {
                            XYZ p1 = JZPoint.ToXYZ(seg.Start);
                            XYZ p2 = JZPoint.ToXYZ(seg.End);
                            var conduit = Conduit.Create(doc, typeId, p1, p2, level.Id);
                            if (conduit != null)
                            {
                                if (seg.Diameter > 0)
                                {
                                    string w = McpMepUtils.TrySetDiameter(conduit, seg.Diameter);
                                    if (w != null) res.Warnings.Add($"Conduit {conduit.Id.GetValue()}: {w}");
                                }
                                res.CreatedIds.Add(conduit.Id.GetValue());
                            }
                        }
                        catch (Exception ex) { res.Warnings.Add($"Segment failed: {ex.Message}"); }
                    }
                    tx.Commit();
                }

                res.CreatedCount = res.CreatedIds.Count;
                Result = new AIResult<MepCreateResult>
                {
                    Success = res.CreatedCount > 0,
                    Message = $"Created {res.CreatedCount} conduit(s)" +
                              (res.Warnings.Count > 0 ? $". ⚠ {res.Warnings.Count} note(s): {res.Warnings[0]}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<MepCreateResult> { Success = false, Message = $"Error creating conduits: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }

            void Fail(string msg)
            {
                Result = new AIResult<MepCreateResult> { Success = false, Message = msg, Response = res };
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Create Conduit";
    }
}
