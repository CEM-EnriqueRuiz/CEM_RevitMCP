using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    /// <summary>
    /// Event handler for creating global parameters, setting their values and
    /// associating them to element parameters (driving multiple parameters
    /// from a single project-wide value).
    /// </summary>
    public class CreateGlobalParameterEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<GlobalParameterCreationInfo> CreatedInfo { get; private set; }
        public AIResult<List<GlobalParameterResultInfo>> Result { get; private set; }

        public void SetParameters(List<GlobalParameterCreationInfo> data)
        {
            CreatedInfo = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                if (!GlobalParametersManager.AreGlobalParametersAllowed(doc))
                    throw new Exception("Global parameters are not allowed in this document (e.g. family documents)");

                var results = new List<GlobalParameterResultInfo>();
                var warnings = new List<string>();

                using (Transaction tx = new Transaction(doc, "Create Global Parameters"))
                {
                    tx.Start();

                    foreach (var info in CreatedInfo)
                    {
                        if (string.IsNullOrWhiteSpace(info.Name))
                        {
                            warnings.Add("Skipped a global parameter with empty name");
                            continue;
                        }

                        try
                        {
                            var resultInfo = new GlobalParameterResultInfo { Name = info.Name };

                            // 1. Reuse or create
                            ElementId existingId = GlobalParametersManager.FindByName(doc, info.Name);
                            GlobalParameter gp;

                            if (existingId != ElementId.InvalidElementId)
                            {
                                gp = doc.GetElement(existingId) as GlobalParameter;
                                resultInfo.AlreadyExisted = true;
                            }
                            else
                            {
#if REVIT2022_OR_GREATER
                                gp = GlobalParameter.Create(doc, info.Name, McpParameterUtils.GetSpecTypeId(info.DataType));
#else
                                gp = GlobalParameter.Create(doc, info.Name, McpParameterUtils.GetParameterType(info.DataType));
#endif
                            }

                            if (gp == null)
                            {
                                warnings.Add($"Could not create or find global parameter '{info.Name}'");
                                continue;
                            }

                            resultInfo.Id = gp.Id.GetIntValue();

                            // 2. Reporting flag
                            if (info.IsReporting && !gp.IsReporting)
                            {
                                try { gp.IsReporting = true; }
                                catch (Exception exRep) { warnings.Add($"'{info.Name}': could not set reporting ({exRep.Message})"); }
                            }

                            // 3. Set value
                            if (info.Value != null && !info.IsReporting)
                            {
                                ParameterValue paramValue = BuildParameterValue(info);
                                if (paramValue != null)
                                {
                                    try { gp.SetValue(paramValue); }
                                    catch (Exception exVal)
                                    {
                                        warnings.Add($"'{info.Name}': could not set value ({exVal.Message})");
                                    }
                                }
                            }

                            // 4. Associate element parameters
                            foreach (var assoc in info.Associations ?? new List<ParameterAssociationInfo>())
                            {
                                Element element = doc.GetElement(new ElementId(assoc.ElementId));
                                if (element == null)
                                {
                                    resultInfo.AssociationErrors.Add($"Element {assoc.ElementId} not found");
                                    continue;
                                }

                                Parameter param = McpParameterUtils.ResolveParameter(
                                    doc, element,
                                    assoc.BuiltInParameter,
                                    assoc.ParameterName,
                                    preferType: false,
                                    fallbackToOtherScope: true);

                                if (param == null)
                                {
                                    resultInfo.AssociationErrors.Add(
                                        $"Parameter '{assoc.BuiltInParameter}{assoc.ParameterName}' not found on element {assoc.ElementId}");
                                    continue;
                                }

                                try
                                {
                                    if (!param.CanBeAssociatedWithGlobalParameter(gp.Id))
                                    {
                                        resultInfo.AssociationErrors.Add(
                                            $"Parameter '{param.Definition.Name}' on element {assoc.ElementId} cannot be associated (type mismatch or unsupported)");
                                        continue;
                                    }

                                    param.AssociateWithGlobalParameter(gp.Id);
                                    resultInfo.AssociatedCount++;
                                }
                                catch (Exception exAssoc)
                                {
                                    resultInfo.AssociationErrors.Add(
                                        $"Element {assoc.ElementId}: {exAssoc.Message}");
                                }
                            }

                            results.Add(resultInfo);
                        }
                        catch (Exception exItem)
                        {
                            warnings.Add($"Error processing '{info.Name}': {exItem.Message}");
                        }
                    }

                    tx.Commit();
                }

                int totalAssociations = results.Sum(r => r.AssociatedCount);
                string message = $"Processed {results.Count} global parameter(s), {totalAssociations} parameter association(s) created.";
                if (warnings.Count > 0)
                    message += "\n\n⚠ Warnings:\n  • " + string.Join("\n  • ", warnings);

                Result = new AIResult<List<GlobalParameterResultInfo>>
                {
                    Success = true,
                    Message = message,
                    Response = results
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<GlobalParameterResultInfo>>
                {
                    Success = false,
                    Message = $"Error creating global parameters: {ex.Message}"
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        /// <summary>
        /// Converts the friendly value into a strongly-typed ParameterValue
        /// </summary>
        private ParameterValue BuildParameterValue(GlobalParameterCreationInfo info)
        {
            string dt = (info.DataType ?? "Text").Trim().ToLowerInvariant();

            switch (dt)
            {
                case "integer":
                    return new IntegerParameterValue(
                        Convert.ToInt32(info.Value, System.Globalization.CultureInfo.InvariantCulture));

                case "yesno":
                case "boolean":
                    bool bv = info.Value is bool b
                        ? b
                        : bool.TryParse(info.Value?.ToString(), out bool parsed) && parsed;
                    return new IntegerParameterValue(bv ? 1 : 0);

                case "text":
                case "url":
                    return new StringParameterValue(info.Value?.ToString() ?? string.Empty);

                case "number":
                case "length":
                case "area":
                case "volume":
                case "angle":
                    double raw = Convert.ToDouble(info.Value, System.Globalization.CultureInfo.InvariantCulture);
                    return new DoubleParameterValue(McpParameterUtils.ToInternalUnits(raw, info.DataType));

                default:
                    return new StringParameterValue(info.Value?.ToString() ?? string.Empty);
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName()
        {
            return "Create Global Parameter";
        }
    }
}
