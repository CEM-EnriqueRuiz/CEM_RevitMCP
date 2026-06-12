using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands
{
    public class CreateReferencePlaneCommand : ExternalEventCommandBase
    {
        private CreateReferencePlaneEventHandler _handler => (CreateReferencePlaneEventHandler)Handler;

        /// <summary>
        /// Command name for MCP protocol
        /// </summary>
        public override string CommandName => "create_reference_plane";

        public CreateReferencePlaneCommand(UIApplication uiApp)
            : base(new CreateReferencePlaneEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<ReferencePlaneData> data = parameters["data"]?.ToObject<List<ReferencePlaneData>>();
                if (data == null || data.Count == 0)
                    throw new ArgumentNullException(nameof(data), "No reference plane data provided");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create reference plane operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create reference plane: {ex.Message}");
            }
        }
    }
}
