using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Views;
using CEM_IAModeler_CommandSet.Services.Views;

namespace CEM_IAModeler_CommandSet.Commands.Views
{
    public class CreateScheduleCommand : ExternalEventCommandBase
    {
        private CreateScheduleEventHandler H => (CreateScheduleEventHandler)Handler;
        public override string CommandName => "create_schedule";
        public CreateScheduleCommand(UIApplication u) : base(new CreateScheduleEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<ScheduleCreationRequest>();
            if (d == null || string.IsNullOrWhiteSpace(d.Category)) throw new ArgumentException("category is required");
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("create_schedule timed out");
        }
    }

    public class DuplicateViewCommand : ExternalEventCommandBase
    {
        private DuplicateViewEventHandler H => (DuplicateViewEventHandler)Handler;
        public override string CommandName => "duplicate_view";
        public DuplicateViewCommand(UIApplication u) : base(new DuplicateViewEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<DuplicateViewRequest>() ?? new DuplicateViewRequest();
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("duplicate_view timed out");
        }
    }

    public class SetViewPropertiesCommand : ExternalEventCommandBase
    {
        private SetViewPropertiesEventHandler H => (SetViewPropertiesEventHandler)Handler;
        public override string CommandName => "set_view_properties";
        public SetViewPropertiesCommand(UIApplication u) : base(new SetViewPropertiesEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<SetViewPropertiesRequest>() ?? new SetViewPropertiesRequest();
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("set_view_properties timed out");
        }
    }

    public class ApplyFilterToViewCommand : ExternalEventCommandBase
    {
        private ApplyFilterToViewEventHandler H => (ApplyFilterToViewEventHandler)Handler;
        public override string CommandName => "apply_filter_to_view";
        public ApplyFilterToViewCommand(UIApplication u) : base(new ApplyFilterToViewEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<ApplyFilterToViewRequest>();
            if (d == null || string.IsNullOrWhiteSpace(d.FilterName)) throw new ArgumentException("filterName is required");
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("apply_filter_to_view timed out");
        }
    }

    public class PlaceTextCommand : ExternalEventCommandBase
    {
        private PlaceTextEventHandler H => (PlaceTextEventHandler)Handler;
        public override string CommandName => "place_text";
        public PlaceTextCommand(UIApplication u) : base(new PlaceTextEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<PlaceTextRequest>();
            if (d == null || string.IsNullOrWhiteSpace(d.Text) || d.Position == null)
                throw new ArgumentException("text and position are required");
            H.Request = d; if (RaiseAndWaitForCompletion(15000)) return H.Result; throw new TimeoutException("place_text timed out");
        }
    }

    public class CreateFilledRegionCommand : ExternalEventCommandBase
    {
        private FilledRegionEventHandler H => (FilledRegionEventHandler)Handler;
        public override string CommandName => "create_filled_region";
        public CreateFilledRegionCommand(UIApplication u) : base(new FilledRegionEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<FilledRegionRequest>();
            if (d == null || d.Boundary == null || d.Boundary.Count < 3) throw new ArgumentException("boundary (>=3 points) required");
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("create_filled_region timed out");
        }
    }

    public class CreateRevisionCommand : ExternalEventCommandBase
    {
        private RevisionEventHandler H => (RevisionEventHandler)Handler;
        public override string CommandName => "create_revision";
        public CreateRevisionCommand(UIApplication u) : base(new RevisionEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<RevisionRequest>() ?? new RevisionRequest();
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("create_revision timed out");
        }
    }

    public class CreateLegendCommand : ExternalEventCommandBase
    {
        private CreateLegendEventHandler H => (CreateLegendEventHandler)Handler;
        public override string CommandName => "create_legend";
        public CreateLegendCommand(UIApplication u) : base(new CreateLegendEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<CreateLegendRequest>() ?? new CreateLegendRequest();
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("create_legend timed out");
        }
    }
}
