using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Models.Views;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Views
{
    // ─────────────────────────────────────────────────────────────────────────
    //  create_schedule
    // ─────────────────────────────────────────────────────────────────────────
    public class CreateScheduleEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public ScheduleCreationRequest Request { get; set; }
        public AIResult<ScheduleCreationResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new ScheduleCreationResult();
            try
            {
                if (!McpResolveUtils.TryResolveBuiltInCategory(Request.Category, out BuiltInCategory bic))
                    throw new ArgumentException($"Unknown category '{Request.Category}'. Use a BuiltInCategory name like OST_Walls.");

                var catId = new ElementId(bic);
                ViewSchedule schedule;

                using (var tx = new Transaction(doc, "Create Schedule"))
                {
                    tx.Start();
                    schedule = ViewSchedule.CreateSchedule(doc, catId);
                    var def = schedule.Definition;
                    def.IsItemized = Request.Itemized;

                    // Map available schedulable fields by name once.
                    var schedulable = def.GetSchedulableFields();
                    var byName = new Dictionary<string, SchedulableField>(StringComparer.OrdinalIgnoreCase);
                    foreach (var sf in schedulable)
                    {
                        string name = sf.GetName(doc);
                        if (!string.IsNullOrWhiteSpace(name) && !byName.ContainsKey(name))
                            byName[name] = sf;
                    }

                    var requested = (Request.Fields != null && Request.Fields.Count > 0)
                        ? Request.Fields
                        : schedulable.Take(5).Select(sf => sf.GetName(doc)).ToList();

                    var addedByName = new Dictionary<string, ScheduleField>(StringComparer.OrdinalIgnoreCase);
                    foreach (var fname in requested)
                    {
                        if (byName.TryGetValue(fname, out var sf))
                        {
                            var added = def.AddField(sf);
                            res.Fields.Add(fname);
                            addedByName[fname] = added;
                        }
                        else res.Warnings.Add($"Field '{fname}' is not schedulable for this category; skipped.");
                    }
                    if (res.Fields.Count == 0)
                        res.Warnings.Add("No valid fields added; schedule created empty.");

                    // Sorting / grouping
                    if (!string.IsNullOrWhiteSpace(Request.SortBy) && addedByName.TryGetValue(Request.SortBy, out var sortField))
                    {
                        var order = Request.SortOrder?.Trim().Equals("Descending", StringComparison.OrdinalIgnoreCase) == true
                            ? ScheduleSortOrder.Descending : ScheduleSortOrder.Ascending;
                        def.AddSortGroupField(new ScheduleSortGroupField(sortField.FieldId, order));
                    }
                    else if (!string.IsNullOrWhiteSpace(Request.SortBy))
                        res.Warnings.Add($"sortBy '{Request.SortBy}' is not one of the added fields; skipped.");

                    if (!string.IsNullOrWhiteSpace(Request.Name))
                    {
                        try { schedule.Name = Request.Name; }
                        catch { res.Warnings.Add($"Could not set name '{Request.Name}' (maybe duplicate)."); }
                    }
                    tx.Commit();
                }

                res.ScheduleId = schedule.Id.GetValue();
                res.Name = schedule.Name;
                Result = new AIResult<ScheduleCreationResult>
                {
                    Success = true,
                    Message = $"Created schedule '{schedule.Name}' (id {res.ScheduleId}) with {res.Fields.Count} field(s)" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<ScheduleCreationResult>
                    { Success = false, Message = $"Error creating schedule: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Create Schedule";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  duplicate_view
    // ─────────────────────────────────────────────────────────────────────────
    public class DuplicateViewEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public DuplicateViewRequest Request { get; set; }
        public AIResult<DuplicateViewResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new DuplicateViewResult();
            try
            {
                var source = McpResolveUtils.ResolveView(doc, uiDoc, Request.ViewId);
                if (source == null) throw new ArgumentException($"View '{Request.ViewId}' not found.");

                ViewDuplicateOption opt = (Request.Option ?? "Duplicate").Trim().ToLowerInvariant() switch
                {
                    "withdetailing" => ViewDuplicateOption.WithDetailing,
                    "asdependent" => ViewDuplicateOption.AsDependent,
                    _ => ViewDuplicateOption.Duplicate
                };

                if (!source.CanViewBeDuplicated(opt))
                {
                    res.Warnings.Add($"View cannot be duplicated as {opt}; falling back to plain Duplicate.");
                    opt = ViewDuplicateOption.Duplicate;
                    if (!source.CanViewBeDuplicated(opt))
                        throw new InvalidOperationException("This view cannot be duplicated.");
                }

                ElementId newId;
                using (var tx = new Transaction(doc, "Duplicate View"))
                {
                    tx.Start();
                    newId = source.Duplicate(opt);
                    if (!string.IsNullOrWhiteSpace(Request.NewName) && opt != ViewDuplicateOption.AsDependent)
                    {
                        var nv = doc.GetElement(newId) as View;
                        try { if (nv != null) nv.Name = Request.NewName; }
                        catch { res.Warnings.Add($"Could not set name '{Request.NewName}' (maybe duplicate)."); }
                    }
                    tx.Commit();
                }

                var newView = doc.GetElement(newId) as View;
                res.NewViewId = newId.GetValue();
                res.Name = newView?.Name;
                Result = new AIResult<DuplicateViewResult>
                {
                    Success = true,
                    Message = $"Duplicated '{source.Name}' → '{res.Name}' (id {res.NewViewId}) as {opt}" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<DuplicateViewResult>
                    { Success = false, Message = $"Error duplicating view: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Duplicate View";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  set_view_properties
    // ─────────────────────────────────────────────────────────────────────────
    public class SetViewPropertiesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public SetViewPropertiesRequest Request { get; set; }
        public AIResult<SetViewPropertiesResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new SetViewPropertiesResult();
            try
            {
                var view = McpResolveUtils.ResolveView(doc, uiDoc, Request.ViewId);
                if (view == null) throw new ArgumentException($"View '{Request.ViewId}' not found.");
                res.ViewId = view.Id.GetValue();

                using (var tx = new Transaction(doc, "Set View Properties"))
                {
                    tx.Start();

                    if (!string.IsNullOrWhiteSpace(Request.TemplateName))
                    {
                        var tmpl = McpResolveUtils.ResolveView(doc, uiDoc, Request.TemplateName)
                                   ?? FindTemplate(Request.TemplateName);
                        if (tmpl != null && tmpl.IsTemplate)
                        {
                            try { view.ViewTemplateId = tmpl.Id; res.Applied.Add($"template={tmpl.Name}"); }
                            catch { res.Warnings.Add($"Could not apply template '{Request.TemplateName}'."); }
                        }
                        else res.Warnings.Add($"View template '{Request.TemplateName}' not found.");
                    }

                    if (Request.Scale.HasValue)
                    {
                        try { view.Scale = Request.Scale.Value; res.Applied.Add($"scale=1:{Request.Scale.Value}"); }
                        catch { res.Warnings.Add("Scale is controlled by the template or not settable on this view."); }
                    }

                    if (Request.CropVisible.HasValue)
                    {
                        try { view.CropBoxVisible = Request.CropVisible.Value; res.Applied.Add($"cropVisible={Request.CropVisible.Value}"); }
                        catch { res.Warnings.Add("Crop visibility not settable on this view."); }
                    }
                    if (Request.CropActive.HasValue)
                    {
                        try { view.CropBoxActive = Request.CropActive.Value; res.Applied.Add($"cropActive={Request.CropActive.Value}"); }
                        catch { res.Warnings.Add("Crop active not settable on this view."); }
                    }

                    if (!string.IsNullOrWhiteSpace(Request.DetailLevel) &&
                        Enum.TryParse(Request.DetailLevel, true, out ViewDetailLevel dl))
                    {
                        try { view.DetailLevel = dl; res.Applied.Add($"detailLevel={dl}"); }
                        catch { res.Warnings.Add("Detail level controlled by template or not settable."); }
                    }
                    else if (!string.IsNullOrWhiteSpace(Request.DetailLevel))
                        res.Warnings.Add($"Unknown detailLevel '{Request.DetailLevel}'.");

                    if (!string.IsNullOrWhiteSpace(Request.Discipline) &&
                        Enum.TryParse(Request.Discipline, true, out ViewDiscipline disc))
                    {
                        try { view.Discipline = disc; res.Applied.Add($"discipline={disc}"); }
                        catch { res.Warnings.Add("Discipline controlled by template or not settable."); }
                    }
                    else if (!string.IsNullOrWhiteSpace(Request.Discipline))
                        res.Warnings.Add($"Unknown discipline '{Request.Discipline}'.");

                    tx.Commit();
                }

                Result = new AIResult<SetViewPropertiesResult>
                {
                    Success = res.Applied.Count > 0 || res.Warnings.Count == 0,
                    Message = $"Updated view '{view.Name}': {(res.Applied.Count > 0 ? string.Join(", ", res.Applied) : "nothing changed")}" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<SetViewPropertiesResult>
                    { Success = false, Message = $"Error setting view properties: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        private View FindTemplate(string nameOrId)
        {
            var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().Where(v => v.IsTemplate).ToList();
            if (long.TryParse(nameOrId, out long id))
            {
                var byId = views.FirstOrDefault(v => v.Id.GetValue() == id);
                if (byId != null) return byId;
            }
            return views.FirstOrDefault(v => v.Name.Equals(nameOrId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Set View Properties";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  apply_filter_to_view
    // ─────────────────────────────────────────────────────────────────────────
    public class ApplyFilterToViewEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public ApplyFilterToViewRequest Request { get; set; }
        public AIResult<ApplyFilterToViewResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new ApplyFilterToViewResult();
            try
            {
                var view = McpResolveUtils.ResolveView(doc, uiDoc, Request.ViewId);
                if (view == null) throw new ArgumentException($"View '{Request.ViewId}' not found.");
                res.ViewId = view.Id.GetValue();

                var filters = new FilteredElementCollector(doc).OfClass(typeof(ParameterFilterElement))
                    .Cast<ParameterFilterElement>().ToList();
                ParameterFilterElement filter = null;
                if (long.TryParse(Request.FilterName, out long fid))
                    filter = filters.FirstOrDefault(f => f.Id.GetValue() == fid);
                filter ??= filters.FirstOrDefault(f => f.Name.Equals(Request.FilterName?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (filter == null) throw new ArgumentException($"Filter '{Request.FilterName}' not found. Create it first with create_view_filter.");
                res.FilterId = filter.Id.GetValue();

                using (var tx = new Transaction(doc, "Apply Filter To View"))
                {
                    tx.Start();
                    if (!view.GetFilters().Contains(filter.Id))
                    {
                        if (!view.IsFilterApplied(filter.Id))
                            view.AddFilter(filter.Id);
                    }
                    view.SetFilterVisibility(filter.Id, Request.Visible);

                    var ogs = new OverrideGraphicSettings();
                    bool anyOverride = false;
                    if (Request.ColorRgb != null && Request.ColorRgb.Count == 3)
                    {
                        var c = new Color((byte)Request.ColorRgb[0], (byte)Request.ColorRgb[1], (byte)Request.ColorRgb[2]);
                        ogs.SetProjectionLineColor(c);
                        ogs.SetCutLineColor(c);
                        ogs.SetSurfaceForegroundPatternColor(c);
                        anyOverride = true;
                    }
                    if (Request.LineWeight.HasValue)
                    {
                        int w = Math.Max(1, Math.Min(16, Request.LineWeight.Value));
                        ogs.SetProjectionLineWeight(w);
                        anyOverride = true;
                    }
                    if (Request.Transparency.HasValue)
                    {
                        ogs.SetSurfaceTransparency(Math.Max(0, Math.Min(100, Request.Transparency.Value)));
                        anyOverride = true;
                    }
                    if (anyOverride) view.SetFilterOverrides(filter.Id, ogs);
                    tx.Commit();
                }

                Result = new AIResult<ApplyFilterToViewResult>
                {
                    Success = true,
                    Message = $"Applied filter '{filter.Name}' to view '{view.Name}' (visible={Request.Visible})" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<ApplyFilterToViewResult>
                    { Success = false, Message = $"Error applying filter: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Apply Filter To View";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  place_text
    // ─────────────────────────────────────────────────────────────────────────
    public class PlaceTextEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public PlaceTextRequest Request { get; set; }
        public AIResult<PlaceTextResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new PlaceTextResult();
            try
            {
                if (string.IsNullOrWhiteSpace(Request.Text)) throw new ArgumentException("text is required.");
                if (Request.Position == null) throw new ArgumentException("position is required.");

                var view = McpResolveUtils.ResolveView(doc, uiDoc, Request.ViewId);
                if (view == null) throw new ArgumentException($"View '{Request.ViewId}' not found.");

                var ttType = McpResolveUtils.ResolveType(doc, Request.TextTypeName, typeof(TextNoteType))
                             ?? McpResolveUtils.FirstTypeOfClass(doc, typeof(TextNoteType));
                if (ttType == null) throw new InvalidOperationException("No TextNoteType available in the document.");

                XYZ pos = JZPoint.ToXYZ(Request.Position);
                TextNote note;
                using (var tx = new Transaction(doc, "Place Text"))
                {
                    tx.Start();
                    if (Request.Width > 0)
                    {
                        var opts = new TextNoteOptions(ttType.Id);
                        note = TextNote.Create(doc, view.Id, pos, McpResolveUtils.MmToFt(Request.Width), Request.Text, opts);
                    }
                    else
                    {
                        note = TextNote.Create(doc, view.Id, pos, Request.Text, ttType.Id);
                    }
                    tx.Commit();
                }

                res.TextNoteId = note.Id.GetValue();
                Result = new AIResult<PlaceTextResult>
                {
                    Success = true,
                    Message = $"Placed text note (id {res.TextNoteId}) in view '{view.Name}'.",
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<PlaceTextResult>
                    { Success = false, Message = $"Error placing text: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Place Text";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  create_filled_region
    // ─────────────────────────────────────────────────────────────────────────
    public class FilledRegionEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public FilledRegionRequest Request { get; set; }
        public AIResult<FilledRegionResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new FilledRegionResult();
            try
            {
                var view = McpResolveUtils.ResolveView(doc, uiDoc, Request.ViewId);
                if (view == null) throw new ArgumentException($"View '{Request.ViewId}' not found.");

                var loop = McpResolveUtils.BuildCurveLoop(Request.Boundary, out string w);
                if (loop == null) throw new ArgumentException(w ?? "Invalid boundary.");

                var frType = McpResolveUtils.ResolveType(doc, Request.FilledRegionTypeName, typeof(FilledRegionType))
                             ?? McpResolveUtils.FirstTypeOfClass(doc, typeof(FilledRegionType));
                if (frType == null) throw new InvalidOperationException("No FilledRegionType available in the document.");

                FilledRegion fr;
                using (var tx = new Transaction(doc, "Create Filled Region"))
                {
                    tx.Start();
                    fr = FilledRegion.Create(doc, frType.Id, view.Id, new List<CurveLoop> { loop });
                    tx.Commit();
                }

                res.FilledRegionId = fr.Id.GetValue();
                Result = new AIResult<FilledRegionResult>
                {
                    Success = true,
                    Message = $"Created filled region (id {res.FilledRegionId}) in view '{view.Name}'.",
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<FilledRegionResult>
                    { Success = false, Message = $"Error creating filled region: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Create Filled Region";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  create_revision (+ optional cloud)
    // ─────────────────────────────────────────────────────────────────────────
    public class RevisionEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public RevisionRequest Request { get; set; }
        public AIResult<RevisionResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new RevisionResult();
            try
            {
                Revision rev;
                using (var tx = new Transaction(doc, "Create Revision"))
                {
                    tx.Start();
                    rev = Revision.Create(doc);
                    rev.Description = Request.Description ?? "";
                    if (!string.IsNullOrWhiteSpace(Request.IssuedTo)) rev.IssuedTo = Request.IssuedTo;
                    if (!string.IsNullOrWhiteSpace(Request.IssuedBy)) rev.IssuedBy = Request.IssuedBy;
                    rev.Issued = Request.Issued;

                    // Optional revision cloud
                    if (!string.IsNullOrWhiteSpace(Request.CloudViewId) && Request.CloudBoundary != null && Request.CloudBoundary.Count >= 3)
                    {
                        var view = McpResolveUtils.ResolveView(doc, uiDoc, Request.CloudViewId);
                        if (view == null) res.Warnings.Add($"Cloud view '{Request.CloudViewId}' not found; revision created without cloud.");
                        else
                        {
                            var loop = McpResolveUtils.BuildCurveLoop(Request.CloudBoundary, out string w);
                            if (loop == null) res.Warnings.Add(w ?? "Invalid cloud boundary; revision created without cloud.");
                            else
                            {
                                var curves = loop.Cast<Curve>().ToList();
                                var cloud = RevisionCloud.Create(doc, view, rev.Id, curves);
                                res.CloudId = cloud.Id.GetValue();
                            }
                        }
                    }
                    tx.Commit();
                }

                res.RevisionId = rev.Id.GetValue();
                Result = new AIResult<RevisionResult>
                {
                    Success = true,
                    Message = $"Created revision (id {res.RevisionId})" +
                              (res.CloudId != 0 ? $" with cloud (id {res.CloudId})" : "") +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<RevisionResult>
                    { Success = false, Message = $"Error creating revision: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Create Revision";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  create_legend  (duplicate an existing legend; optionally place on a sheet)
    // ─────────────────────────────────────────────────────────────────────────
    public class CreateLegendEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public CreateLegendRequest Request { get; set; }
        public AIResult<CreateLegendResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new CreateLegendResult();
            try
            {
                // There is no public API to create a legend from scratch; duplicate an existing one.
                var legend = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
                    .FirstOrDefault(v => v.ViewType == ViewType.Legend && !v.IsTemplate);
                if (legend == null)
                    throw new InvalidOperationException("No existing legend view to duplicate. Create one legend manually first; this tool duplicates it.");

                ElementId newId;
                using (var tx = new Transaction(doc, "Create Legend"))
                {
                    tx.Start();
                    newId = legend.Duplicate(ViewDuplicateOption.Duplicate);
                    var nv = doc.GetElement(newId) as View;
                    if (nv != null)
                    {
                        if (!string.IsNullOrWhiteSpace(Request.Name))
                        {
                            try { nv.Name = Request.Name; } catch { res.Warnings.Add($"Could not set name '{Request.Name}'."); }
                        }
                        if (Request.Scale.HasValue) { try { nv.Scale = Request.Scale.Value; } catch { } }
                    }

                    if (Request.SheetId != 0)
                    {
                        var sheet = doc.GetElement(McpResolveUtils.ToElementId(Request.SheetId)) as ViewSheet;
                        if (sheet == null) res.Warnings.Add($"Sheet id {Request.SheetId} not found; legend created but not placed.");
                        else if (Viewport.CanAddViewToSheet(doc, sheet.Id, newId))
                        {
                            var outline = sheet.Outline; // BoundingBoxUV in sheet space
                            var center = new XYZ((outline.Min.U + outline.Max.U) * 0.5,
                                                 (outline.Min.V + outline.Max.V) * 0.5, 0);
                            var vp = Viewport.Create(doc, sheet.Id, newId, center);
                            res.ViewportId = vp.Id.GetValue();
                        }
                        else res.Warnings.Add("Legend could not be added to the sheet.");
                    }
                    tx.Commit();
                }

                var view = doc.GetElement(newId) as View;
                res.LegendViewId = newId.GetValue();
                res.Name = view?.Name;
                Result = new AIResult<CreateLegendResult>
                {
                    Success = true,
                    Message = $"Created legend '{res.Name}' (id {res.LegendViewId})" +
                              (res.ViewportId != 0 ? " and placed on sheet" : "") +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<CreateLegendResult>
                    { Success = false, Message = $"Error creating legend: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Create Legend";
    }
}
