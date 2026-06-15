using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Services.Core;
using RevitMCPSDK.API.Base;

namespace CEM_IAModeler_CommandSet.Commands.Core
{
    public class SetElementParametersCommand : ExternalEventCommandBase
    {
        private SetElementParametersEventHandler _handler => (SetElementParametersEventHandler)Handler;

        /// <summary>
        /// Command name for MCP protocol
        /// </summary>
        public override string CommandName => "set_element_parameters";

        public SetElementParametersCommand(UIApplication uiApp)
            : base(new SetElementParametersEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<ParameterSetRequest> data = parameters["data"]?.ToObject<List<ParameterSetRequest>>();
                if (data == null || data.Count == 0)
                    throw new ArgumentNullException(nameof(data), "No parameter set requests provided");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(30000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Set element parameters operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set element parameters: {ex.Message}");
            }
        }
    }
}
