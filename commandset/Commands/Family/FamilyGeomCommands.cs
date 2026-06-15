using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Family;
using CEM_IAModeler_CommandSet.Services.Family;

namespace CEM_IAModeler_CommandSet.Commands.Family
{
    public class FamilyExtrusionCommand : ExternalEventCommandBase
    {
        private FamilyExtrusionEventHandler H => (FamilyExtrusionEventHandler)Handler;
        public override string CommandName => "family_create_extrusion";
        public FamilyExtrusionCommand(UIApplication u) : base(new FamilyExtrusionEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<FamilyExtrusionRequest>();
            bool hasLoops = d?.Loops != null && d.Loops.Count > 0;
            bool hasProfile = d?.Profile != null && d.Profile.Count >= 3;
            if (d == null || (!hasLoops && !hasProfile))
                throw new ArgumentException("provide loops[] (outer + optional holes) or a profile of >=3 points");
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("family_create_extrusion timed out");
        }
    }

    public class FamilyRevolutionCommand : ExternalEventCommandBase
    {
        private FamilyRevolutionEventHandler H => (FamilyRevolutionEventHandler)Handler;
        public override string CommandName => "family_create_revolution";
        public FamilyRevolutionCommand(UIApplication u) : base(new FamilyRevolutionEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<FamilyRevolutionRequest>();
            if (d == null || d.Profile == null || d.Profile.Count < 3) throw new ArgumentException("profile (>=3 points) required");
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("family_create_revolution timed out");
        }
    }

    public class FamilyAssociateParameterCommand : ExternalEventCommandBase
    {
        private FamilyAssociateParameterEventHandler H => (FamilyAssociateParameterEventHandler)Handler;
        public override string CommandName => "family_associate_parameter";
        public FamilyAssociateParameterCommand(UIApplication u) : base(new FamilyAssociateParameterEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<FamilyAssociateParameterRequest>();
            if (d == null || d.ElementId == 0 || string.IsNullOrWhiteSpace(d.ElementParameter) || string.IsNullOrWhiteSpace(d.FamilyParameter))
                throw new ArgumentException("elementId, elementParameter and familyParameter required");
            H.Request = d; if (RaiseAndWaitForCompletion(15000)) return H.Result; throw new TimeoutException("family_associate_parameter timed out");
        }
    }

    public class FamilyAddReferencePlaneCommand : ExternalEventCommandBase
    {
        private FamilyAddReferencePlaneEventHandler H => (FamilyAddReferencePlaneEventHandler)Handler;
        public override string CommandName => "family_add_reference_plane";
        public FamilyAddReferencePlaneCommand(UIApplication u) : base(new FamilyAddReferencePlaneEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<FamilyAddReferencePlaneRequest>();
            if (d == null || d.BubbleEnd == null || d.FreeEnd == null || d.CutVectorPoint == null)
                throw new ArgumentException("bubbleEnd, freeEnd and cutVectorPoint required");
            H.Request = d; if (RaiseAndWaitForCompletion(15000)) return H.Result; throw new TimeoutException("family_add_reference_plane timed out");
        }
    }
}
