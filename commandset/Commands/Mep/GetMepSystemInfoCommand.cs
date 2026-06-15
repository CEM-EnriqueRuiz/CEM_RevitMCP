using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Mep;
using CEM_IAModeler_CommandSet.Services.Mep;

namespace CEM_IAModeler_CommandSet.Commands.Mep
{
    public class GetMepSystemInfoCommand : ExternalEventCommandBase
    {
        private GetMepSystemInfoEventHandler _handler => (GetMepSystemInfoEventHandler)Handler;

        public override string CommandName => "mep_get_system_info";

        public GetMepSystemInfoCommand(UIApplication uiApp)
            : base(new GetMepSystemInfoEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<MepSystemQuery>();
            if (data == null || data.ElementId == 0)
                throw new ArgumentException("elementId is required");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(20000))
                return _handler.Result;
            throw new TimeoutException("mep_get_system_info timed out");
        }
    }
}
