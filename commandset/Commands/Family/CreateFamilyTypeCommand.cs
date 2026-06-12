using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Services.Family;

namespace RevitMCPCommandSet.Commands.Family
{
    public class CreateFamilyTypeCommand : ExternalEventCommandBase
    {
        private CreateFamilyTypeEventHandler _handler => (CreateFamilyTypeEventHandler)Handler;

        public override string CommandName => "family_create_type";

        public CreateFamilyTypeCommand(UIApplication uiApp)
            : base(new CreateFamilyTypeEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<CreateFamilyTypeRequest>();
            if (data == null || string.IsNullOrWhiteSpace(data.SourceTypeName) || string.IsNullOrWhiteSpace(data.NewTypeName))
                throw new ArgumentException("sourceTypeName and newTypeName are required");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(20000))
                return _handler.Result;
            throw new TimeoutException("family_create_type timed out");
        }
    }
}
