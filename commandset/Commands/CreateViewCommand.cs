using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Services;

namespace RevitMCPCommandSet.Commands
{
    /// <summary>
    ///     Generic view creation: floor/ceiling plan, section, elevation, 3D, drafting.
    /// </summary>
    public class CreateViewCommand : ExternalEventCommandBase
    {
        private CreateViewEventHandler _handler => (CreateViewEventHandler)Handler;

        public override string CommandName => "create_view";

        public CreateViewCommand(UIApplication uiApp)
            : base(new CreateViewEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<ViewCreationRequest>();
            if (data == null || string.IsNullOrWhiteSpace(data.ViewType))
                throw new ArgumentException("viewType is required");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(20000))
                return _handler.Result;
            throw new TimeoutException("create_view timed out");
        }
    }
}
