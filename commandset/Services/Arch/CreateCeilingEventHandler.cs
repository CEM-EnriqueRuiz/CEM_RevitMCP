using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Arch
{
    /// <summary>
    ///     Creates a ceiling via Ceiling.Create (Revit 2022+). On 2020/2021 the API is
    ///     unavailable, so the handler reports that clearly instead of failing silently.
    /// </summary>
    public class CreateCeilingEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public CreateSlabRequest Request { get; set; }
        public AIResult<ArchCreateResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new ArchCreateResult();
            try
            {
#if REVIT2022_OR_GREATER
                ElementType ceilingType = McpResolveUtils.ResolveType(doc, Request.TypeName, typeof(CeilingType))
                                          ?? McpResolveUtils.FirstTypeOfClass(doc, typeof(CeilingType));
                Level level = McpResolveUtils.ResolveLevel(doc, Request.Level);
                if (ceilingType == null) { Fail("No CeilingType available."); return; }
                if (level == null) { Fail("No level available."); return; }

                CurveLoop loop = McpResolveUtils.BuildCurveLoop(Request.Boundary, out string w);
                if (loop == null) { Fail($"Invalid boundary: {w}"); return; }

                using (var tx = new Transaction(doc, "Create Ceiling"))
                {
                    tx.Start();
                    var ceiling = Ceiling.Create(doc, new List<CurveLoop> { loop }, ceilingType.Id, level.Id);
                    if (ceiling != null) res.CreatedIds.Add(ceiling.Id.GetValue());
                    tx.Commit();
                }

                res.CreatedCount = res.CreatedIds.Count;
                Result = new AIResult<ArchCreateResult>
                {
                    Success = res.CreatedCount > 0,
                    Message = res.CreatedCount > 0 ? $"Created ceiling (id {res.CreatedIds[0]})." : "Ceiling creation returned nothing.",
                    Response = res
                };
#else
                Result = new AIResult<ArchCreateResult>
                {
                    Success = false,
                    Message = "Ceiling.Create requires Revit 2022 or newer; not available in this version.",
                    Response = res
                };
#endif
            }
            catch (Exception ex)
            {
                Result = new AIResult<ArchCreateResult> { Success = false, Message = $"Error creating ceiling: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }

#if REVIT2022_OR_GREATER
            void Fail(string msg)
            {
                Result = new AIResult<ArchCreateResult> { Success = false, Message = msg, Response = res };
                _resetEvent.Set();
            }
#endif
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Create Ceiling";
    }
}
