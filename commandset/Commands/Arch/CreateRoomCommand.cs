using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Services.Arch;

namespace RevitMCPCommandSet.Commands.Arch
{
    public class CreateRoomCommand : ExternalEventCommandBase
    {
        private CreateRoomEventHandler _handler => (CreateRoomEventHandler)Handler;

        public override string CommandName => "arch_create_room";

        public CreateRoomCommand(UIApplication uiApp)
            : base(new CreateRoomEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<CreateRoomRequest>() ?? new CreateRoomRequest();
            _handler.Request = data;

            if (RaiseAndWaitForCompletion(30000))
                return _handler.Result;
            throw new TimeoutException("arch_create_room timed out");
        }
    }
}
