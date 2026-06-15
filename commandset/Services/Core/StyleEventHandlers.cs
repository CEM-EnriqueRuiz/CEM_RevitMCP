using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Core
{
    // ─────────────────────────────────────────────────────────────────────────
    //  manage_line_pattern  (LinePatternElement)
    // ─────────────────────────────────────────────────────────────────────────
    public class LinePatternEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public LinePatternRequest Request { get; set; }
        public AIResult<LinePatternResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new LinePatternResult { Action = Request.Action };
            try
            {
                string action = (Request.Action ?? "list").Trim().ToLowerInvariant();
                if (action == "list")
                {
                    foreach (var lpe in new FilteredElementCollector(doc).OfClass(typeof(LinePatternElement)).Cast<LinePatternElement>())
                        res.Patterns.Add(new NamedIdInfo(lpe.Name, lpe.Id.GetValue()));
                    Result = new AIResult<LinePatternResult>
                    {
                        Success = true,
                        Message = $"Found {res.Patterns.Count} line pattern(s).",
                        Response = res
                    };
                    return;
                }

                // create
                if (string.IsNullOrWhiteSpace(Request.Name)) throw new ArgumentException("name is required to create a line pattern.");
                if (Request.SegmentsMm == null || Request.SegmentsMm.Count < 2)
                    throw new ArgumentException("segmentsMm needs at least one dash and one gap (e.g. [5,2]).");

                var existing = new FilteredElementCollector(doc).OfClass(typeof(LinePatternElement))
                    .Cast<LinePatternElement>()
                    .FirstOrDefault(p => p.Name.Equals(Request.Name.Trim(), StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    res.PatternId = existing.Id.GetValue();
                    res.Warnings.Add($"Line pattern '{Request.Name}' already exists; reusing it.");
                    Result = new AIResult<LinePatternResult>
                        { Success = true, Message = $"Line pattern '{Request.Name}' already exists (id {res.PatternId}).", Response = res };
                    return;
                }

                var segs = new List<LinePatternSegment>();
                for (int i = 0; i < Request.SegmentsMm.Count; i++)
                {
                    var kind = (i % 2 == 0) ? LinePatternSegmentType.Dash : LinePatternSegmentType.Space;
                    segs.Add(new LinePatternSegment(kind, McpResolveUtils.MmToFt(Math.Abs(Request.SegmentsMm[i]))));
                }

                using (var tx = new Transaction(doc, "Create Line Pattern"))
                {
                    tx.Start();
                    var lp = new LinePattern(Request.Name) ;
                    lp.SetSegments(segs);
                    var lpe = LinePatternElement.Create(doc, lp);
                    res.PatternId = lpe.Id.GetValue();
                    tx.Commit();
                }

                Result = new AIResult<LinePatternResult>
                {
                    Success = true,
                    Message = $"Created line pattern '{Request.Name}' (id {res.PatternId}).",
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<LinePatternResult>
                    { Success = false, Message = $"Error in manage_line_pattern: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Manage Line Pattern";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  manage_fill_pattern  (FillPatternElement)
    // ─────────────────────────────────────────────────────────────────────────
    public class FillPatternEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public FillPatternRequest Request { get; set; }
        public AIResult<FillPatternResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new FillPatternResult { Action = Request.Action };
            try
            {
                string action = (Request.Action ?? "list").Trim().ToLowerInvariant();
                if (action == "list")
                {
                    foreach (var fpe in new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement)).Cast<FillPatternElement>())
                        res.Patterns.Add(new NamedIdInfo(fpe.Name, fpe.Id.GetValue()));
                    Result = new AIResult<FillPatternResult>
                        { Success = true, Message = $"Found {res.Patterns.Count} fill pattern(s).", Response = res };
                    return;
                }

                if (string.IsNullOrWhiteSpace(Request.Name)) throw new ArgumentException("name is required to create a fill pattern.");

                var target = Request.Target?.Trim().Equals("Model", StringComparison.OrdinalIgnoreCase) == true
                    ? FillPatternTarget.Model : FillPatternTarget.Drafting;

                var existing = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement))
                    .Cast<FillPatternElement>()
                    .FirstOrDefault(p => p.Name.Equals(Request.Name.Trim(), StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    res.PatternId = existing.Id.GetValue();
                    res.Warnings.Add($"Fill pattern '{Request.Name}' already exists; reusing it.");
                    Result = new AIResult<FillPatternResult>
                        { Success = true, Message = $"Fill pattern '{Request.Name}' already exists (id {res.PatternId}).", Response = res };
                    return;
                }

                using (var tx = new Transaction(doc, "Create Fill Pattern"))
                {
                    tx.Start();
                    double angleRad = Request.Angle * Math.PI / 180.0;
                    double spacingFt = McpResolveUtils.MmToFt(Math.Max(0.1, Request.SpacingMm));
                    var fp = new FillPattern(Request.Name, target, FillPatternHostOrientation.ToView,
                        angleRad, spacingFt);
                    var fpe = FillPatternElement.Create(doc, fp);
                    res.PatternId = fpe.Id.GetValue();
                    tx.Commit();
                }

                Result = new AIResult<FillPatternResult>
                {
                    Success = true,
                    Message = $"Created {target} fill pattern '{Request.Name}' (id {res.PatternId}).",
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<FillPatternResult>
                    { Success = false, Message = $"Error in manage_fill_pattern: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Manage Fill Pattern";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  set_object_styles  (Category line weight / color / pattern)
    // ─────────────────────────────────────────────────────────────────────────
    public class ObjectStylesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public ObjectStylesRequest Request { get; set; }
        public AIResult<ObjectStylesResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new ObjectStylesResult { Category = Request.Category };
            try
            {
                var cat = McpResolveUtils.ResolveCategory(doc, Request.Category);
                if (cat == null) throw new ArgumentException($"Category '{Request.Category}' not found.");

                using (var tx = new Transaction(doc, "Set Object Styles"))
                {
                    tx.Start();

                    if (Request.ProjectionLineWeight.HasValue)
                    {
                        int w = Math.Max(1, Math.Min(16, Request.ProjectionLineWeight.Value));
                        try { cat.SetLineWeight(w, GraphicsStyleType.Projection); res.Applied.Add($"projectionLineWeight={w}"); }
                        catch { res.Warnings.Add("Projection line weight not settable for this category."); }
                    }
                    if (Request.CutLineWeight.HasValue)
                    {
                        int w = Math.Max(1, Math.Min(16, Request.CutLineWeight.Value));
                        try { cat.SetLineWeight(w, GraphicsStyleType.Cut); res.Applied.Add($"cutLineWeight={w}"); }
                        catch { res.Warnings.Add("Cut line weight not settable for this category (not cuttable)."); }
                    }
                    if (Request.ColorRgb != null && Request.ColorRgb.Count == 3)
                    {
                        try
                        {
                            cat.LineColor = new Color((byte)Request.ColorRgb[0], (byte)Request.ColorRgb[1], (byte)Request.ColorRgb[2]);
                            res.Applied.Add("lineColor");
                        }
                        catch { res.Warnings.Add("Line color not settable for this category."); }
                    }
                    if (!string.IsNullOrWhiteSpace(Request.LinePatternName))
                    {
                        var lpe = ResolveLinePattern(Request.LinePatternName);
                        if (lpe != null)
                        {
                            try { cat.SetLinePatternId(lpe.Id, GraphicsStyleType.Projection); res.Applied.Add($"linePattern={lpe.Name}"); }
                            catch { res.Warnings.Add("Line pattern not settable for this category."); }
                        }
                        else res.Warnings.Add($"Line pattern '{Request.LinePatternName}' not found.");
                    }

                    tx.Commit();
                }

                Result = new AIResult<ObjectStylesResult>
                {
                    Success = res.Applied.Count > 0,
                    Message = $"Object styles for '{cat.Name}': {(res.Applied.Count > 0 ? string.Join(", ", res.Applied) : "nothing changed")}" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<ObjectStylesResult>
                    { Success = false, Message = $"Error in set_object_styles: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        private LinePatternElement ResolveLinePattern(string nameOrId)
        {
            var all = new FilteredElementCollector(doc).OfClass(typeof(LinePatternElement)).Cast<LinePatternElement>().ToList();
            if (long.TryParse(nameOrId, out long id))
            {
                var byId = all.FirstOrDefault(p => p.Id.GetValue() == id);
                if (byId != null) return byId;
            }
            return all.FirstOrDefault(p => p.Name.Equals(nameOrId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Set Object Styles";
    }
}
