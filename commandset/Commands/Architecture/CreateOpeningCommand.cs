using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Architecture;
using CEM_IAModeler_CommandSet.Services.Architecture;

namespace CEM_IAModeler_CommandSet.Commands.Architecture
{
    public class CreateOpeningCommand : ExternalEventCommandBase
    {
        private CreateOpeningEventHandler _handler => (CreateOpeningEventHandler)Handler;

        public override string CommandName => "arch_create_opening";

        public CreateOpeningCommand(UIApplication uiApp)
            : base(new CreateOpeningEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<CreateOpeningRequest>();
            if (data == null || data.HostId == 0 || data.Boundary == null || data.Boundary.Count < 2)
                throw new ArgumentException("hostId and a boundary (>=2 points) are required");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(30000))
                return _handler.Result;
            throw new TimeoutException("arch_create_opening timed out");
        }
    }
}
