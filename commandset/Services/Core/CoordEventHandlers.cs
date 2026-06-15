using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Core
{
    // ─────────────────────────────────────────────────────────────────────────
    //  coord_manage_worksets
    // ─────────────────────────────────────────────────────────────────────────
    public class ManageWorksetsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public ManageWorksetsRequest Request { get; set; }
        public AIResult<ManageWorksetsResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new ManageWorksetsResult { Action = Request.Action };
            try
            {
                if (!doc.IsWorkshared)
                {
                    Result = new AIResult<ManageWorksetsResult>
                    {
                        Success = false,
                        Message = "This document is not workshared. Worksets require a workshared (central/local) model.",
                        Response = res
                    };
                    _resetEvent.Set();
                    return;
                }

                string action = (Request.Action ?? "list").Trim().ToLowerInvariant();
                switch (action)
                {
                    case "list":
                        FillWorksets(res);
                        Result = new AIResult<ManageWorksetsResult>
                            { Success = true, Message = $"Found {res.Worksets.Count} user workset(s).", Response = res };
                        break;

                    case "create":
                        if (string.IsNullOrWhiteSpace(Request.Name)) throw new ArgumentException("name is required to create a workset.");
                        if (!WorksetTable.IsWorksetNameUnique(doc, Request.Name))
                        {
                            var existing = GetUserWorksets().FirstOrDefault(w => w.Name.Equals(Request.Name, StringComparison.OrdinalIgnoreCase));
                            if (existing != null)
                            {
                                res.WorksetId = existing.Id.IntegerValue;
                                res.Warnings.Add($"Workset '{Request.Name}' already exists; reusing it.");
                            }
                        }
                        else
                        {
                            using (var tx = new Transaction(doc, "Create Workset"))
                            {
                                tx.Start();
                                var ws = Workset.Create(doc, Request.Name);
                                res.WorksetId = ws.Id.IntegerValue;
                                tx.Commit();
                            }
                        }
                        FillWorksets(res);
                        Result = new AIResult<ManageWorksetsResult>
                            { Success = true, Message = $"Workset '{Request.Name}' ready (id {res.WorksetId}).", Response = res };
                        break;

                    case "set_active":
                        {
                            var ws = GetUserWorksets().FirstOrDefault(w => w.Name.Equals(Request.Name, StringComparison.OrdinalIgnoreCase));
                            if (ws == null) throw new ArgumentException($"Workset '{Request.Name}' not found.");
                            doc.GetWorksetTable().SetActiveWorksetId(ws.Id);
                            res.WorksetId = ws.Id.IntegerValue;
                            Result = new AIResult<ManageWorksetsResult>
                                { Success = true, Message = $"Active workset set to '{ws.Name}'.", Response = res };
                        }
                        break;

                    case "assign":
                        {
                            var ws = GetUserWorksets().FirstOrDefault(w => w.Name.Equals(Request.Name, StringComparison.OrdinalIgnoreCase));
                            if (ws == null) throw new ArgumentException($"Workset '{Request.Name}' not found.");
                            res.WorksetId = ws.Id.IntegerValue;
                            using (var tx = new Transaction(doc, "Assign Workset"))
                            {
                                tx.Start();
                                foreach (var idVal in Request.ElementIds ?? new List<long>())
                                {
                                    var el = McpResolveUtils.GetElement(doc, idVal);
                                    if (el == null) { res.Warnings.Add($"Element {idVal} not found; skipped."); continue; }
                                    var p = el.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
                                    if (p != null && !p.IsReadOnly) { p.Set(ws.Id.IntegerValue); res.AssignedCount++; }
                                    else res.Warnings.Add($"Element {idVal}: workset not assignable; skipped.");
                                }
                                tx.Commit();
                            }
                            Result = new AIResult<ManageWorksetsResult>
                                { Success = res.AssignedCount > 0, Message = $"Assigned {res.AssignedCount} element(s) to workset '{ws.Name}'" +
                                  (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."), Response = res };
                        }
                        break;

                    default:
                        throw new ArgumentException($"Unknown action '{Request.Action}'. Use list|create|set_active|assign.");
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<ManageWorksetsResult>
                    { Success = false, Message = $"Error in coord_manage_worksets: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        private IList<Workset> GetUserWorksets() =>
            new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset).ToWorksets();

        private void FillWorksets(ManageWorksetsResult res)
        {
            foreach (var w in GetUserWorksets())
                res.Worksets.Add(new WorksetInfo
                {
                    Id = w.Id.IntegerValue, Name = w.Name, Kind = w.Kind.ToString(),
                    IsOpen = w.IsOpen, IsEditable = w.IsEditable
                });
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Manage Worksets";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  coord_link_model
    // ─────────────────────────────────────────────────────────────────────────
    public class LinkModelEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public LinkModelRequest Request { get; set; }
        public AIResult<LinkModelResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new LinkModelResult { Action = Request.Action };
            try
            {
                string action = (Request.Action ?? "list").Trim().ToLowerInvariant();
                switch (action)
                {
                    case "list":
                        FillLinks(res);
                        Result = new AIResult<LinkModelResult>
                            { Success = true, Message = $"Found {res.Links.Count} link(s).", Response = res };
                        break;

                    case "load":
                        LoadLink(res);
                        break;

                    case "reload":
                    case "unload":
                    case "remove":
                        ModifyLink(action, res);
                        break;

                    default:
                        throw new ArgumentException($"Unknown action '{Request.Action}'. Use list|load|reload|unload|remove.");
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<LinkModelResult>
                    { Success = false, Message = $"Error in coord_link_model: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        private void LoadLink(LinkModelResult res)
        {
            if (string.IsNullOrWhiteSpace(Request.Path) || !System.IO.File.Exists(Request.Path))
                throw new ArgumentException($"path '{Request.Path}' does not exist.");

            string ext = System.IO.Path.GetExtension(Request.Path).ToLowerInvariant();
            using (var tx = new Transaction(doc, "Load Link"))
            {
                tx.Start();
                if (ext == ".rvt")
                {
                    var mp = ModelPathUtils.ConvertUserVisiblePathToModelPath(Request.Path);
                    var opts = new RevitLinkOptions(false);
                    var loadResult = RevitLinkType.Create(doc, mp, opts);
                    var linkTypeId = loadResult.ElementId;
                    var inst = RevitLinkInstance.Create(doc, linkTypeId);
                    res.LinkTypeId = linkTypeId.GetValue();
                    res.LinkInstanceId = inst.Id.GetValue();
                }
                else if (ext == ".dwg" || ext == ".dxf" || ext == ".dgn")
                {
                    var opts = new DWGImportOptions { ThisViewOnly = false };
                    doc.Link(Request.Path, opts, doc.ActiveView, out var cadId);
                    res.LinkTypeId = cadId.GetValue();
                }
                else if (ext == ".ifc")
                {
                    res.Warnings.Add("IFC link: opening IFC and linking the resulting .rvt is recommended; direct IFC link is not supported by this tool.");
                }
                else throw new ArgumentException($"Unsupported link extension '{ext}'. Use .rvt, .dwg, .dxf or .dgn.");
                tx.Commit();
            }
            FillLinks(res);
            Result = new AIResult<LinkModelResult>
                { Success = res.LinkTypeId != 0, Message = $"Linked '{System.IO.Path.GetFileName(Request.Path)}'" +
                  (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."), Response = res };
        }

        private void ModifyLink(string action, LinkModelResult res)
        {
            var types = new FilteredElementCollector(doc).OfClass(typeof(RevitLinkType)).Cast<RevitLinkType>().ToList();
            RevitLinkType type = null;
            if (long.TryParse(Request.LinkId, out long lid))
                type = types.FirstOrDefault(t => t.Id.GetValue() == lid);
            type ??= types.FirstOrDefault(t => t.Name.Equals(Request.LinkId?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (type == null) throw new ArgumentException($"Revit link '{Request.LinkId}' not found.");
            res.LinkTypeId = type.Id.GetValue();

            switch (action)
            {
                case "reload": type.Reload(); break;
                case "unload": type.Unload(null); break;
                case "remove":
                    using (var tx = new Transaction(doc, "Remove Link"))
                    {
                        tx.Start();
                        doc.Delete(type.Id);
                        tx.Commit();
                    }
                    break;
            }
            if (action != "remove") FillLinks(res);
            Result = new AIResult<LinkModelResult>
                { Success = true, Message = $"Link '{type.Name}' {action}ed.", Response = res };
        }

        private void FillLinks(LinkModelResult res)
        {
            var instances = new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>().ToList();
            foreach (var type in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkType)).Cast<RevitLinkType>())
            {
                var info = new LinkInfo { TypeId = type.Id.GetValue(), Name = type.Name, Kind = "Revit" };
                try { info.Status = type.GetLinkedFileStatus().ToString(); } catch { }
                info.InstanceIds = instances.Where(i => i.GetTypeId() == type.Id).Select(i => i.Id.GetValue()).ToList();
                res.Links.Add(info);
            }
            foreach (var cad in new FilteredElementCollector(doc).OfClass(typeof(CADLinkType)).Cast<CADLinkType>())
                res.Links.Add(new LinkInfo { TypeId = cad.Id.GetValue(), Name = cad.Name, Kind = "CAD" });
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Link Model";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  coord_purge_unused
    // ─────────────────────────────────────────────────────────────────────────
    public class PurgeUnusedEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public PurgeUnusedRequest Request { get; set; }
        public AIResult<PurgeUnusedResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new PurgeUnusedResult { DryRun = Request.DryRun };
            try
            {
#if REVIT2024_OR_GREATER
                // Build the category set (empty = all categories Revit considers).
                ISet<ElementId> catIds = new HashSet<ElementId>();
                foreach (var cn in Request.Categories ?? new List<string>())
                {
                    if (McpResolveUtils.TryResolveBuiltInCategory(cn, out var bic))
                        catIds.Add(new ElementId(bic));
                    else res.Warnings.Add($"Unknown category '{cn}'; skipped.");
                }

                var unused = doc.GetUnusedElements(catIds);
                res.CandidateCount = unused.Count;
                foreach (var id in unused.Take(20))
                {
                    var el = doc.GetElement(id);
                    if (el != null) res.Sample.Add($"{el.Name} ({el.GetType().Name})");
                }

                if (!Request.DryRun && unused.Count > 0)
                {
                    using (var tx = new Transaction(doc, "Purge Unused"))
                    {
                        tx.Start();
                        try
                        {
                            var deleted = doc.Delete(unused);
                            res.DeletedCount = deleted?.Count ?? 0;
                        }
                        catch (Exception delEx) { res.Warnings.Add($"Some elements could not be deleted: {delEx.Message}"); }
                        tx.Commit();
                    }
                }

                Result = new AIResult<PurgeUnusedResult>
                {
                    Success = true,
                    Message = Request.DryRun
                        ? $"Dry run: {res.CandidateCount} unused element(s) could be purged."
                        : $"Purged {res.DeletedCount} of {res.CandidateCount} unused element(s)" +
                          (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
#else
                Result = new AIResult<PurgeUnusedResult>
                {
                    Success = false,
                    Message = "coord_purge_unused requires Revit 2024+ (Document.GetUnusedElements). Use Revit's Purge Unused UI on older versions.",
                    Response = res
                };
#endif
            }
            catch (Exception ex)
            {
                Result = new AIResult<PurgeUnusedResult>
                    { Success = false, Message = $"Error in coord_purge_unused: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Purge Unused";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  coord_audit_model  (read-only QA summary)
    // ─────────────────────────────────────────────────────────────────────────
    public class AuditModelEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public AuditModelRequest Request { get; set; }
        public AIResult<AuditModelResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new AuditModelResult();
            try
            {
                var warnings = doc.GetWarnings();
                res.WarningCount = warnings.Count;
                res.TopWarnings = warnings
                    .GroupBy(w => w.GetDescriptionText())
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => $"{g.Count()}× {g.Key}")
                    .ToList();

                res.GroupTypeCount = new FilteredElementCollector(doc).OfClass(typeof(GroupType)).GetElementCount();
                res.GroupInstanceCount = new FilteredElementCollector(doc).OfClass(typeof(Group)).GetElementCount();

                res.InPlaceFamilyCount = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
                    .Cast<FamilyInstance>().Count(fi => fi.Symbol?.Family?.IsInPlace == true);

                res.LinkCount = new FilteredElementCollector(doc).OfClass(typeof(RevitLinkType)).GetElementCount()
                              + new FilteredElementCollector(doc).OfClass(typeof(CADLinkType)).GetElementCount();

                res.DesignOptionCount = new FilteredElementCollector(doc).OfClass(typeof(DesignOption)).GetElementCount();
                res.Worksharing = doc.IsWorkshared;

#if REVIT2024_OR_GREATER
                try { res.UnusedElementCount = doc.GetUnusedElements(new HashSet<ElementId>()).Count; }
                catch { res.Notes.Add("Unused-element count unavailable."); }
#else
                res.Notes.Add("Unused-element count requires Revit 2024+.");
#endif
                if (res.InPlaceFamilyCount > 0) res.Notes.Add($"{res.InPlaceFamilyCount} in-place families (prefer loadable families).");
                if (res.WarningCount > 0) res.Notes.Add($"{res.WarningCount} warnings — review with get_warnings.");

                Result = new AIResult<AuditModelResult>
                {
                    Success = true,
                    Message = $"Audit: {res.WarningCount} warnings, {res.GroupInstanceCount} group instances, " +
                              $"{res.InPlaceFamilyCount} in-place families, {res.LinkCount} links, {res.UnusedElementCount} unused.",
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<AuditModelResult>
                    { Success = false, Message = $"Error in coord_audit_model: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Audit Model";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  coord_manage_phases
    // ─────────────────────────────────────────────────────────────────────────
    public class ManagePhasesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public ManagePhasesRequest Request { get; set; }
        public AIResult<ManagePhasesResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new ManagePhasesResult { Action = Request.Action };
            try
            {
                string action = (Request.Action ?? "list").Trim().ToLowerInvariant();
                if (action == "list")
                {
                    foreach (Phase ph in doc.Phases)
                        res.Phases.Add(new PhaseInfo { Id = ph.Id.GetValue(), Name = ph.Name });
                    foreach (var pf in new FilteredElementCollector(doc).OfClass(typeof(PhaseFilter)).Cast<PhaseFilter>())
                        res.PhaseFilters.Add(new PhaseInfo { Id = pf.Id.GetValue(), Name = pf.Name });
                    Result = new AIResult<ManagePhasesResult>
                        { Success = true, Message = $"{res.Phases.Count} phase(s), {res.PhaseFilters.Count} phase filter(s).", Response = res };
                    return;
                }

                if (action == "set_element_phase")
                {
                    if (Request.ElementIds == null || Request.ElementIds.Count == 0)
                        throw new ArgumentException("elementIds is required.");

                    ElementId createdId = ResolvePhaseId(Request.PhaseCreated);
                    bool clearDemo = Request.PhaseDemolished?.Trim().Equals("None", StringComparison.OrdinalIgnoreCase) == true;
                    ElementId demoId = clearDemo ? ElementId.InvalidElementId : ResolvePhaseId(Request.PhaseDemolished);

                    using (var tx = new Transaction(doc, "Set Element Phase"))
                    {
                        tx.Start();
                        foreach (var idVal in Request.ElementIds)
                        {
                            var el = McpResolveUtils.GetElement(doc, idVal);
                            if (el == null) { res.Warnings.Add($"Element {idVal} not found; skipped."); continue; }
                            bool any = false;
                            if (createdId != ElementId.InvalidElementId)
                            {
                                var p = el.get_Parameter(BuiltInParameter.PHASE_CREATED);
                                if (p != null && !p.IsReadOnly) { p.Set(createdId); any = true; }
                            }
                            if (clearDemo || demoId != ElementId.InvalidElementId)
                            {
                                var p = el.get_Parameter(BuiltInParameter.PHASE_DEMOLISHED);
                                if (p != null && !p.IsReadOnly) { p.Set(clearDemo ? ElementId.InvalidElementId : demoId); any = true; }
                            }
                            if (any) res.UpdatedCount++;
                            else res.Warnings.Add($"Element {idVal}: phase parameters not writable; skipped.");
                        }
                        tx.Commit();
                    }
                    Result = new AIResult<ManagePhasesResult>
                        { Success = res.UpdatedCount > 0, Message = $"Updated phase on {res.UpdatedCount} element(s)" +
                          (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."), Response = res };
                    return;
                }

                throw new ArgumentException($"Unknown action '{Request.Action}'. Use list|set_element_phase.");
            }
            catch (Exception ex)
            {
                Result = new AIResult<ManagePhasesResult>
                    { Success = false, Message = $"Error in coord_manage_phases: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        private ElementId ResolvePhaseId(string nameOrId)
        {
            if (string.IsNullOrWhiteSpace(nameOrId)) return ElementId.InvalidElementId;
            foreach (Phase ph in doc.Phases)
            {
                if ((long.TryParse(nameOrId, out long id) && ph.Id.GetValue() == id) ||
                    ph.Name.Equals(nameOrId.Trim(), StringComparison.OrdinalIgnoreCase))
                    return ph.Id;
            }
            return ElementId.InvalidElementId;
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Manage Phases";
    }
}
