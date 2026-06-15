using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Services.Core;

namespace CEM_IAModeler_CommandSet.Commands.Core
{
    public class ManageWorksetsCommand : ExternalEventCommandBase
    {
        private ManageWorksetsEventHandler H => (ManageWorksetsEventHandler)Handler;
        public override string CommandName => "coord_manage_worksets";
        public ManageWorksetsCommand(UIApplication u) : base(new ManageWorksetsEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            H.Request = p?.ToObject<ManageWorksetsRequest>() ?? new ManageWorksetsRequest();
            if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("coord_manage_worksets timed out");
        }
    }

    public class LinkModelCommand : ExternalEventCommandBase
    {
        private LinkModelEventHandler H => (LinkModelEventHandler)Handler;
        public override string CommandName => "coord_link_model";
        public LinkModelCommand(UIApplication u) : base(new LinkModelEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            H.Request = p?.ToObject<LinkModelRequest>() ?? new LinkModelRequest();
            if (RaiseAndWaitForCompletion(60000)) return H.Result; throw new TimeoutException("coord_link_model timed out");
        }
    }

    public class PurgeUnusedCommand : ExternalEventCommandBase
    {
        private PurgeUnusedEventHandler H => (PurgeUnusedEventHandler)Handler;
        public override string CommandName => "coord_purge_unused";
        public PurgeUnusedCommand(UIApplication u) : base(new PurgeUnusedEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            H.Request = p?.ToObject<PurgeUnusedRequest>() ?? new PurgeUnusedRequest();
            if (RaiseAndWaitForCompletion(60000)) return H.Result; throw new TimeoutException("coord_purge_unused timed out");
        }
    }

    public class AuditModelCommand : ExternalEventCommandBase
    {
        private AuditModelEventHandler H => (AuditModelEventHandler)Handler;
        public override string CommandName => "coord_audit_model";
        public AuditModelCommand(UIApplication u) : base(new AuditModelEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            H.Request = p?.ToObject<AuditModelRequest>() ?? new AuditModelRequest();
            if (RaiseAndWaitForCompletion(30000)) return H.Result; throw new TimeoutException("coord_audit_model timed out");
        }
    }

    public class ManagePhasesCommand : ExternalEventCommandBase
    {
        private ManagePhasesEventHandler H => (ManagePhasesEventHandler)Handler;
        public override string CommandName => "coord_manage_phases";
        public ManagePhasesCommand(UIApplication u) : base(new ManagePhasesEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            H.Request = p?.ToObject<ManagePhasesRequest>() ?? new ManagePhasesRequest();
            if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("coord_manage_phases timed out");
        }
    }
}
