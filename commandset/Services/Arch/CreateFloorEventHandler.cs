using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Arch
{
    /// <summary>
    ///     Creates a floor via Floor.Create(doc, IList&lt;CurveLoop&gt;, floorTypeId, levelId).
    /// </summary>
    public class CreateFloorEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
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
                ElementType floorType = McpResolveUtils.ResolveType(doc, Request.TypeName, typeof(FloorType))
                                        ?? McpResolveUtils.FirstTypeOfClass(doc, typeof(FloorType));
                Level level = McpResolveUtils.ResolveLevel(doc, Request.Level);
                if (floorType == null) { Fail("No FloorType available."); return; }
                if (level == null) { Fail("No level available."); return; }

                CurveLoop loop = McpResolveUtils.BuildCurveLoop(Request.Boundary, out string w);
                if (loop == null) { Fail($"Invalid boundary: {w}"); return; }

                using (var tx = new Transaction(doc, "Create Floor"))
                {
                    tx.Start();
                    var floor = Floor.Create(doc, new List<CurveLoop> { loop }, floorType.Id, level.Id);
                    if (floor != null) res.CreatedIds.Add(floor.Id.GetValue());
                    tx.Commit();
                }

                res.CreatedCount = res.CreatedIds.Count;
                Result = new AIResult<ArchCreateResult>
                {
                    Success = res.CreatedCount > 0,
                    Message = res.CreatedCount > 0 ? $"Created floor (id {res.CreatedIds[0]})." : "Floor creation returned nothing.",
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<ArchCreateResult> { Success = false, Message = $"Error creating floor: {ex.Message}", Response = res };
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

        public string GetName() => "Create Floor";
    }
}
