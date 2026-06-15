using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Mep;
using CEM_IAModeler_CommandSet.Services.Mep;

namespace CEM_IAModeler_CommandSet.Commands.Mep
{
    public class CreateConduitCommand : ExternalEventCommandBase
    {
        private CreateConduitEventHandler _handler => (CreateConduitEventHandler)Handler;

        public override string CommandName => "mep_create_conduit";

        public CreateConduitCommand(UIApplication uiApp)
            : base(new CreateConduitEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<MepCurveRequest>();
            if (data == null || data.Segments == null || data.Segments.Count == 0)
                throw new ArgumentException("No conduit segments provided");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(30000))
                return _handler.Result;
            throw new TimeoutException("mep_create_conduit timed out");
        }
    }
}
