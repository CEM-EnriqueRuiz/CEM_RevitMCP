using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Views;
using CEM_IAModeler_CommandSet.Services.Views;

namespace CEM_IAModeler_CommandSet.Commands.Views
{
    public class ExportViewImageCommand : ExternalEventCommandBase
    {
        private ExportViewImageEventHandler H => (ExportViewImageEventHandler)Handler;
        public override string CommandName => "export_view_image";
        public ExportViewImageCommand(UIApplication u) : base(new ExportViewImageEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<ExportViewImageRequest>() ?? new ExportViewImageRequest();
            if (d.PixelSize < 32 || d.PixelSize > 10000) throw new ArgumentException("pixelSize must be between 32 and 10000");
            H.Request = d; if (RaiseAndWaitForCompletion(60000)) return H.Result; throw new TimeoutException("export_view_image timed out");
        }
    }
}
