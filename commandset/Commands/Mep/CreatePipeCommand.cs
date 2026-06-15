using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Mep;
using CEM_IAModeler_CommandSet.Services.Mep;

namespace CEM_IAModeler_CommandSet.Commands.Mep
{
    public class CreatePipeCommand : ExternalEventCommandBase
    {
        private CreatePipeEventHandler _handler => (CreatePipeEventHandler)Handler;

        public override string CommandName => "mep_create_pipe";

        public CreatePipeCommand(UIApplication uiApp)
            : base(new CreatePipeEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<MepCurveRequest>();
            if (data == null || data.Segments == null || data.Segments.Count == 0)
                throw new ArgumentException("No pipe segments provided");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(30000))
                return _handler.Result;
            throw new TimeoutException("mep_create_pipe timed out");
        }
    }
}
