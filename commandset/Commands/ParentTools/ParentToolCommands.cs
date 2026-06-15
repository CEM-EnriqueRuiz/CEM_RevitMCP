using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Services.ParentTools;

namespace CEM_IAModeler_CommandSet.Commands.ParentTools
{
    // High-level "parent" tools that collapse the IFC/link → native reconstruction
    // pipeline the AI used to hand-write as send_code_to_revit recipes. Units mm.

    public class LinkExtractGeometryCommand : ExternalEventCommandBase
    {
        private LinkExtractEventHandler H => (LinkExtractEventHandler)Handler;
        public override string CommandName => "parent_link_extract_geometry";
        public LinkExtractGeometryCommand(UIApplication u) : base(new LinkExtractEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            H.Request = p?.ToObject<LinkExtractRequest>() ?? new LinkExtractRequest();
            if (RaiseAndWaitForCompletion(60000)) return H.Result;
            throw new TimeoutException("parent_link_extract_geometry timed out");
        }
    }

    public class SolidToMemberParamsCommand : ExternalEventCommandBase
    {
        private MemberParamsEventHandler H => (MemberParamsEventHandler)Handler;
        public override string CommandName => "parent_solid_to_member_params";
        public SolidToMemberParamsCommand(UIApplication u) : base(new MemberParamsEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<MemberParamsRequest>();
            if (d == null) throw new ArgumentException("request body required");
            H.Request = d;
            if (RaiseAndWaitForCompletion(120000)) return H.Result;
            throw new TimeoutException("parent_solid_to_member_params timed out");
        }
    }

    public class ReconstructNativeCommand : ExternalEventCommandBase
    {
        private ReconstructEventHandler H => (ReconstructEventHandler)Handler;
        public override string CommandName => "parent_reconstruct_native";
        public ReconstructNativeCommand(UIApplication u) : base(new ReconstructEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<ReconstructRequest>();
            if (d == null || string.IsNullOrWhiteSpace(d.Category))
                throw new ArgumentException("category required");
            H.Request = d;
            if (RaiseAndWaitForCompletion(300000)) return H.Result;
            throw new TimeoutException("parent_reconstruct_native timed out");
        }
    }

    public class ValidateDeviationCommand : ExternalEventCommandBase
    {
        private ValidateDeviationEventHandler H => (ValidateDeviationEventHandler)Handler;
        public override string CommandName => "parent_validate_deviation";
        public ValidateDeviationCommand(UIApplication u) : base(new ValidateDeviationEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            H.Request = p?.ToObject<ValidateDeviationRequest>() ?? new ValidateDeviationRequest();
            if (RaiseAndWaitForCompletion(180000)) return H.Result;
            throw new TimeoutException("parent_validate_deviation timed out");
        }
    }
}
