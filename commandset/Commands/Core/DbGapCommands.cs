using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Services.Core;

namespace CEM_IAModeler_CommandSet.Commands.Core
{
    public class CreateModelLinesCommand : ExternalEventCommandBase
    {
        private CreateModelLinesEventHandler H => (CreateModelLinesEventHandler)Handler;
        public override string CommandName => "create_model_lines";
        public CreateModelLinesCommand(UIApplication u) : base(new CreateModelLinesEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<CreateModelLinesRequest>();
            if (d == null || d.Points == null || d.Points.Count < 2) throw new ArgumentException("points (>=2) required");
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("create_model_lines timed out");
        }
    }

    public class CreateDetailLinesCommand : ExternalEventCommandBase
    {
        private CreateDetailLinesEventHandler H => (CreateDetailLinesEventHandler)Handler;
        public override string CommandName => "create_detail_lines";
        public CreateDetailLinesCommand(UIApplication u) : base(new CreateDetailLinesEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<CreateDetailLinesRequest>();
            if (d == null || d.Points == null || d.Points.Count < 2) throw new ArgumentException("points (>=2) required");
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("create_detail_lines timed out");
        }
    }

    public class CreateDirectShapeCommand : ExternalEventCommandBase
    {
        private CreateDirectShapeEventHandler H => (CreateDirectShapeEventHandler)Handler;
        public override string CommandName => "create_direct_shape";
        public CreateDirectShapeCommand(UIApplication u) : base(new CreateDirectShapeEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<CreateDirectShapeRequest>();
            if (d == null) throw new ArgumentException("request body required");
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("create_direct_shape timed out");
        }
    }

    public class CreateSpotDimensionCommand : ExternalEventCommandBase
    {
        private CreateSpotDimensionEventHandler H => (CreateSpotDimensionEventHandler)Handler;
        public override string CommandName => "create_spot_dimension";
        public CreateSpotDimensionCommand(UIApplication u) : base(new CreateSpotDimensionEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<CreateSpotDimensionRequest>();
            if (d == null || d.ElementId == 0 || d.Point == null) throw new ArgumentException("elementId and point required");
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("create_spot_dimension timed out");
        }
    }

    public class CreateAssemblyCommand : ExternalEventCommandBase
    {
        private CreateAssemblyEventHandler H => (CreateAssemblyEventHandler)Handler;
        public override string CommandName => "create_assembly";
        public CreateAssemblyCommand(UIApplication u) : base(new CreateAssemblyEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<CreateAssemblyRequest>();
            if (d == null || d.ElementIds == null || d.ElementIds.Count == 0) throw new ArgumentException("elementIds required");
            H.Request = d; if (RaiseAndWaitForCompletion(30000)) return H.Result; throw new TimeoutException("create_assembly timed out");
        }
    }

    public class CreateMultiSegmentGridCommand : ExternalEventCommandBase
    {
        private CreateMultiSegmentGridEventHandler H => (CreateMultiSegmentGridEventHandler)Handler;
        public override string CommandName => "create_multi_segment_grid";
        public CreateMultiSegmentGridCommand(UIApplication u) : base(new CreateMultiSegmentGridEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<CreateMultiSegmentGridRequest>();
            if (d == null || d.Points == null || d.Points.Count < 2) throw new ArgumentException("points (>=2) required");
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("create_multi_segment_grid timed out");
        }
    }
}
