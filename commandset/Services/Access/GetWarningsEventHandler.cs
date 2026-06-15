using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Access
{
    public class WarningGroupInfo
    {
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("count")] public int Count { get; set; }
        [JsonProperty("severity")] public string Severity { get; set; }
        [JsonProperty("sampleElementIds")] public List<long> SampleElementIds { get; set; } = new();
    }

    public class WarningsSummary
    {
        [JsonProperty("totalWarnings")] public int TotalWarnings { get; set; }
        [JsonProperty("groupCount")] public int GroupCount { get; set; }
        [JsonProperty("groups")] public List<WarningGroupInfo> Groups { get; set; } = new();
    }

    /// <summary>
    ///     Reads Document.GetWarnings() and returns them grouped by description with counts
    ///     and a sample of affected element ids — the QA backbone for any persona.
    /// </summary>
    public class GetWarningsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public int MaxElementsPerWarning { get; set; } = 20;
        public AIResult<WarningsSummary> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            try
            {
                IList<FailureMessage> warnings = doc.GetWarnings();
                var grouped = new Dictionary<string, WarningGroupInfo>(StringComparer.OrdinalIgnoreCase);

                foreach (var w in warnings)
                {
                    string desc = w.GetDescriptionText();
                    if (!grouped.TryGetValue(desc, out var g))
                    {
                        g = new WarningGroupInfo
                        {
                            Description = desc,
                            Severity = w.GetSeverity().ToString()
                        };
                        grouped[desc] = g;
                    }
                    g.Count++;
                    if (g.SampleElementIds.Count < MaxElementsPerWarning)
                    {
                        foreach (var id in w.GetFailingElements())
                        {
                            if (g.SampleElementIds.Count >= MaxElementsPerWarning) break;
                            g.SampleElementIds.Add(id.GetValue());
                        }
                    }
                }

                var summary = new WarningsSummary
                {
                    TotalWarnings = warnings.Count,
                    GroupCount = grouped.Count,
                    Groups = grouped.Values.OrderByDescending(g => g.Count).ToList()
                };

                Result = new AIResult<WarningsSummary>
                {
                    Success = true,
                    Message = $"{warnings.Count} warning(s) in {grouped.Count} group(s).",
                    Response = summary
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<WarningsSummary>
                    { Success = false, Message = $"Error reading warnings: {ex.Message}" };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Get Warnings";
    }
}
