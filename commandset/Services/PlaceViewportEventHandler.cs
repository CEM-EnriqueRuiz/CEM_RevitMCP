using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    /// <summary>
    ///     Places views as viewports on an existing sheet (grid auto-layout unless a center is given).
    /// </summary>
    public class PlaceViewportEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public PlaceViewportRequest Request { get; set; }
        public AIResult<PlaceViewportResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new PlaceViewportResult();
            try
            {
                var sheet = McpResolveUtils.GetElement(doc, Request.SheetId) as ViewSheet;
                if (sheet == null)
                {
                    Result = new AIResult<PlaceViewportResult> { Success = false, Message = $"Sheet {Request.SheetId} not found." };
                    _resetEvent.Set();
                    return;
                }

                using (var tx = new Transaction(doc, "Place Viewports"))
                {
                    tx.Start();
                    double startX = McpResolveUtils.MmToFt(150), startY = McpResolveUtils.MmToFt(600);
                    double stepX = McpResolveUtils.MmToFt(450), stepY = McpResolveUtils.MmToFt(450);
                    int perRow = 3, i = 0;

                    foreach (long vid in Request.ViewIds)
                    {
                        var view = McpResolveUtils.GetElement(doc, vid) as View;
                        if (view == null) { res.Warnings.Add($"View {vid} not found."); continue; }
                        if (!Viewport.CanAddViewToSheet(doc, sheet.Id, view.Id))
                        {
                            res.Warnings.Add($"View {vid} cannot be placed (already on a sheet or unplaceable).");
                            continue;
                        }

                        XYZ pt;
                        if (Request.Center != null && Request.ViewIds.Count == 1)
                            pt = JZPoint.ToXYZ(Request.Center);
                        else
                            pt = new XYZ(startX + (i % perRow) * stepX, startY - (i / perRow) * stepY, 0);

                        var vp = Viewport.Create(doc, sheet.Id, view.Id, pt);
                        if (vp != null) { res.PlacedViewports.Add(vp.Id.GetValue()); i++; }
                    }
                    tx.Commit();
                }

                Result = new AIResult<PlaceViewportResult>
                {
                    Success = res.PlacedViewports.Count > 0,
                    Message = $"Placed {res.PlacedViewports.Count} viewport(s)" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<PlaceViewportResult>
                    { Success = false, Message = $"Error placing viewports: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Place Viewport";
    }
}
