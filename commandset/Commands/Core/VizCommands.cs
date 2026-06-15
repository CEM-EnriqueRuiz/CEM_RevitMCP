using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Services.Core;

namespace CEM_IAModeler_CommandSet.Commands.Core
{
    public class CreateMaterialCommand : ExternalEventCommandBase
    {
        private CreateMaterialEventHandler H => (CreateMaterialEventHandler)Handler;
        public override string CommandName => "viz_create_material";
        public CreateMaterialCommand(UIApplication u) : base(new CreateMaterialEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<CreateMaterialRequest>();
            if (d == null || string.IsNullOrWhiteSpace(d.Name)) throw new ArgumentException("name is required");
            H.Request = d; if (RaiseAndWaitForCompletion(15000)) return H.Result; throw new TimeoutException("viz_create_material timed out");
        }
    }

    public class SetMaterialAppearanceCommand : ExternalEventCommandBase
    {
        private SetMaterialAppearanceEventHandler H => (SetMaterialAppearanceEventHandler)Handler;
        public override string CommandName => "viz_set_material_appearance";
        public SetMaterialAppearanceCommand(UIApplication u) : base(new SetMaterialAppearanceEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<SetMaterialAppearanceRequest>();
            if (d == null || string.IsNullOrWhiteSpace(d.Material)) throw new ArgumentException("material is required");
            H.Request = d; if (RaiseAndWaitForCompletion(15000)) return H.Result; throw new TimeoutException("viz_set_material_appearance timed out");
        }
    }

    public class ListMaterialsCommand : ExternalEventCommandBase
    {
        private ListMaterialsEventHandler H => (ListMaterialsEventHandler)Handler;
        public override string CommandName => "viz_list_materials";
        public ListMaterialsCommand(UIApplication u) : base(new ListMaterialsEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            H.Request = p?.ToObject<ListMaterialsRequest>() ?? new ListMaterialsRequest();
            if (RaiseAndWaitForCompletion(15000)) return H.Result; throw new TimeoutException("viz_list_materials timed out");
        }
    }

    public class AssignMaterialCommand : ExternalEventCommandBase
    {
        private AssignMaterialEventHandler H => (AssignMaterialEventHandler)Handler;
        public override string CommandName => "viz_assign_material";
        public AssignMaterialCommand(UIApplication u) : base(new AssignMaterialEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<AssignMaterialRequest>();
            if (d == null || string.IsNullOrWhiteSpace(d.Material) || d.ElementIds == null || d.ElementIds.Count == 0)
                throw new ArgumentException("material and elementIds are required");
            H.Request = d; if (RaiseAndWaitForCompletion(30000)) return H.Result; throw new TimeoutException("viz_assign_material timed out");
        }
    }
}
