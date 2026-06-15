using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Models.Family;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Family
{
    /// <summary>
    ///     Lists loaded family types (FamilySymbols), optionally filtered by category and
    ///     family name, with optional parameter listings. Read-only.
    /// </summary>
    public class GetFamilyTypesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public GetFamilyTypesRequest Request { get; set; }
        public AIResult<List<FamilyTypeSummary>> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            try
            {
                BuiltInCategory? bicFilter = null;
                if (!string.IsNullOrWhiteSpace(Request.Category) &&
                    McpResolveUtils.TryResolveBuiltInCategory(Request.Category, out var bic))
                    bicFilter = bic;

                var collector = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol));
                if (bicFilter.HasValue) collector = collector.OfCategory(bicFilter.Value);

                var output = new List<FamilyTypeSummary>();
                foreach (FamilySymbol fs in collector.Cast<FamilySymbol>())
                {
                    if (!string.IsNullOrWhiteSpace(Request.FamilyName) &&
                        fs.FamilyName.IndexOf(Request.FamilyName, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    var s = new FamilyTypeSummary
                    {
                        TypeId = fs.Id.GetValue(),
                        FamilyName = fs.FamilyName,
                        TypeName = fs.Name,
                        Category = fs.Category?.Name
                    };
                    if (Request.IncludeParameters)
                    {
                        foreach (Parameter p in fs.Parameters)
                            if (p?.Definition != null) s.Parameters.Add(p.Definition.Name);
                    }
                    output.Add(s);
                }

                Result = new AIResult<List<FamilyTypeSummary>>
                {
                    Success = true,
                    Message = $"Found {output.Count} family type(s).",
                    Response = output
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<FamilyTypeSummary>> { Success = false, Message = $"Error listing family types: {ex.Message}" };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Get Family Types";
    }
}
