using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Architecture;
using CEM_IAModeler_CommandSet.Services.Architecture;

namespace CEM_IAModeler_CommandSet.Commands.Architecture
{
    public class CreateWallCommand : ExternalEventCommandBase
    {
        private CreateWallEventHandler _handler => (CreateWallEventHandler)Handler;

        public override string CommandName => "arch_create_wall";

        public CreateWallCommand(UIApplication uiApp)
            : base(new CreateWallEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<CreateWallRequest>();
            if (data == null || data.Segments == null || data.Segments.Count == 0)
                throw new ArgumentException("No wall segments provided");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(30000))
                return _handler.Result;
            throw new TimeoutException("arch_create_wall timed out");
        }
    }
}
