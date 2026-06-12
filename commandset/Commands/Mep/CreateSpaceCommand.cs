using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPCommandSet.Services.Mep;

namespace RevitMCPCommandSet.Commands.Mep
{
    public class CreateSpaceCommand : ExternalEventCommandBase
    {
        private CreateSpaceEventHandler _handler => (CreateSpaceEventHandler)Handler;

        public override string CommandName => "mep_create_space";

        public CreateSpaceCommand(UIApplication uiApp)
            : base(new CreateSpaceEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<MepSpaceRequest>() ?? new MepSpaceRequest();
            _handler.Request = data;

            if (RaiseAndWaitForCompletion(30000))
                return _handler.Result;
            throw new TimeoutException("mep_create_space timed out");
        }
    }
}
