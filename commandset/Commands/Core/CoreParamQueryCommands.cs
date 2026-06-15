using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Services.Core;

namespace CEM_IAModeler_CommandSet.Commands.Core
{
    public class CopyParameterValuesCommand : ExternalEventCommandBase
    {
        private CopyParameterValuesEventHandler H => (CopyParameterValuesEventHandler)Handler;
        public override string CommandName => "copy_parameter_values";
        public CopyParameterValuesCommand(UIApplication uiApp) : base(new CopyParameterValuesEventHandler(), uiApp) { }
        public override object Execute(JObject p, string requestId)
        {
            var d = p?.ToObject<CopyParameterValuesRequest>();
            if (d == null || d.SourceId == 0 || d.TargetIds == null || d.TargetIds.Count == 0) throw new ArgumentException("sourceId and targetIds required");
            H.Request = d;
            if (RaiseAndWaitForCompletion(30000)) return H.Result;
            throw new TimeoutException("copy_parameter_values timed out");
        }
    }

    public class BulkSetByFilterCommand : ExternalEventCommandBase
    {
        private BulkSetByFilterEventHandler H => (BulkSetByFilterEventHandler)Handler;
        public override string CommandName => "bulk_set_by_filter";
        public BulkSetByFilterCommand(UIApplication uiApp) : base(new BulkSetByFilterEventHandler(), uiApp) { }
        public override object Execute(JObject p, string requestId)
        {
            var d = p?.ToObject<BulkSetByFilterRequest>();
            if (d == null || d.Categories == null || d.Categories.Count == 0) throw new ArgumentException("categories required");
            if (string.IsNullOrWhiteSpace(d.ParameterName) && string.IsNullOrWhiteSpace(d.BuiltInParameter)) throw new ArgumentException("parameterName or builtInParameter required");
            H.Request = d;
            if (RaiseAndWaitForCompletion(60000)) return H.Result;
            throw new TimeoutException("bulk_set_by_filter timed out");
        }
    }

    public class ListParamsForCategoryCommand : ExternalEventCommandBase
    {
        private ListParamsForCategoryEventHandler H => (ListParamsForCategoryEventHandler)Handler;
        public override string CommandName => "list_parameters_for_category";
        public ListParamsForCategoryCommand(UIApplication uiApp) : base(new ListParamsForCategoryEventHandler(), uiApp) { }
        public override object Execute(JObject p, string requestId)
        {
            var d = p?.ToObject<ListParamsForCategoryRequest>();
            if (d == null || string.IsNullOrWhiteSpace(d.Category)) throw new ArgumentException("category required");
            H.Request = d;
            if (RaiseAndWaitForCompletion(20000)) return H.Result;
            throw new TimeoutException("list_parameters_for_category timed out");
        }
    }

    public class CreateProjectParameterCommand : ExternalEventCommandBase
    {
        private CreateProjectParameterEventHandler H => (CreateProjectParameterEventHandler)Handler;
        public override string CommandName => "create_project_parameter";
        public CreateProjectParameterCommand(UIApplication uiApp) : base(new CreateProjectParameterEventHandler(), uiApp) { }
        public override object Execute(JObject p, string requestId)
        {
            var d = p?.ToObject<CreateProjectParameterRequest>();
            if (d == null || string.IsNullOrWhiteSpace(d.Name) || d.Categories == null || d.Categories.Count == 0) throw new ArgumentException("name and categories required");
            H.Request = d;
            if (RaiseAndWaitForCompletion(20000)) return H.Result;
            throw new TimeoutException("create_project_parameter timed out");
        }
    }

    public class GetElementInfoCommand : ExternalEventCommandBase
    {
        private GetElementInfoEventHandler H => (GetElementInfoEventHandler)Handler;
        public override string CommandName => "get_element_info";
        public GetElementInfoCommand(UIApplication uiApp) : base(new GetElementInfoEventHandler(), uiApp) { }
        public override object Execute(JObject p, string requestId)
        {
            var d = p?.ToObject<ElementIdsRequest>();
            if (d == null || d.ElementIds == null || d.ElementIds.Count == 0) throw new ArgumentException("elementIds required");
            H.Request = d;
            if (RaiseAndWaitForCompletion(20000)) return H.Result;
            throw new TimeoutException("get_element_info timed out");
        }
    }

    public class GetElementGeometryCommand : ExternalEventCommandBase
    {
        private GetElementGeometryEventHandler H => (GetElementGeometryEventHandler)Handler;
        public override string CommandName => "get_element_geometry";
        public GetElementGeometryCommand(UIApplication uiApp) : base(new GetElementGeometryEventHandler(), uiApp) { }
        public override object Execute(JObject p, string requestId)
        {
            var d = p?.ToObject<ElementIdsRequest>();
            if (d == null || d.ElementIds == null || d.ElementIds.Count == 0) throw new ArgumentException("elementIds required");
            H.Request = d;
            if (RaiseAndWaitForCompletion(20000)) return H.Result;
            throw new TimeoutException("get_element_geometry timed out");
        }
    }

    public class FindElementsCommand : ExternalEventCommandBase
    {
        private FindElementsEventHandler H => (FindElementsEventHandler)Handler;
        public override string CommandName => "find_elements";
        public FindElementsCommand(UIApplication uiApp) : base(new FindElementsEventHandler(), uiApp) { }
        public override object Execute(JObject p, string requestId)
        {
            var d = p?.ToObject<FindElementsRequest>();
            if (d == null || d.Categories == null || d.Categories.Count == 0) throw new ArgumentException("categories required");
            H.Request = d;
            if (RaiseAndWaitForCompletion(30000)) return H.Result;
            throw new TimeoutException("find_elements timed out");
        }
    }
}
