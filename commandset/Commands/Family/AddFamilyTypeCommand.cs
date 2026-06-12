using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Services.Family;

namespace RevitMCPCommandSet.Commands.Family
{
    public class AddFamilyTypeCommand : ExternalEventCommandBase
    {
        private AddFamilyTypeEventHandler _handler => (AddFamilyTypeEventHandler)Handler;

        public override string CommandName => "family_add_type";

        public AddFamilyTypeCommand(UIApplication uiApp)
            : base(new AddFamilyTypeEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<FamilyAddTypeRequest>();
            if (data == null || string.IsNullOrWhiteSpace(data.TypeName))
                throw new ArgumentException("typeName is required");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(20000))
                return _handler.Result;
            throw new TimeoutException("family_add_type timed out");
        }
    }
}
