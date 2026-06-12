using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Services.Family;

namespace RevitMCPCommandSet.Commands.Family
{
    public class AddFamilyParameterCommand : ExternalEventCommandBase
    {
        private AddFamilyParameterEventHandler _handler => (AddFamilyParameterEventHandler)Handler;

        public override string CommandName => "family_add_parameter";

        public AddFamilyParameterCommand(UIApplication uiApp)
            : base(new AddFamilyParameterEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<FamilyAddParameterRequest>();
            if (data == null || string.IsNullOrWhiteSpace(data.Name))
                throw new ArgumentException("parameter name is required");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(20000))
                return _handler.Result;
            throw new TimeoutException("family_add_parameter timed out");
        }
    }
}
