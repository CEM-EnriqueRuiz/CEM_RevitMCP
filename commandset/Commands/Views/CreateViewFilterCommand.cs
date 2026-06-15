using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Services.Views;
using RevitMCPSDK.API.Base;

namespace CEM_IAModeler_CommandSet.Commands.Views
{
    public class CreateViewFilterCommand : ExternalEventCommandBase
    {
        private CreateViewFilterEventHandler _handler => (CreateViewFilterEventHandler)Handler;

        /// <summary>
        /// Command name for MCP protocol
        /// </summary>
        public override string CommandName => "create_view_filter";

        public CreateViewFilterCommand(UIApplication uiApp)
            : base(new CreateViewFilterEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                ViewFilterCreationInfo data = parameters["data"]?.ToObject<ViewFilterCreationInfo>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "No view filter data provided");
                if (string.IsNullOrWhiteSpace(data.Name))
                    throw new ArgumentException("Filter name is required");
                if (data.Categories == null || data.Categories.Count == 0)
                    throw new ArgumentException("At least one category is required (e.g. OST_Walls)");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(20000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create view filter operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create view filter: {ex.Message}");
            }
        }
    }
}
