using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Services.Family;

namespace RevitMCPCommandSet.Commands.Family
{
    public class SetFamilyFormulaCommand : ExternalEventCommandBase
    {
        private SetFamilyFormulaEventHandler _handler => (SetFamilyFormulaEventHandler)Handler;

        public override string CommandName => "family_set_formula";

        public SetFamilyFormulaCommand(UIApplication uiApp)
            : base(new SetFamilyFormulaEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<FamilySetFormulaRequest>();
            if (data == null || string.IsNullOrWhiteSpace(data.ParameterName))
                throw new ArgumentException("parameterName is required");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(20000))
                return _handler.Result;
            throw new TimeoutException("family_set_formula timed out");
        }
    }
}
