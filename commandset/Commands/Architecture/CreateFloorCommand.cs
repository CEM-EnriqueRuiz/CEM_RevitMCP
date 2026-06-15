using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Architecture;
using CEM_IAModeler_CommandSet.Services.Architecture;

namespace CEM_IAModeler_CommandSet.Commands.Architecture
{
    public class CreateFloorCommand : ExternalEventCommandBase
    {
        private CreateFloorEventHandler _handler => (CreateFloorEventHandler)Handler;

        public override string CommandName => "arch_create_floor";

        public CreateFloorCommand(UIApplication uiApp)
            : base(new CreateFloorEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<CreateSlabRequest>();
            if (data == null || data.Boundary == null || data.Boundary.Count < 3)
                throw new ArgumentException("A floor needs a boundary of at least 3 points");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(30000))
                return _handler.Result;
            throw new TimeoutException("arch_create_floor timed out");
        }
    }
}
