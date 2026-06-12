using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    /// <summary>
    ///     Generic tagging: tags explicit elements or all elements of given categories
    ///     in a view, choosing a sensible tag type per element when none is specified.
    /// </summary>
    public class TagElementsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public TagElementsRequest Request { get; set; }
        public AIResult<TagElementsResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new TagElementsResult();
            try
            {
                View view = McpResolveUtils.ResolveView(doc, uiDoc, Request.ViewId.ToString());
                if (view == null)
                {
                    Result = new AIResult<TagElementsResult> { Success = false, Message = "View not found." };
                    _resetEvent.Set();
                    return;
                }

                // Resolve target elements
                var targets = new List<Element>();
                if (Request.ElementIds != null && Request.ElementIds.Count > 0)
                {
                    foreach (long id in Request.ElementIds)
                    {
                        var el = McpResolveUtils.GetElement(doc, id);
                        if (el != null) targets.Add(el);
                    }
                }
                else
                {
                    var bics = new List<BuiltInCategory>();
                    foreach (var c in Request.Categories)
                        if (McpResolveUtils.TryResolveBuiltInCategory(c, out var bic)) bics.Add(bic);

                    foreach (var bic in bics)
                    {
                        var collected = new FilteredElementCollector(doc, view.Id)
                            .OfCategory(bic).WhereElementIsNotElementType().ToElements();
                        targets.AddRange(collected);
                    }
                }

                if (targets.Count == 0)
                {
                    Result = new AIResult<TagElementsResult>
                        { Success = false, Message = "No elements matched to tag.", Response = res };
                    _resetEvent.Set();
                    return;
                }

                TagOrientation orient = (Request.Orientation ?? "Horizontal").Trim()
                    .Equals("Vertical", StringComparison.OrdinalIgnoreCase)
                    ? TagOrientation.Vertical : TagOrientation.Horizontal;

                using (var tx = new Transaction(doc, "Tag Elements"))
                {
                    tx.Start();
                    foreach (var el in targets)
                    {
                        try
                        {
                            ElementId tagTypeId = ResolveTagType(el);
                            XYZ pt = GetTagPoint(el);
                            var reference = new Reference(el);

                            var tag = IndependentTag.Create(
                                doc, tagTypeId, view.Id, reference,
                                Request.AddLeader, orient, pt);

                            if (tag != null)
                            {
                                res.NewTagIds.Add(tag.Id.GetValue());
                                res.TaggedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (res.Warnings.Count < 10)
                                res.Warnings.Add($"Element {el.Id.GetValue()}: {ex.Message}");
                        }
                    }
                    tx.Commit();
                }

                Result = new AIResult<TagElementsResult>
                {
                    Success = res.TaggedCount > 0,
                    Message = $"Tagged {res.TaggedCount}/{targets.Count} element(s)" +
                              (res.Warnings.Count > 0 ? $". ⚠ {res.Warnings.Count} skipped (e.g. {res.Warnings[0]})" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<TagElementsResult>
                    { Success = false, Message = $"Error tagging: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        private ElementId _explicitTagType = null;
        private bool _resolvedExplicit = false;

        private ElementId ResolveTagType(Element el)
        {
            // Explicit type requested by the AI
            if (!_resolvedExplicit && !string.IsNullOrWhiteSpace(Request.TagTypeName))
            {
                var t = McpResolveUtils.ResolveType(doc, Request.TagTypeName);
                _explicitTagType = t?.Id;
                _resolvedExplicit = true;
            }
            if (_explicitTagType != null && _explicitTagType != ElementId.InvalidElementId)
                return _explicitTagType;

            // Otherwise pick the first tag FamilySymbol whose category tags this element's category
            var catId = el.Category?.Id;
            var tagSymbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs =>
                    fs.Category != null &&
                    fs.Category.CategoryType == CategoryType.Annotation);

            // Try to match a tag whose name references the category (best effort)
            if (catId != null)
            {
                var match = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
                    .FirstOrDefault(fs => fs.Category != null &&
                        fs.Category.Name.IndexOf(el.Category.Name, StringComparison.OrdinalIgnoreCase) >= 0);
                if (match != null) tagSymbol = match;
            }

            if (tagSymbol != null)
            {
                if (!tagSymbol.IsActive) { tagSymbol.Activate(); doc.Regenerate(); }
                return tagSymbol.Id;
            }
            return ElementId.InvalidElementId;
        }

        private XYZ GetTagPoint(Element el)
        {
            try
            {
                var bb = el.get_BoundingBox(null);
                if (bb != null) return (bb.Min + bb.Max) * 0.5;
                if (el.Location is LocationPoint lp) return lp.Point;
                if (el.Location is LocationCurve lc) return lc.Curve.Evaluate(0.5, true);
            }
            catch { }
            return XYZ.Zero;
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Tag Elements";
    }
}
