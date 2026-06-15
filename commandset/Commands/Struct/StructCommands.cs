using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Services.Struct;

namespace CEM_IAModeler_CommandSet.Commands.Struct
{
    public class CreateBeamCommand : ExternalEventCommandBase
    {
        private StructLineMemberEventHandler H => (StructLineMemberEventHandler)Handler;
        public override string CommandName => "struct_create_beam";
        public CreateBeamCommand(UIApplication u) : base(new StructLineMemberEventHandler { MemberType = StructuralType.Beam }, u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<StructLineMemberRequest>();
            if (d == null || d.Start == null || d.End == null) throw new ArgumentException("start and end required");
            H.MemberType = StructuralType.Beam;
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("struct_create_beam timed out");
        }
    }

    public class CreateBraceCommand : ExternalEventCommandBase
    {
        private StructLineMemberEventHandler H => (StructLineMemberEventHandler)Handler;
        public override string CommandName => "struct_create_brace";
        public CreateBraceCommand(UIApplication u) : base(new StructLineMemberEventHandler { MemberType = StructuralType.Brace }, u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<StructLineMemberRequest>();
            if (d == null || d.Start == null || d.End == null) throw new ArgumentException("start and end required");
            H.MemberType = StructuralType.Brace;
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("struct_create_brace timed out");
        }
    }

    public class CreateColumnCommand : ExternalEventCommandBase
    {
        private StructColumnEventHandler H => (StructColumnEventHandler)Handler;
        public override string CommandName => "struct_create_column";
        public CreateColumnCommand(UIApplication u) : base(new StructColumnEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<StructColumnRequest>();
            if (d == null || d.Location == null) throw new ArgumentException("location required");
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("struct_create_column timed out");
        }
    }

    public class CreateFoundationCommand : ExternalEventCommandBase
    {
        private StructFoundationEventHandler H => (StructFoundationEventHandler)Handler;
        public override string CommandName => "struct_create_foundation";
        public CreateFoundationCommand(UIApplication u) : base(new StructFoundationEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<StructFoundationRequest>();
            if (d == null || d.Location == null) throw new ArgumentException("location required");
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("struct_create_foundation timed out");
        }
    }
}
