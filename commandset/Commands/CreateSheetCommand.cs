using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Services;

namespace RevitMCPCommandSet.Commands
{
    /// <summary>
    ///     Create a sheet from a title block and optionally place views on it.
    /// </summary>
    public class CreateSheetCommand : ExternalEventCommandBase
    {
        private CreateSheetEventHandler _handler => (CreateSheetEventHandler)Handler;

        public override string CommandName => "create_sheet";

        public CreateSheetCommand(UIApplication uiApp)
            : base(new CreateSheetEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            var data = parameters?.ToObject<SheetCreationRequest>() ?? new SheetCreationRequest();
            _handler.Request = data;

            if (RaiseAndWaitForCompletion(20000))
                return _handler.Result;
            throw new TimeoutException("create_sheet timed out");
        }
    }
}
