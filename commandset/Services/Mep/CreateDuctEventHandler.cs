using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Models.Mep;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Mep
{
    /// <summary>
    ///     Creates ducts via Duct.Create(Document, systemTypeId, ductTypeId, levelId, start, end).
    /// </summary>
    public class CreateDuctEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
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
                ElementId systemTypeId = McpMepUtils.ResolveMechanicalSystemType(doc, Request.SystemTypeName);
                ElementId ductTypeId = McpMepUtils.ResolveDuctType(doc, Request.TypeName);
                Level level = McpResolveUtils.ResolveLevel(doc, Request.Level);

                if (systemTypeId == ElementId.InvalidElementId) { Fail("No MechanicalSystemType available in the document."); return; }
                if (ductTypeId == ElementId.InvalidElementId) { Fail("No DuctType available in the document."); return; }
                if (level == null) { Fail("No level available."); return; }

                using (var tx = new Transaction(doc, "Create Duct"))
                {
                    tx.Start();
                    foreach (var seg in Request.Segments)
                    {
                        if (seg.Start == null || seg.End == null) { res.Warnings.Add("Segment missing start/end; skipped."); continue; }
                        try
                        {
                            XYZ p1 = JZPoint.ToXYZ(seg.Start);
                            XYZ p2 = JZPoint.ToXYZ(seg.End);
                            var duct = Duct.Create(doc, systemTypeId, ductTypeId, level.Id, p1, p2);
                            if (duct != null)
                            {
                                string w = seg.Diameter > 0
                                    ? McpMepUtils.TrySetDiameter(duct, seg.Diameter)
                                    : McpMepUtils.TrySetWidthHeight(duct, seg.Width, seg.Height);
                                if (w != null) res.Warnings.Add($"Duct {duct.Id.GetValue()}: {w}");
                                res.CreatedIds.Add(duct.Id.GetValue());
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
                    Message = $"Created {res.CreatedCount} duct(s)" +
                              (res.Warnings.Count > 0 ? $". ⚠ {res.Warnings.Count} note(s): {res.Warnings[0]}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<MepCreateResult> { Success = false, Message = $"Error creating ducts: {ex.Message}", Response = res };
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

        public string GetName() => "Create Duct";
    }
}
