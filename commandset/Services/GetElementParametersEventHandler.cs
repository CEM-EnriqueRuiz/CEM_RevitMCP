using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    /// <summary>
    ///     Reads instance (and optionally type) parameters from elements. Read-only,
    ///     no transaction needed.
    /// </summary>
    public class GetElementParametersEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public GetParametersRequest Request { get; set; }
        public AIResult<List<ElementParametersInfo>> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            try
            {
                var output = new List<ElementParametersInfo>();
                var wanted = (Request.ParameterNames ?? new List<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (long id in Request.ElementIds)
                {
                    var info = new ElementParametersInfo { ElementId = id };
                    Element el = McpResolveUtils.GetElement(doc, id);
                    if (el == null)
                    {
                        info.Found = false;
                        output.Add(info);
                        continue;
                    }

                    info.Found = true;
                    info.Name = el.Name;
                    info.Category = el.Category?.Name;

                    ElementId typeId = el.GetTypeId();
                    Element typeEl = (typeId != null && typeId != ElementId.InvalidElementId)
                        ? doc.GetElement(typeId) : null;
                    info.TypeName = typeEl?.Name;

                    CollectParameters(el, false, wanted, info);
                    if (Request.IncludeType && typeEl != null)
                        CollectParameters(typeEl, true, wanted, info);

                    output.Add(info);
                }

                int found = output.Count(o => o.Found);
                Result = new AIResult<List<ElementParametersInfo>>
                {
                    Success = true,
                    Message = $"Read parameters from {found}/{output.Count} element(s).",
                    Response = output
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<ElementParametersInfo>>
                    { Success = false, Message = $"Error reading parameters: {ex.Message}" };
            }
            finally { _resetEvent.Set(); }
        }

        private void CollectParameters(Element el, bool isType,
            HashSet<string> wanted, ElementParametersInfo info)
        {
            foreach (Parameter p in el.Parameters)
            {
                if (p?.Definition == null) continue;
                string name = p.Definition.Name;

                if (wanted.Count > 0 && !wanted.Contains(name)) continue;

                string value = ReadValue(p);
                if (!Request.IncludeEmpty && string.IsNullOrEmpty(value)) continue;

                string builtIn = null;
                if (p.Definition is InternalDefinition idef && idef.BuiltInParameter != BuiltInParameter.INVALID)
                    builtIn = idef.BuiltInParameter.ToString();

                info.Parameters.Add(new ParameterValueInfo
                {
                    Name = name,
                    Value = value,
                    StorageType = p.StorageType.ToString(),
                    IsReadOnly = p.IsReadOnly,
                    IsType = isType,
                    BuiltIn = builtIn
                });
            }
        }

        private static string ReadValue(Parameter p)
        {
            try
            {
                if (!p.HasValue) return string.Empty;
                switch (p.StorageType)
                {
                    case StorageType.String: return p.AsString() ?? string.Empty;
                    case StorageType.Integer: return p.AsValueString() ?? p.AsInteger().ToString();
                    case StorageType.Double: return p.AsValueString() ?? p.AsDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);
                    case StorageType.ElementId:
                        var eid = p.AsElementId();
                        return eid == null ? string.Empty : eid.GetValue().ToString();
                    default: return p.AsValueString() ?? string.Empty;
                }
            }
            catch { return string.Empty; }
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Get Element Parameters";
    }
}
