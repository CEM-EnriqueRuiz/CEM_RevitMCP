using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Services.Family;

namespace RevitMCPCommandSet.Commands.Family
{
    public class LoadFamilyCommand : ExternalEventCommandBase
    {
        private LoadFamilyEventHandler _handler => (LoadFamilyEventHandler)Handler;

        public override string CommandName => "family_load";

        public LoadFamilyCommand(UIApplication uiApp)
            : base(new LoadFamilyEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<FamilyLoadRequest>();
            if (data == null || string.IsNullOrWhiteSpace(data.Path))
                throw new ArgumentException("path to the .rfa file is required");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(30000))
                return _handler.Result;
            throw new TimeoutException("family_load timed out");
        }
    }
}
