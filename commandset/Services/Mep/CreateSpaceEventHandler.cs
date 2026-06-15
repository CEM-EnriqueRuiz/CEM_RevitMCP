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
    ///     Creates MEP spaces. If points are given, places a space at each (mm) on the level;
    ///     otherwise auto-places spaces in all enclosed regions on the level.
    /// </summary>
    public class CreateSpaceEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public MepSpaceRequest Request { get; set; }
        public AIResult<MepInfoResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new MepInfoResult();
            try
            {
                Level level = McpResolveUtils.ResolveLevel(doc, Request.Level);
                if (level == null) { Fail("No level available."); return; }

                using (var tx = new Transaction(doc, "Create Space"))
                {
                    tx.Start();
                    if (Request.Points != null && Request.Points.Count > 0)
                    {
                        foreach (var jp in Request.Points)
                        {
                            try
                            {
                                var uv = new UV(McpResolveUtils.MmToFt(jp.X), McpResolveUtils.MmToFt(jp.Y));
                                Space sp = doc.Create.NewSpace(level, uv);
                                if (sp != null)
                                {
                                    res.CreatedIds.Add(sp.Id.GetValue());
                                    if (Request.Tag) TryTag(sp);
                                }
                            }
                            catch (Exception ex) { res.Warnings.Add($"Point failed: {ex.Message}"); }
                        }
                    }
                    else
                    {
                        try
                        {
                            // Auto-create spaces in all enclosed regions on the level, last phase, active view.
                            Phase phase = doc.Phases.Size > 0 ? doc.Phases.get_Item(doc.Phases.Size - 1) : null;
                            if (phase == null) { res.Warnings.Add("No phase available for auto-placement."); }
                            else
                            {
                                ICollection<ElementId> created = doc.Create.NewSpaces2(level, phase, doc.ActiveView);
                                foreach (var id in created)
                                {
                                    res.CreatedIds.Add(id.GetValue());
                                    if (Request.Tag && doc.GetElement(id) is Space sp) TryTag(sp);
                                }
                            }
                        }
                        catch (Exception ex) { res.Warnings.Add($"Auto-place failed: {ex.Message}"); }
                    }
                    tx.Commit();
                }

                res.Count = res.CreatedIds.Count;
                Result = new AIResult<MepInfoResult>
                {
                    Success = res.Count > 0,
                    Message = $"Created {res.Count} space(s)" +
                              (res.Warnings.Count > 0 ? $". ⚠ {res.Warnings.Count} note(s): {res.Warnings[0]}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<MepInfoResult> { Success = false, Message = $"Error creating spaces: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }

            void Fail(string msg)
            {
                Result = new AIResult<MepInfoResult> { Success = false, Message = msg, Response = res };
                _resetEvent.Set();
            }
        }

        private void TryTag(Space sp)
        {
            try
            {
                var loc = (sp.Location as LocationPoint)?.Point;
                if (loc == null) return;
                var uv = new UV(loc.X, loc.Y);
#if REVIT2022_OR_GREATER
                doc.Create.NewSpaceTag(sp, uv, doc.ActiveView);
#else
                doc.Create.NewSpaceTag(sp, uv, doc.ActiveView);
#endif
            }
            catch { /* tagging is best-effort */ }
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Create Space";
    }
}
