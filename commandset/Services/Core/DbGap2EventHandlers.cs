using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Core
{
    // ─────────────────────────────────────────────────────────────────────────
    //  edit_curtain_grid  (CurtainGrid.AddGridLine)
    // ─────────────────────────────────────────────────────────────────────────
    public class EditCurtainGridEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public EditCurtainGridRequest Request { get; set; }
        public AIResult<EditCurtainGridResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new EditCurtainGridResult { HostId = Request.HostId };
            try
            {
                var host = McpResolveUtils.GetElement(doc, Request.HostId);
                CurtainGrid grid = ExtractGrid(host);
                if (grid == null) throw new ArgumentException($"Element {Request.HostId} has no curtain grid (expected a curtain wall or curtain system).");

                using (var tx = new Transaction(doc, "Edit Curtain Grid"))
                {
                    tx.Start();
                    foreach (var p in Request.UGridPoints ?? new List<JZPoint>())
                    {
                        try { grid.AddGridLine(true, JZPoint.ToXYZ(p), Request.OneSegmentOnly); res.AddedU++; }
                        catch (Exception ex) { res.Warnings.Add($"U grid at ({p.X},{p.Y},{p.Z}) skipped: {ex.Message}"); }
                    }
                    foreach (var p in Request.VGridPoints ?? new List<JZPoint>())
                    {
                        try { grid.AddGridLine(false, JZPoint.ToXYZ(p), Request.OneSegmentOnly); res.AddedV++; }
                        catch (Exception ex) { res.Warnings.Add($"V grid at ({p.X},{p.Y},{p.Z}) skipped: {ex.Message}"); }
                    }
                    tx.Commit();
                }

                Result = new AIResult<EditCurtainGridResult>
                {
                    Success = res.AddedU + res.AddedV > 0,
                    Message = $"Added {res.AddedU} U and {res.AddedV} V grid line(s) to element {Request.HostId}" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<EditCurtainGridResult> { Success = false, Message = $"Error: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        private CurtainGrid ExtractGrid(Element host)
        {
            switch (host)
            {
                case Wall w when w.CurtainGrid != null: return w.CurtainGrid;
                case CurtainSystem cs when cs.CurtainGrids != null && cs.CurtainGrids.Size > 0:
                    foreach (CurtainGrid g in cs.CurtainGrids) return g;
                    return null;
                default: return null;
            }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Edit Curtain Grid";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  place_image  (ImageType.Create + ImageInstance.Create)
    // ─────────────────────────────────────────────────────────────────────────
    public class PlaceImageEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public PlaceImageRequest Request { get; set; }
        public AIResult<PlaceImageResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new PlaceImageResult();
            try
            {
                if (string.IsNullOrWhiteSpace(Request.Path) || !System.IO.File.Exists(Request.Path))
                    throw new ArgumentException($"path '{Request.Path}' does not exist.");

                var view = McpResolveUtils.ResolveView(doc, uiDoc, Request.ViewId);
                if (view == null) throw new ArgumentException($"View '{Request.ViewId}' not found.");

                XYZ center = Request.Center != null
                    ? JZPoint.ToXYZ(Request.Center)
                    : (view.CropBox != null ? (view.CropBox.Min + view.CropBox.Max) * 0.5 : XYZ.Zero);

                using (var tx = new Transaction(doc, "Place Image"))
                {
                    tx.Start();
                    var typeOpts = new ImageTypeOptions(Request.Path, false, ImageTypeSource.Import);
                    var imageType = ImageType.Create(doc, typeOpts);
                    res.ImageTypeId = imageType.Id.GetValue();

                    var placeOpts = new ImagePlacementOptions(center, BoxPlacement.Center);
                    var inst = ImageInstance.Create(doc, view, imageType.Id, placeOpts);
                    res.ImageInstanceId = inst.Id.GetValue();
                    tx.Commit();
                }

                Result = new AIResult<PlaceImageResult>
                {
                    Success = true,
                    Message = $"Placed image '{System.IO.Path.GetFileName(Request.Path)}' (instance id {res.ImageInstanceId}) in '{view.Name}'.",
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<PlaceImageResult> { Success = false, Message = $"Error placing image: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Place Image";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  create_parts  (PartUtils.CreateParts)
    // ─────────────────────────────────────────────────────────────────────────
    public class CreatePartsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public CreatePartsRequest Request { get; set; }
        public AIResult<CreatePartsResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new CreatePartsResult();
            try
            {
                if (Request.ElementIds == null || Request.ElementIds.Count == 0)
                    throw new ArgumentException("elementIds is required.");

                var ids = new List<ElementId>();
                foreach (var v in Request.ElementIds)
                {
                    var el = McpResolveUtils.GetElement(doc, v);
                    if (el == null) { res.Warnings.Add($"Element {v} not found; skipped."); continue; }
                    if (!PartUtils.AreElementsValidForCreateParts(doc, new List<ElementId> { el.Id }))
                    { res.Warnings.Add($"Element {v} cannot be split into parts; skipped."); continue; }
                    ids.Add(el.Id);
                }
                if (ids.Count == 0) throw new ArgumentException("No elements are valid for CreateParts.");
                res.SourceCount = ids.Count;

                using (var tx = new Transaction(doc, "Create Parts"))
                {
                    tx.Start();
                    PartUtils.CreateParts(doc, ids);
                    doc.Regenerate();
                    foreach (var srcId in ids)
                    {
                        if (PartUtils.HasAssociatedParts(doc, srcId))
                        {
                            foreach (var partId in PartUtils.GetAssociatedParts(doc, srcId, false, false))
                                res.PartIds.Add(partId.GetValue());
                        }
                    }
                    tx.Commit();
                }

                res.PartCount = res.PartIds.Count;
                Result = new AIResult<CreatePartsResult>
                {
                    Success = res.PartCount > 0,
                    Message = $"Created {res.PartCount} part(s) from {res.SourceCount} element(s)" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<CreatePartsResult> { Success = false, Message = $"Error creating parts: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Create Parts";
    }
}
