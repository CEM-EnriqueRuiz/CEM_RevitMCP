using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Cemengal;
using CEM_IAModeler_CommandSet.Services.Cemengal;

namespace CEM_IAModeler_CommandSet.Commands.Cemengal
{
    // The Cemengal add-ins (sibling CEM_RevitAPI repo), called through their own code. The MCP client
    // gives up after 2 minutes, so the long runs wait 115 s and say what to do if Revit is still working.

    public class OpenToolCommand : ExternalEventCommandBase
    {
        private OpenToolEventHandler H => (OpenToolEventHandler)Handler;
        public override string CommandName => "cem_open_tool";
        public OpenToolCommand(UIApplication u) : base(new OpenToolEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            H.Request = p?.ToObject<OpenToolRequest>() ?? new OpenToolRequest();
            if (RaiseAndWaitForCompletion(20000)) return H.Result;
            throw new TimeoutException("cem_open_tool timed out: Revit is busy (a modal window may be open).");
        }
    }

    public class SwapFindTypesCommand : ExternalEventCommandBase
    {
        private SwapFindTypesEventHandler H => (SwapFindTypesEventHandler)Handler;
        public override string CommandName => "cem_swap_find_types";
        public SwapFindTypesCommand(UIApplication u) : base(new SwapFindTypesEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            H.Request = p?.ToObject<SwapFindTypesRequest>() ?? new SwapFindTypesRequest();
            if (RaiseAndWaitForCompletion(60000)) return H.Result;
            throw new TimeoutException("cem_swap_find_types timed out");
        }
    }

    public class SwapListPresetsCommand : ExternalEventCommandBase
    {
        private SwapListPresetsEventHandler H => (SwapListPresetsEventHandler)Handler;
        public override string CommandName => "cem_swap_list_presets";
        public SwapListPresetsCommand(UIApplication u) : base(new SwapListPresetsEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            H.Request = new SwapListPresetsRequest();
            if (RaiseAndWaitForCompletion(30000)) return H.Result;
            throw new TimeoutException("cem_swap_list_presets timed out");
        }
    }

    public class SwapRunCommand : ExternalEventCommandBase
    {
        private SwapRunEventHandler H => (SwapRunEventHandler)Handler;
        public override string CommandName => "cem_swap_run";
        public SwapRunCommand(UIApplication u) : base(new SwapRunEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<SwapRunRequest>();
            if (d == null || (string.IsNullOrWhiteSpace(d.Preset) && d.Swap == null))
                throw new ArgumentException("preset (a name from cem_swap_list_presets) or swap (sources + target) required");
            H.Request = d;
            if (RaiseAndWaitForCompletion(115000)) return H.Result;
            throw new TimeoutException("cem_swap_run is still running in Revit after 115 s. Wait, then check the model " +
                                       "(take_screenshot, cem_swap_find_types counts) before running again; a second run skips what is already replaced.");
        }
    }

    public class SwapRemoveTrialCommand : ExternalEventCommandBase
    {
        private SwapRemoveTrialEventHandler H => (SwapRemoveTrialEventHandler)Handler;
        public override string CommandName => "cem_swap_remove_trial";
        public SwapRemoveTrialCommand(UIApplication u) : base(new SwapRemoveTrialEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            H.Request = new SwapRemoveTrialRequest();
            if (RaiseAndWaitForCompletion(60000)) return H.Result;
            throw new TimeoutException("cem_swap_remove_trial timed out");
        }
    }

    public class RulesApplyCommand : ExternalEventCommandBase
    {
        private RulesApplyEventHandler H => (RulesApplyEventHandler)Handler;
        public override string CommandName => "cem_rules_apply";
        public RulesApplyCommand(UIApplication u) : base(new RulesApplyEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            H.Request = p?.ToObject<RulesApplyRequest>() ?? new RulesApplyRequest();
            if (RaiseAndWaitForCompletion(115000)) return H.Result;
            throw new TimeoutException("cem_rules_apply is still running in Revit after 115 s (large model, or a CEM Rules error " +
                                       "dialog waiting in Revit). Wait for it to finish before running it again.");
        }
    }
}
