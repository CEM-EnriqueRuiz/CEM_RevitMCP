using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Services;

namespace RevitMCPCommandSet.Commands
{
    /// <summary>
    ///     Move / rotate / mirror / copy / array a batch of elements.
    /// </summary>
    public class TransformElementsCommand : ExternalEventCommandBase
    {
        private TransformElementsEventHandler _handler => (TransformElementsEventHandler)Handler;

        public override string CommandName => "transform_elements";

        public TransformElementsCommand(UIApplication uiApp)
            : base(new TransformElementsEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<TransformRequest>();
            if (data == null || data.ElementIds == null || data.ElementIds.Count == 0)
                throw new ArgumentException("No elementIds provided");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(30000))
                return _handler.Result;
            throw new TimeoutException("transform_elements timed out");
        }
    }
}
