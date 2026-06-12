using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Family
{
    /// <summary>
    ///     Duplicates a loaded family type (FamilySymbol.Duplicate) and applies parameter
    ///     overrides — the project-context way to make a new type. mm/deg are converted.
    /// </summary>
    public class CreateFamilyTypeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public CreateFamilyTypeRequest Request { get; set; }
        public AIResult<FamilyOpResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new FamilyOpResult();
            try
            {
                var source = McpResolveUtils.ResolveType(doc, Request.SourceTypeName) as FamilySymbol;
                if (source == null)
                {
                    Result = new AIResult<FamilyOpResult> { Success = false, Message = $"Source type '{Request.SourceTypeName}' not found." };
                    _resetEvent.Set();
                    return;
                }

                using (var tx = new Transaction(doc, "Create Family Type"))
                {
                    tx.Start();
                    var newSymbol = source.Duplicate(Request.NewTypeName) as FamilySymbol;
                    if (newSymbol != null)
                    {
                        res.Ids.Add(newSymbol.Id.GetValue());
                        foreach (var ov in Request.Parameters ?? new List<ParamOverride>())
                        {
                            Parameter p = newSymbol.LookupParameter(ov.Name);
                            if (p == null) { res.Warnings.Add($"Parameter '{ov.Name}' not found."); continue; }
                            string err = McpParameterUtils.SetParameterValue(doc, p, ov.Value);
                            if (err != null) res.Warnings.Add($"'{ov.Name}': {err}");
                        }
                    }
                    tx.Commit();
                }

                res.Count = res.Ids.Count;
                Result = new AIResult<FamilyOpResult>
                {
                    Success = res.Count > 0,
                    Message = res.Count > 0
                        ? $"Created type '{Request.NewTypeName}' (id {res.Ids[0]})" + (res.Warnings.Count > 0 ? $". ⚠ {res.Warnings.Count} param note(s): {res.Warnings[0]}" : ".")
                        : "Type duplication returned nothing.",
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<FamilyOpResult> { Success = false, Message = $"Error creating family type: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Create Family Type";
    }
}
