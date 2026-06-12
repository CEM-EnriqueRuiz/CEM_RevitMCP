using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services;

namespace RevitMCPCommandSet.Commands
{
    /// <summary>
    ///     Returns the model's warnings (Document.GetWarnings) grouped and summarized.
    /// </summary>
    public class GetWarningsCommand : ExternalEventCommandBase
    {
        private GetWarningsEventHandler _handler => (GetWarningsEventHandler)Handler;

        public override string CommandName => "get_warnings";

        public GetWarningsCommand(UIApplication uiApp)
            : base(new GetWarningsEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            _handler.MaxElementsPerWarning = parameters?["maxElementsPerWarning"]?.ToObject<int>() ?? 20;

            if (RaiseAndWaitForCompletion(20000))
                return _handler.Result;
            throw new TimeoutException("get_warnings timed out");
        }
    }
}
