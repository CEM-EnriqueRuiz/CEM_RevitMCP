using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    /// <summary>
    ///     Creates views of several kinds via the modern ViewX.Create factories,
    ///     resolving view family types and levels by name so the AI can stay generic.
    /// </summary>
    public class CreateViewEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public ViewCreationRequest Request { get; set; }
        public AIResult<ViewCreationResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new ViewCreationResult { ViewType = Request.ViewType };
            try
            {
                View view = null;
                string kind = (Request.ViewType ?? "FloorPlan").Trim().ToLowerInvariant();

                using (var tx = new Transaction(doc, "Create View"))
                {
                    tx.Start();
                    switch (kind)
                    {
                        case "floorplan":   view = CreatePlan(ViewFamily.FloorPlan, res); break;
                        case "ceilingplan": view = CreatePlan(ViewFamily.CeilingPlan, res); break;
                        case "areaplan":    view = CreatePlan(ViewFamily.AreaPlan, res); break;
                        case "3d":          view = Create3D(res); break;
                        case "drafting":    view = CreateDrafting(res); break;
                        case "section":
                        case "elevation":   view = CreateSection(res); break;
                        default:
                            res.Warnings.Add($"Unknown viewType '{Request.ViewType}', defaulting to FloorPlan.");
                            view = CreatePlan(ViewFamily.FloorPlan, res);
                            break;
                    }

                    if (view != null)
                    {
                        if (!string.IsNullOrWhiteSpace(Request.Name))
                        {
                            try { view.Name = Request.Name; } catch { res.Warnings.Add($"Could not set name '{Request.Name}' (maybe duplicate)."); }
                        }
                        if (Request.Scale.HasValue) { try { view.Scale = Request.Scale.Value; } catch { } }
                        if (!string.IsNullOrWhiteSpace(Request.TemplateName))
                        {
                            var tmpl = McpResolveUtils.ResolveView(doc, null, Request.TemplateName);
                            if (tmpl != null && tmpl.IsTemplate) { try { view.ViewTemplateId = tmpl.Id; } catch { } }
                            else res.Warnings.Add($"View template '{Request.TemplateName}' not found.");
                        }
                    }
                    tx.Commit();
                }

                if (view == null)
                {
                    Result = new AIResult<ViewCreationResult>
                        { Success = false, Message = "View could not be created.", Response = res };
                    _resetEvent.Set();
                    return;
                }

                res.ViewId = view.Id.GetValue();
                res.Name = view.Name;
                Result = new AIResult<ViewCreationResult>
                {
                    Success = true,
                    Message = $"Created {Request.ViewType} view '{view.Name}' (id {res.ViewId})" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<ViewCreationResult>
                    { Success = false, Message = $"Error creating view: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        private ElementId ResolveVft(ViewFamily family)
        {
            var vfts = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>().ToList();

            if (!string.IsNullOrWhiteSpace(Request.ViewFamilyTypeName))
            {
                var byName = vfts.FirstOrDefault(v => v.Name.Equals(Request.ViewFamilyTypeName, StringComparison.OrdinalIgnoreCase));
                if (byName != null) return byName.Id;
            }
            var byFamily = vfts.FirstOrDefault(v => v.ViewFamily == family);
            return byFamily?.Id ?? ElementId.InvalidElementId;
        }

        private View CreatePlan(ViewFamily family, ViewCreationResult res)
        {
            Level level = McpResolveUtils.ResolveLevel(doc, Request.Level);
            if (level == null) { res.Warnings.Add("No level available for plan view."); return null; }

            ElementId vftId = ResolveVft(family);
            if (vftId == ElementId.InvalidElementId) { res.Warnings.Add($"No ViewFamilyType for {family}."); return null; }

            return ViewPlan.Create(doc, vftId, level.Id);
        }

        private View Create3D(ViewCreationResult res)
        {
            ElementId vftId = ResolveVft(ViewFamily.ThreeDimensional);
            if (vftId == ElementId.InvalidElementId) { res.Warnings.Add("No 3D ViewFamilyType."); return null; }
            return View3D.CreateIsometric(doc, vftId);
        }

        private View CreateDrafting(ViewCreationResult res)
        {
            ElementId vftId = ResolveVft(ViewFamily.Drafting);
            if (vftId == ElementId.InvalidElementId) { res.Warnings.Add("No Drafting ViewFamilyType."); return null; }
            return ViewDrafting.Create(doc, vftId);
        }

        private View CreateSection(ViewCreationResult res)
        {
            ElementId vftId = ResolveVft(ViewFamily.Section);
            if (vftId == ElementId.InvalidElementId) { res.Warnings.Add("No Section ViewFamilyType."); return null; }
            if (Request.Start == null || Request.End == null)
            {
                res.Warnings.Add("Section needs start and end points; cannot create.");
                return null;
            }

            XYZ p1 = JZPoint.ToXYZ(Request.Start);
            XYZ p2 = JZPoint.ToXYZ(Request.End);
            XYZ dir = (p2 - p1).Normalize();
            XYZ up = XYZ.BasisZ;
            XYZ viewDir = dir.CrossProduct(up).Normalize();

            double depth = McpResolveUtils.MmToFt(Request.Depth);
            double height = McpResolveUtils.MmToFt(Request.Height);
            double length = p1.DistanceTo(p2);
            XYZ mid = (p1 + p2) * 0.5;

            var t = Transform.Identity;
            t.Origin = mid;
            t.BasisX = dir;
            t.BasisY = up;
            t.BasisZ = viewDir;

            var box = new BoundingBoxXYZ { Transform = t };
            box.Min = new XYZ(-length / 2, -height / 2, 0);
            box.Max = new XYZ(length / 2, height / 2, depth);

            return ViewSection.CreateSection(doc, vftId, box);
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Create View";
    }
}
