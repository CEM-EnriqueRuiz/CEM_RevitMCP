using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Services.Family;

namespace RevitMCPCommandSet.Commands.Family
{
    public class PlaceInstanceCommand : ExternalEventCommandBase
    {
        private PlaceInstanceEventHandler _handler => (PlaceInstanceEventHandler)Handler;

        public override string CommandName => "family_place_instance";

        public PlaceInstanceCommand(UIApplication uiApp)
            : base(new PlaceInstanceEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<PlaceInstanceRequest>();
            if (data == null || string.IsNullOrWhiteSpace(data.TypeName) || data.Points == null || data.Points.Count == 0)
                throw new ArgumentException("typeName and points are required");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(30000))
                return _handler.Result;
            throw new TimeoutException("family_place_instance timed out");
        }
    }
}
