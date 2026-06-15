using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Services.Core;

namespace CEM_IAModeler_CommandSet.Commands.Core
{
    public class EditCurtainGridCommand : ExternalEventCommandBase
    {
        private EditCurtainGridEventHandler H => (EditCurtainGridEventHandler)Handler;
        public override string CommandName => "edit_curtain_grid";
        public EditCurtainGridCommand(UIApplication u) : base(new EditCurtainGridEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<EditCurtainGridRequest>();
            if (d == null || d.HostId == 0) throw new ArgumentException("hostId required");
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("edit_curtain_grid timed out");
        }
    }

    public class PlaceImageCommand : ExternalEventCommandBase
    {
        private PlaceImageEventHandler H => (PlaceImageEventHandler)Handler;
        public override string CommandName => "place_image";
        public PlaceImageCommand(UIApplication u) : base(new PlaceImageEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<PlaceImageRequest>();
            if (d == null || string.IsNullOrWhiteSpace(d.Path)) throw new ArgumentException("path required");
            H.Request = d; if (RaiseAndWaitForCompletion(30000)) return H.Result; throw new TimeoutException("place_image timed out");
        }
    }

    public class CreatePartsCommand : ExternalEventCommandBase
    {
        private CreatePartsEventHandler H => (CreatePartsEventHandler)Handler;
        public override string CommandName => "create_parts";
        public CreatePartsCommand(UIApplication u) : base(new CreatePartsEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<CreatePartsRequest>();
            if (d == null || d.ElementIds == null || d.ElementIds.Count == 0) throw new ArgumentException("elementIds required");
            H.Request = d; if (RaiseAndWaitForCompletion(30000)) return H.Result; throw new TimeoutException("create_parts timed out");
        }
    }
}
