using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Services.Core;

namespace CEM_IAModeler_CommandSet.Commands.Core
{
    public class ManageLinePatternCommand : ExternalEventCommandBase
    {
        private LinePatternEventHandler H => (LinePatternEventHandler)Handler;
        public override string CommandName => "manage_line_pattern";
        public ManageLinePatternCommand(UIApplication u) : base(new LinePatternEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<LinePatternRequest>() ?? new LinePatternRequest();
            H.Request = d; if (RaiseAndWaitForCompletion(15000)) return H.Result; throw new TimeoutException("manage_line_pattern timed out");
        }
    }

    public class ManageFillPatternCommand : ExternalEventCommandBase
    {
        private FillPatternEventHandler H => (FillPatternEventHandler)Handler;
        public override string CommandName => "manage_fill_pattern";
        public ManageFillPatternCommand(UIApplication u) : base(new FillPatternEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<FillPatternRequest>() ?? new FillPatternRequest();
            H.Request = d; if (RaiseAndWaitForCompletion(15000)) return H.Result; throw new TimeoutException("manage_fill_pattern timed out");
        }
    }

    public class SetObjectStylesCommand : ExternalEventCommandBase
    {
        private ObjectStylesEventHandler H => (ObjectStylesEventHandler)Handler;
        public override string CommandName => "set_object_styles";
        public SetObjectStylesCommand(UIApplication u) : base(new ObjectStylesEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<ObjectStylesRequest>();
            if (d == null || string.IsNullOrWhiteSpace(d.Category)) throw new ArgumentException("category is required");
            H.Request = d; if (RaiseAndWaitForCompletion(15000)) return H.Result; throw new TimeoutException("set_object_styles timed out");
        }
    }
}
