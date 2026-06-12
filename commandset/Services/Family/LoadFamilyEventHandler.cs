using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;
using DBFamily = Autodesk.Revit.DB.Family;

namespace RevitMCPCommandSet.Services.Family
{
    /// <summary>
    ///     Loads a family (.rfa) into the project, overwriting if requested, and returns
    ///     the family id plus the ids of its types.
    /// </summary>
    public class LoadFamilyEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public FamilyLoadRequest Request { get; set; }
        public AIResult<FamilyOpResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new FamilyOpResult();
            try
            {
                if (!System.IO.File.Exists(Request.Path))
                {
                    Result = new AIResult<FamilyOpResult> { Success = false, Message = $"File not found: {Request.Path}" };
                    _resetEvent.Set();
                    return;
                }

                DBFamily family = null;
                bool ok;
                using (var tx = new Transaction(doc, "Load Family"))
                {
                    tx.Start();
                    ok = doc.LoadFamily(Request.Path, new OverwriteFamilyLoadOptions(Request.Overwrite), out family);
                    tx.Commit();
                }

                if (!ok || family == null)
                {
                    Result = new AIResult<FamilyOpResult>
                    {
                        Success = false,
                        Message = ok ? "Family already up to date (not reloaded)." : "Failed to load family.",
                        Response = res
                    };
                    _resetEvent.Set();
                    return;
                }

                res.Ids.Add(family.Id.GetValue());
                var typeIds = family.GetFamilySymbolIds();
                foreach (var id in typeIds) res.Ids.Add(id.GetValue());
                res.Count = typeIds.Count;

                Result = new AIResult<FamilyOpResult>
                {
                    Success = true,
                    Message = $"Loaded family '{family.Name}' with {typeIds.Count} type(s). First id is the family; rest are types.",
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<FamilyOpResult> { Success = false, Message = $"Error loading family: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Load Family";
    }
}
