using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    /// <summary>
    /// Event handler for the universal parameter writer. Supports instance and
    /// type parameters, BuiltInParameter and display names, automatic storage
    /// type conversion and unit handling (mm / mm² / mm³ / degrees).
    /// </summary>
    public class SetElementParametersEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<ParameterSetRequest> Requests { get; private set; }
        public AIResult<List<ParameterSetResult>> Result { get; private set; }

        public void SetParameters(List<ParameterSetRequest> data)
        {
            Requests = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                var results = new List<ParameterSetResult>();

                using (Transaction tx = new Transaction(doc, "Set Element Parameters"))
                {
                    tx.Start();

                    foreach (var request in Requests)
                    {
                        string paramLabel = !string.IsNullOrWhiteSpace(request.BuiltInParameter)
                            ? request.BuiltInParameter
                            : request.ParameterName;

                        foreach (int id in request.ElementIds ?? new List<int>())
                        {
                            var result = new ParameterSetResult
                            {
                                ElementId = id,
                                Parameter = paramLabel
                            };

                            Element element = doc.GetElement(new ElementId(id));
                            if (element == null)
                            {
                                result.Success = false;
                                result.Message = "Element not found";
                                results.Add(result);
                                continue;
                            }

                            Parameter param = McpParameterUtils.ResolveParameter(
                                doc, element,
                                request.BuiltInParameter,
                                request.ParameterName,
                                request.IsTypeParameter,
                                request.FallbackToOtherScope);

                            if (param == null)
                            {
                                result.Success = false;
                                result.Message = $"Parameter '{paramLabel}' not found on element or its type";
                                results.Add(result);
                                continue;
                            }

                            string error = McpParameterUtils.SetParameterValue(doc, param, request.Value);
                            if (error != null)
                            {
                                result.Success = false;
                                result.Message = error;
                            }
                            else
                            {
                                result.Success = true;
                                result.Message = "OK";
                                try { result.NewValue = param.AsValueString() ?? param.AsString() ?? request.Value?.ToString(); }
                                catch { result.NewValue = request.Value?.ToString(); }
                            }

                            results.Add(result);
                        }
                    }

                    tx.Commit();
                }

                int okCount = results.Count(r => r.Success);
                int failCount = results.Count - okCount;

                Result = new AIResult<List<ParameterSetResult>>
                {
                    Success = failCount == 0 || okCount > 0,
                    Message = $"Set {okCount} parameter value(s) successfully, {failCount} failed. Details per element in Response.",
                    Response = results
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<ParameterSetResult>>
                {
                    Success = false,
                    Message = $"Error setting element parameters: {ex.Message}"
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName()
        {
            return "Set Element Parameters";
        }
    }
}
