using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Services.Core;

namespace CEM_IAModeler_CommandSet.Commands.Core
{
    // Several thin core-editing commands grouped in one file. Each is its own
    // ExternalEventCommandBase with its own handler, per the MCP command contract.

    public class DeleteElementsCommand : ExternalEventCommandBase
    {
        private DeleteElementsEventHandler H => (DeleteElementsEventHandler)Handler;
        public override string CommandName => "delete_elements";
        public DeleteElementsCommand(UIApplication uiApp) : base(new DeleteElementsEventHandler(), uiApp) { }
        public override object Execute(JObject p, string requestId)
        {
            var d = p?.ToObject<DeleteElementsRequest>();
            if (d == null || d.ElementIds == null || d.ElementIds.Count == 0) throw new ArgumentException("elementIds required");
            H.Request = d;
            if (RaiseAndWaitForCompletion(20000)) return H.Result;
            throw new TimeoutException("delete_elements timed out");
        }
    }

    public class DuplicateTypeCommand : ExternalEventCommandBase
    {
        private DuplicateTypeEventHandler H => (DuplicateTypeEventHandler)Handler;
        public override string CommandName => "duplicate_type";
        public DuplicateTypeCommand(UIApplication uiApp) : base(new DuplicateTypeEventHandler(), uiApp) { }
        public override object Execute(JObject p, string requestId)
        {
            var d = p?.ToObject<DuplicateTypeRequest>();
            if (d == null || string.IsNullOrWhiteSpace(d.SourceTypeName) || string.IsNullOrWhiteSpace(d.NewTypeName))
                throw new ArgumentException("sourceTypeName and newTypeName required");
            H.Request = d;
            if (RaiseAndWaitForCompletion(20000)) return H.Result;
            throw new TimeoutException("duplicate_type timed out");
        }
    }

    public class GroupElementsCommand : ExternalEventCommandBase
    {
        private GroupElementsEventHandler H => (GroupElementsEventHandler)Handler;
        public override string CommandName => "group_elements";
        public GroupElementsCommand(UIApplication uiApp) : base(new GroupElementsEventHandler(), uiApp) { }
        public override object Execute(JObject p, string requestId)
        {
            var d = p?.ToObject<GroupElementsRequest>();
            if (d == null || d.ElementIds == null || d.ElementIds.Count < 2) throw new ArgumentException("at least 2 elementIds required");
            H.Request = d;
            if (RaiseAndWaitForCompletion(20000)) return H.Result;
            throw new TimeoutException("group_elements timed out");
        }
    }

    public class UngroupGroupCommand : ExternalEventCommandBase
    {
        private UngroupGroupEventHandler H => (UngroupGroupEventHandler)Handler;
        public override string CommandName => "ungroup_group";
        public UngroupGroupCommand(UIApplication uiApp) : base(new UngroupGroupEventHandler(), uiApp) { }
        public override object Execute(JObject p, string requestId)
        {
            var d = p?.ToObject<ElementIdsRequest>();
            if (d == null || d.ElementIds == null || d.ElementIds.Count == 0) throw new ArgumentException("elementIds (group ids) required");
            H.Request = d;
            if (RaiseAndWaitForCompletion(20000)) return H.Result;
            throw new TimeoutException("ungroup_group timed out");
        }
    }

    public class PinElementsCommand : ExternalEventCommandBase
    {
        private PinElementsEventHandler H => (PinElementsEventHandler)Handler;
        public override string CommandName => "pin_elements";
        public PinElementsCommand(UIApplication uiApp) : base(new PinElementsEventHandler(), uiApp) { }
        public override object Execute(JObject p, string requestId)
        {
            var d = p?.ToObject<PinElementsRequest>();
            if (d == null || d.ElementIds == null || d.ElementIds.Count == 0) throw new ArgumentException("elementIds required");
            H.Request = d;
            if (RaiseAndWaitForCompletion(20000)) return H.Result;
            throw new TimeoutException("pin_elements timed out");
        }
    }

    public class RenameElementCommand : ExternalEventCommandBase
    {
        private RenameElementEventHandler H => (RenameElementEventHandler)Handler;
        public override string CommandName => "rename_element";
        public RenameElementCommand(UIApplication uiApp) : base(new RenameElementEventHandler(), uiApp) { }
        public override object Execute(JObject p, string requestId)
        {
            var d = p?.ToObject<RenameElementRequest>();
            if (d == null || d.ElementId == 0 || string.IsNullOrWhiteSpace(d.NewName)) throw new ArgumentException("elementId and newName required");
            H.Request = d;
            if (RaiseAndWaitForCompletion(20000)) return H.Result;
            throw new TimeoutException("rename_element timed out");
        }
    }
}
