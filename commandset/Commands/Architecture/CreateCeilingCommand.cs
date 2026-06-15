using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Architecture;
using CEM_IAModeler_CommandSet.Services.Architecture;

namespace CEM_IAModeler_CommandSet.Commands.Architecture
{
    public class CreateCeilingCommand : ExternalEventCommandBase
    {
        private CreateCeilingEventHandler _handler => (CreateCeilingEventHandler)Handler;

        public override string CommandName => "arch_create_ceiling";

        public CreateCeilingCommand(UIApplication uiApp)
            : base(new CreateCeilingEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<CreateSlabRequest>();
            if (data == null || data.Boundary == null || data.Boundary.Count < 3)
                throw new ArgumentException("A ceiling needs a boundary of at least 3 points");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(30000))
                return _handler.Result;
            throw new TimeoutException("arch_create_ceiling timed out");
        }
    }
}
