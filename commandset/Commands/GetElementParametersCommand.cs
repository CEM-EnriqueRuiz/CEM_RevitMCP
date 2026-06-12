using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Services;

namespace RevitMCPCommandSet.Commands
{
    /// <summary>
    ///     Reads parameters from elements (read-back companion to set_element_parameters).
    /// </summary>
    public class GetElementParametersCommand : ExternalEventCommandBase
    {
        private GetElementParametersEventHandler _handler => (GetElementParametersEventHandler)Handler;

        public override string CommandName => "get_element_parameters";

        public GetElementParametersCommand(UIApplication uiApp)
            : base(new GetElementParametersEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<GetParametersRequest>() ?? new GetParametersRequest();
            if (data.ElementIds == null || data.ElementIds.Count == 0)
                throw new ArgumentException("No elementIds provided");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(15000))
                return _handler.Result;
            throw new TimeoutException("get_element_parameters timed out");
        }
    }
}
