using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Services.Core;
using RevitMCPSDK.API.Base;

namespace CEM_IAModeler_CommandSet.Commands.Core
{
    public class CreateGlobalParameterCommand : ExternalEventCommandBase
    {
        private CreateGlobalParameterEventHandler _handler => (CreateGlobalParameterEventHandler)Handler;

        /// <summary>
        /// Command name for MCP protocol
        /// </summary>
        public override string CommandName => "create_global_parameter";

        public CreateGlobalParameterCommand(UIApplication uiApp)
            : base(new CreateGlobalParameterEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<GlobalParameterCreationInfo> data =
                    parameters["data"]?.ToObject<List<GlobalParameterCreationInfo>>();
                if (data == null || data.Count == 0)
                    throw new ArgumentNullException(nameof(data), "No global parameter data provided");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(20000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create global parameter operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create global parameter: {ex.Message}");
            }
        }
    }
}
