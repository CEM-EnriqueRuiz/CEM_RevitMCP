using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Models.Mep;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Mep
{
    /// <summary>
    ///     Creates cable trays via CableTray.Create(Document, cableTrayTypeId, start, end, levelId).
    /// </summary>
    public class CreateCableTrayEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
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
                ElementId typeId = McpMepUtils.ResolveCableTrayType(doc, Request.TypeName);
                Level level = McpResolveUtils.ResolveLevel(doc, Request.Level);
                if (typeId == ElementId.InvalidElementId) { Fail("No CableTrayType available."); return; }
                if (level == null) { Fail("No level available."); return; }

                using (var tx = new Transaction(doc, "Create Cable Tray"))
                {
                    tx.Start();
                    foreach (var seg in Request.Segments)
                    {
                        if (seg.Start == null || seg.End == null) { res.Warnings.Add("Segment missing start/end; skipped."); continue; }
                        try
                        {
                            XYZ p1 = JZPoint.ToXYZ(seg.Start);
                            XYZ p2 = JZPoint.ToXYZ(seg.End);
                            var tray = CableTray.Create(doc, typeId, p1, p2, level.Id);
                            if (tray != null)
                            {
                                string w = McpMepUtils.TrySetWidthHeight(tray, seg.Width, seg.Height);
                                if (w != null) res.Warnings.Add($"CableTray {tray.Id.GetValue()}: {w}");
                                res.CreatedIds.Add(tray.Id.GetValue());
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
                    Message = $"Created {res.CreatedCount} cable tray(s)" +
                              (res.Warnings.Count > 0 ? $". ⚠ {res.Warnings.Count} note(s): {res.Warnings[0]}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<MepCreateResult> { Success = false, Message = $"Error creating cable trays: {ex.Message}", Response = res };
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

        public string GetName() => "Create Cable Tray";
    }
}
