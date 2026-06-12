using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Services;

namespace RevitMCPCommandSet.Commands
{
    /// <summary>
    ///     Generic element tagging in a view (generalizes tag_walls / tag_rooms).
    /// </summary>
    public class TagElementsCommand : ExternalEventCommandBase
    {
        private TagElementsEventHandler _handler => (TagElementsEventHandler)Handler;

        public override string CommandName => "tag_elements";

        public TagElementsCommand(UIApplication uiApp)
            : base(new TagElementsEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<TagElementsRequest>() ?? new TagElementsRequest();
            if ((data.ElementIds == null || data.ElementIds.Count == 0) &&
                (data.Categories == null || data.Categories.Count == 0))
                throw new ArgumentException("Provide elementIds or categories to tag");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(30000))
                return _handler.Result;
            throw new TimeoutException("tag_elements timed out");
        }
    }
}
