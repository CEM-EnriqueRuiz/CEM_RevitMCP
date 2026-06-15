using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Family;
using CEM_IAModeler_CommandSet.Services.Family;

namespace CEM_IAModeler_CommandSet.Commands.Family
{
    public class GetFamilyTypesCommand : ExternalEventCommandBase
    {
        private GetFamilyTypesEventHandler _handler => (GetFamilyTypesEventHandler)Handler;

        public override string CommandName => "family_get_types";

        public GetFamilyTypesCommand(UIApplication uiApp)
            : base(new GetFamilyTypesEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<GetFamilyTypesRequest>() ?? new GetFamilyTypesRequest();
            _handler.Request = data;

            if (RaiseAndWaitForCompletion(20000))
                return _handler.Result;
            throw new TimeoutException("family_get_types timed out");
        }
    }
}
