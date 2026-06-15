using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Core
{
    // ----- delete_elements -----
    public class DeleteElementsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public DeleteElementsRequest Request { get; set; }
        public AIResult<CoreEditResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new CoreEditResult();
            try
            {
                var ids = Request.ElementIds.Select(McpResolveUtils.ToElementId).Where(i => doc.GetElement(i) != null).ToList();
                using (var tx = new Transaction(doc, "Delete Elements"))
                {
                    tx.Start();
                    foreach (var id in ids)
                    {
                        try { var deleted = doc.Delete(id); res.Count += deleted.Count; }
                        catch (Exception ex) { res.Warnings.Add($"{id.GetValue()}: {ex.Message}"); }
                    }
                    tx.Commit();
                }
                Result = new AIResult<CoreEditResult> { Success = true, Message = $"Deleted {res.Count} element(s) (incl. dependents).", Response = res };
            }
            catch (Exception ex) { Result = new AIResult<CoreEditResult> { Success = false, Message = $"Error deleting: {ex.Message}", Response = res }; }
            finally { _e.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Delete Elements";
    }

    // ----- duplicate_type -----
    public class DuplicateTypeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public DuplicateTypeRequest Request { get; set; }
        public AIResult<CoreEditResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new CoreEditResult();
            try
            {
                var src = McpResolveUtils.ResolveType(doc, Request.SourceTypeName);
                if (src == null) { Result = new AIResult<CoreEditResult> { Success = false, Message = $"Source type '{Request.SourceTypeName}' not found." }; _e.Set(); return; }
                using (var tx = new Transaction(doc, "Duplicate Type"))
                {
                    tx.Start();
                    var dupId = src.Duplicate(Request.NewTypeName);
                    var dup = doc.GetElement(dupId.Id);
                    if (dup != null)
                    {
                        res.Ids.Add(dup.Id.GetValue());
                        foreach (var ov in Request.Parameters ?? new List<ParamOverride>())
                        {
                            var p = dup.LookupParameter(ov.Name);
                            if (p == null) { res.Warnings.Add($"Param '{ov.Name}' not found."); continue; }
                            var err = McpParameterUtils.SetParameterValue(doc, p, ov.Value);
                            if (err != null) res.Warnings.Add($"'{ov.Name}': {err}");
                        }
                    }
                    tx.Commit();
                }
                res.Count = res.Ids.Count;
                Result = new AIResult<CoreEditResult> { Success = res.Count > 0, Message = res.Count > 0 ? $"Duplicated to '{Request.NewTypeName}' (id {res.Ids[0]})." : "Duplicate returned nothing.", Response = res };
            }
            catch (Exception ex) { Result = new AIResult<CoreEditResult> { Success = false, Message = $"Error duplicating type: {ex.Message}", Response = res }; }
            finally { _e.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Duplicate Type";
    }

    // ----- group_elements -----
    public class GroupElementsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public GroupElementsRequest Request { get; set; }
        public AIResult<CoreEditResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new CoreEditResult();
            try
            {
                var ids = Request.ElementIds.Select(McpResolveUtils.ToElementId).Where(i => doc.GetElement(i) != null).ToList();
                using (var tx = new Transaction(doc, "Group Elements"))
                {
                    tx.Start();
                    var group = doc.Create.NewGroup(ids);
                    if (group != null)
                    {
                        res.Ids.Add(group.Id.GetValue());
                        if (!string.IsNullOrWhiteSpace(Request.Name))
                        {
                            try { group.GroupType.Name = Request.Name; } catch { res.Warnings.Add("Could not set group name."); }
                        }
                    }
                    tx.Commit();
                }
                res.Count = res.Ids.Count;
                Result = new AIResult<CoreEditResult> { Success = res.Count > 0, Message = res.Count > 0 ? $"Grouped {ids.Count} element(s) into group {res.Ids[0]}." : "Group not created.", Response = res };
            }
            catch (Exception ex) { Result = new AIResult<CoreEditResult> { Success = false, Message = $"Error grouping: {ex.Message}", Response = res }; }
            finally { _e.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Group Elements";
    }

    // ----- ungroup_group -----
    public class UngroupGroupEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public ElementIdsRequest Request { get; set; }
        public AIResult<CoreEditResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new CoreEditResult();
            try
            {
                using (var tx = new Transaction(doc, "Ungroup"))
                {
                    tx.Start();
                    foreach (var gid in Request.ElementIds)
                    {
                        if (doc.GetElement(McpResolveUtils.ToElementId(gid)) is Group g)
                        {
                            var members = g.UngroupMembers();
                            res.Ids.AddRange(members.Select(m => m.GetValue()));
                        }
                        else res.Warnings.Add($"{gid} is not a group.");
                    }
                    tx.Commit();
                }
                res.Count = res.Ids.Count;
                Result = new AIResult<CoreEditResult> { Success = true, Message = $"Ungrouped into {res.Count} member(s).", Response = res };
            }
            catch (Exception ex) { Result = new AIResult<CoreEditResult> { Success = false, Message = $"Error ungrouping: {ex.Message}", Response = res }; }
            finally { _e.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Ungroup Group";
    }

    // ----- pin_elements -----
    public class PinElementsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public PinElementsRequest Request { get; set; }
        public AIResult<CoreEditResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new CoreEditResult();
            try
            {
                using (var tx = new Transaction(doc, "Pin/Unpin"))
                {
                    tx.Start();
                    foreach (var id in Request.ElementIds)
                    {
                        var el = McpResolveUtils.GetElement(doc, id);
                        if (el == null) { res.Warnings.Add($"{id} not found."); continue; }
                        try { el.Pinned = Request.Pinned; res.Ids.Add(id); } catch (Exception ex) { res.Warnings.Add($"{id}: {ex.Message}"); }
                    }
                    tx.Commit();
                }
                res.Count = res.Ids.Count;
                Result = new AIResult<CoreEditResult> { Success = true, Message = $"{(Request.Pinned ? "Pinned" : "Unpinned")} {res.Count} element(s).", Response = res };
            }
            catch (Exception ex) { Result = new AIResult<CoreEditResult> { Success = false, Message = $"Error pinning: {ex.Message}", Response = res }; }
            finally { _e.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Pin Elements";
    }

    // ----- rename_element -----
    public class RenameElementEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public RenameElementRequest Request { get; set; }
        public AIResult<CoreEditResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new CoreEditResult();
            try
            {
                var el = McpResolveUtils.GetElement(doc, Request.ElementId);
                if (el == null) { Result = new AIResult<CoreEditResult> { Success = false, Message = $"Element {Request.ElementId} not found." }; _e.Set(); return; }
                string old = el.Name;
                using (var tx = new Transaction(doc, "Rename Element"))
                {
                    tx.Start();
                    el.Name = Request.NewName;
                    tx.Commit();
                }
                res.Ids.Add(Request.ElementId); res.Count = 1;
                Result = new AIResult<CoreEditResult> { Success = true, Message = $"Renamed '{old}' -> '{Request.NewName}'.", Response = res };
            }
            catch (Exception ex) { Result = new AIResult<CoreEditResult> { Success = false, Message = $"Error renaming: {ex.Message}", Response = res }; }
            finally { _e.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Rename Element";
    }
}
