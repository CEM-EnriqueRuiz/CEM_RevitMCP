using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPCommandSet.Services.Mep;

namespace RevitMCPCommandSet.Commands.Mep
{
    public class PlaceMepEquipmentCommand : ExternalEventCommandBase
    {
        private PlaceMepEquipmentEventHandler _handler => (PlaceMepEquipmentEventHandler)Handler;

        public override string CommandName => "mep_place_equipment";

        public PlaceMepEquipmentCommand(UIApplication uiApp)
            : base(new PlaceMepEquipmentEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<MepEquipmentRequest>();
            if (data == null || string.IsNullOrWhiteSpace(data.TypeName) || data.Points == null || data.Points.Count == 0)
                throw new ArgumentException("typeName and points are required");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(30000))
                return _handler.Result;
            throw new TimeoutException("mep_place_equipment timed out");
        }
    }
}
