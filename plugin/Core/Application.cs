using Autodesk.Revit.UI;

namespace CEM_IAModeler.Core
{
    public class Application : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            // Ribbon UI is provided by CEM_RibbonUI (the MCP "Switch" button lives on the
            // Cemengal tab). This add-in registers no UI of its own to avoid a duplicate panel.
            // The plugin DLL is referenced by CEM_RibbonUI; the MCP server is toggled from there.
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                // Avoid lazily creating the SocketService singleton if it was never started.
                if (SocketService.HasInstance && SocketService.Instance.IsRunning)
                {
                    SocketService.Instance.Stop();
                }
            }
            catch { }

            return Result.Succeeded;
        }
    }
}
