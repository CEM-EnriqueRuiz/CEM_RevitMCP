using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using CEM_RevitAuth;
using System;

namespace CEM_IAModeler.Core
{
    [Transaction(TransactionMode.Manual)]
    public class MCPServiceConnection : AuthenticatedExternalCommand
    {
        // Inheriting AuthenticatedExternalCommand seals Execute() to run EnsureAuthorized()
        // first; ExecuteAuthorized() (below) only runs with a valid CEM license. This ensures
        // the socket listener — which arms the MCP external-event handlers — never starts for
        // an unlicensed user, beyond the ribbon button's Enabled flag.
        protected override void ExecuteAuthorized()
        {
            try
            {
                // 获取socket服务 / Obtain socket service.
                SocketService service = SocketService.Instance;

                if (service.IsRunning)
                {
                    service.Stop();
                    TaskDialog.Show("revitMCP", "Close Server");
                }
                else
                {
                    // base.Application (the inherited UIApplication) — qualified to avoid the
                    // sibling CEM_IAModeler.Core.Application IExternalApplication type.
                    service.Initialize(base.Application);
                    service.Start();
                    TaskDialog.Show("revitMCP", "Open Server");
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("revitMCP", $"Error: {ex.Message}");
            }
        }
    }
}
