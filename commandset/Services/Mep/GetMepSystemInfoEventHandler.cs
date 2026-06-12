using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Mep
{
    /// <summary>
    ///     Reads the MEP system that an element belongs to (or that an element id IS),
    ///     returning its element membership and flow for review. Read-only.
    /// </summary>
    public class GetMepSystemInfoEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public MepSystemQuery Request { get; set; }
        public AIResult<MepSystemInfo> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            try
            {
                Element el = McpResolveUtils.GetElement(doc, Request.ElementId);
                if (el == null)
                {
                    Result = new AIResult<MepSystemInfo> { Success = false, Message = $"Element {Request.ElementId} not found." };
                    _resetEvent.Set();
                    return;
                }

                MEPSystem system = el as MEPSystem;
                if (system == null)
                {
                    // The element belongs to a system: find via its connectors
                    system = FindSystemOf(el);
                }

                if (system == null)
                {
                    Result = new AIResult<MepSystemInfo> { Success = false, Message = $"No MEP system found for element {Request.ElementId}." };
                    _resetEvent.Set();
                    return;
                }

                var info = new MepSystemInfo
                {
                    SystemId = system.Id.GetValue(),
                    SystemName = system.Name,
                    SystemType = system.GetType().Name
                };

                try
                {
                    foreach (Element e in system.Elements)
                        info.ElementIds.Add(e.Id.GetValue());
                }
                catch { }
                info.ElementCount = info.ElementIds.Count;

                try
                {
                    Parameter flow = system.get_Parameter(BuiltInParameter.RBS_DUCT_FLOW_PARAM)
                                  ?? system.LookupParameter("Flow");
                    info.Flow = flow?.AsValueString();
                }
                catch { }

                Result = new AIResult<MepSystemInfo>
                {
                    Success = true,
                    Message = $"System '{info.SystemName}' ({info.SystemType}) has {info.ElementCount} element(s).",
                    Response = info
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<MepSystemInfo> { Success = false, Message = $"Error reading system: {ex.Message}" };
            }
            finally { _resetEvent.Set(); }
        }

        private MEPSystem FindSystemOf(Element el)
        {
            try
            {
                ConnectorManager cm = null;
                if (el is MEPCurve curve) cm = curve.ConnectorManager;
                else if (el is FamilyInstance fi) cm = fi.MEPModel?.ConnectorManager;

                if (cm != null)
                {
                    foreach (Connector c in cm.Connectors)
                    {
                        if (c.MEPSystem != null) return c.MEPSystem;
                    }
                }
            }
            catch { }
            return null;
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Get MEP System Info";
    }
}
