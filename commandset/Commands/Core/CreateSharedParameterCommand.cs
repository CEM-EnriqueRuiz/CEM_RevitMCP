using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Services.Core;
using RevitMCPSDK.API.Base;

namespace CEM_IAModeler_CommandSet.Commands.Core
{
    public class CreateSharedParameterCommand : ExternalEventCommandBase
    {
        private CreateSharedParameterEventHandler _handler => (CreateSharedParameterEventHandler)Handler;

        /// <summary>
        /// Command name for MCP protocol
        /// </summary>
        public override string CommandName => "create_shared_parameter";

        public CreateSharedParameterCommand(UIApplication uiApp)
            : base(new CreateSharedParameterEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<SharedParameterCreationInfo> data =
                    parameters["data"]?.ToObject<List<SharedParameterCreationInfo>>();
                if (data == null || data.Count == 0)
                    throw new ArgumentNullException(nameof(data), "No shared parameter data provided");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(20000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create shared parameter operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create shared parameter: {ex.Message}");
            }
        }
    }
}
