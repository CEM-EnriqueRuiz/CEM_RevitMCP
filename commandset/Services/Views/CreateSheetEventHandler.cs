using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Models.Views;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Views
{
    /// <summary>
    ///     Creates a ViewSheet from a title block, then places any requested views
    ///     as viewports auto-arranged in a simple grid.
    /// </summary>
    public class CreateSheetEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public SheetCreationRequest Request { get; set; }
        public AIResult<SheetCreationResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new SheetCreationResult();
            try
            {
                ElementId tbId = ResolveTitleBlock();

                ViewSheet sheet;
                using (var tx = new Transaction(doc, "Create Sheet"))
                {
                    tx.Start();
                    sheet = ViewSheet.Create(doc, tbId);
                    if (!string.IsNullOrWhiteSpace(Request.Number)) { try { sheet.SheetNumber = Request.Number; } catch { res.Warnings.Add($"Sheet number '{Request.Number}' rejected (maybe duplicate)."); } }
                    if (!string.IsNullOrWhiteSpace(Request.Name)) { try { sheet.Name = Request.Name; } catch { } }

                    // Auto-place requested views in a grid
                    if (Request.ViewIds != null && Request.ViewIds.Count > 0)
                    {
                        PlaceViewsGrid(sheet, Request.ViewIds, res);
                    }
                    tx.Commit();
                }

                res.SheetId = sheet.Id.GetValue();
                res.Number = sheet.SheetNumber;
                Result = new AIResult<SheetCreationResult>
                {
                    Success = true,
                    Message = $"Created sheet '{sheet.SheetNumber} - {sheet.Name}' (id {res.SheetId}), placed {res.PlacedViewports.Count} viewport(s)" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<SheetCreationResult>
                    { Success = false, Message = $"Error creating sheet: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        private ElementId ResolveTitleBlock()
        {
            var tbs = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsElementType().Cast<FamilySymbol>().ToList();

            if (!string.IsNullOrWhiteSpace(Request.TitleBlockName))
            {
                var match = tbs.FirstOrDefault(t =>
                    t.Name.Equals(Request.TitleBlockName, StringComparison.OrdinalIgnoreCase) ||
                    $"{t.FamilyName} : {t.Name}".Equals(Request.TitleBlockName, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match.Id;
            }
            return tbs.FirstOrDefault()?.Id ?? ElementId.InvalidElementId;
        }

        private void PlaceViewsGrid(ViewSheet sheet, List<long> viewIds, SheetCreationResult res)
        {
            // Simple grid layout in sheet space (feet)
            double startX = McpResolveUtils.MmToFt(150), startY = McpResolveUtils.MmToFt(600);
            double stepX = McpResolveUtils.MmToFt(450), stepY = McpResolveUtils.MmToFt(450);
            int perRow = 3, i = 0;

            foreach (long vid in viewIds)
            {
                var view = McpResolveUtils.GetElement(doc, vid) as View;
                if (view == null) { res.Warnings.Add($"View {vid} not found."); continue; }
                if (!Viewport.CanAddViewToSheet(doc, sheet.Id, view.Id))
                {
                    res.Warnings.Add($"View {vid} cannot be placed (already on a sheet or unplaceable).");
                    continue;
                }
                double x = startX + (i % perRow) * stepX;
                double y = startY - (i / perRow) * stepY;
                var vp = Viewport.Create(doc, sheet.Id, view.Id, new XYZ(x, y, 0));
                if (vp != null) { res.PlacedViewports.Add(vp.Id.GetValue()); i++; }
            }
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Create Sheet";
    }
}
