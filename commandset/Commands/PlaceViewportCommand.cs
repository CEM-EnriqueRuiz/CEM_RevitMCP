using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Services;

namespace RevitMCPCommandSet.Commands
{
    /// <summary>
    ///     Place one or more views as viewports on an existing sheet.
    /// </summary>
    public class PlaceViewportCommand : ExternalEventCommandBase
    {
        private PlaceViewportEventHandler _handler => (PlaceViewportEventHandler)Handler;

        public override string CommandName => "place_viewport";

        public PlaceViewportCommand(UIApplication uiApp)
            : base(new PlaceViewportEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<PlaceViewportRequest>();
            if (data == null || data.SheetId == 0 || data.ViewIds == null || data.ViewIds.Count == 0)
                throw new ArgumentException("sheetId and viewIds are required");

            _handler.Request = data;

            if (RaiseAndWaitForCompletion(20000))
                return _handler.Result;
            throw new TimeoutException("place_viewport timed out");
        }
    }
}
