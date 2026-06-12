using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Core
{
    // ----- copy_parameter_values -----
    public class CopyParameterValuesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public CopyParameterValuesRequest Request { get; set; }
        public AIResult<CoreEditResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new CoreEditResult();
            try
            {
                var src = McpResolveUtils.GetElement(doc, Request.SourceId);
                if (src == null) { Fail(res, "Source element not found"); return; }

                var names = (Request.ParameterNames ?? new List<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                if (names.Count == 0)
                    names = src.Parameters.Cast<Parameter>().Where(p => p.HasValue && !p.IsReadOnly && p.Definition != null).Select(p => p.Definition.Name).Distinct().ToList();

                using (var tx = new Transaction(doc, "Copy Parameter Values"))
                {
                    tx.Start();
                    foreach (var tid in Request.TargetIds)
                    {
                        var tgt = McpResolveUtils.GetElement(doc, tid);
                        if (tgt == null) { res.Warnings.Add($"{tid} not found."); continue; }
                        foreach (var n in names)
                        {
                            var sp = src.LookupParameter(n); var tp = tgt.LookupParameter(n);
                            if (sp == null || tp == null || tp.IsReadOnly) continue;
                            try
                            {
                                switch (sp.StorageType)
                                {
                                    case StorageType.String: tp.Set(sp.AsString() ?? ""); break;
                                    case StorageType.Integer: tp.Set(sp.AsInteger()); break;
                                    case StorageType.Double: tp.Set(sp.AsDouble()); break;
                                    case StorageType.ElementId: tp.Set(sp.AsElementId()); break;
                                }
                            }
                            catch (Exception ex) { res.Warnings.Add($"{tid}.{n}: {ex.Message}"); }
                        }
                        res.Ids.Add(tid);
                    }
                    tx.Commit();
                }
                res.Count = res.Ids.Count;
                Result = new AIResult<CoreEditResult> { Success = true, Message = $"Copied {names.Count} parameter(s) to {res.Count} target(s).", Response = res };
            }
            catch (Exception ex) { Fail(res, ex.Message); }
            finally { _e.Set(); }
        }
        private void Fail(CoreEditResult r, string m) { Result = new AIResult<CoreEditResult> { Success = false, Message = m, Response = r }; _e.Set(); }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Copy Parameter Values";
    }

    // ----- bulk_set_by_filter -----
    public class BulkSetByFilterEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public BulkSetByFilterRequest Request { get; set; }
        public AIResult<CoreEditResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new CoreEditResult();
            try
            {
                var bics = new List<BuiltInCategory>();
                foreach (var c in Request.Categories) if (McpResolveUtils.TryResolveBuiltInCategory(c, out var b)) bics.Add(b);
                if (bics.Count == 0) { Fail(res, "No valid categories."); return; }

                var elements = new List<Element>();
                foreach (var bic in bics)
                {
                    var col = Request.ActiveViewOnly
                        ? new FilteredElementCollector(doc, doc.ActiveView.Id).OfCategory(bic).WhereElementIsNotElementType()
                        : new FilteredElementCollector(doc).OfCategory(bic).WhereElementIsNotElementType();
                    elements.AddRange(col.ToElements());
                }

                int ok = 0;
                using (var tx = new Transaction(doc, "Bulk Set Parameter"))
                {
                    tx.Start();
                    foreach (var el in elements)
                    {
                        var p = McpParameterUtils.ResolveParameter(doc, el, Request.BuiltInParameter, Request.ParameterName, Request.IsTypeParameter, true);
                        if (p == null) continue;
                        var err = McpParameterUtils.SetParameterValue(doc, p, Request.Value);
                        if (err == null) { ok++; if (res.Ids.Count < 200) res.Ids.Add(el.Id.GetValue()); }
                        else if (res.Warnings.Count < 10) res.Warnings.Add(err);
                    }
                    tx.Commit();
                }
                res.Count = ok;
                Result = new AIResult<CoreEditResult> { Success = ok > 0, Message = $"Set parameter on {ok}/{elements.Count} element(s).", Response = res };
            }
            catch (Exception ex) { Fail(res, ex.Message); }
            finally { _e.Set(); }
        }
        private void Fail(CoreEditResult r, string m) { Result = new AIResult<CoreEditResult> { Success = false, Message = m, Response = r }; _e.Set(); }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Bulk Set By Filter";
    }

    // ----- list_parameters_for_category -----
    public class ListParamsForCategoryEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public ListParamsForCategoryRequest Request { get; set; }
        public AIResult<List<ParamDescriptor>> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app;
            try
            {
                if (!McpResolveUtils.TryResolveBuiltInCategory(Request.Category, out var bic))
                { Result = new AIResult<List<ParamDescriptor>> { Success = false, Message = $"Unknown category '{Request.Category}'." }; _e.Set(); return; }

                var sample = new FilteredElementCollector(doc).OfCategory(bic).WhereElementIsNotElementType().FirstElement();
                var list = new List<ParamDescriptor>();
                if (sample != null)
                {
                    Add(sample, false, list);
                    if (Request.IncludeType) { var t = doc.GetElement(sample.GetTypeId()); if (t != null) Add(t, true, list); }
                }
                Result = new AIResult<List<ParamDescriptor>>
                { Success = true, Message = sample == null ? $"No elements of '{Request.Category}' found to sample." : $"Found {list.Count} parameter(s) on '{Request.Category}'.", Response = list };
            }
            catch (Exception ex) { Result = new AIResult<List<ParamDescriptor>> { Success = false, Message = ex.Message }; }
            finally { _e.Set(); }
        }
        private static void Add(Element el, bool isType, List<ParamDescriptor> list)
        {
            foreach (Parameter p in el.Parameters)
            {
                if (p?.Definition == null) continue;
                string bi = (p.Definition is InternalDefinition idf && idf.BuiltInParameter != BuiltInParameter.INVALID) ? idf.BuiltInParameter.ToString() : null;
                list.Add(new ParamDescriptor { Name = p.Definition.Name, StorageType = p.StorageType.ToString(), IsReadOnly = p.IsReadOnly, IsType = isType, BuiltIn = bi });
            }
        }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "List Parameters For Category";
    }

    // ----- create_project_parameter -----
    public class CreateProjectParameterEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public CreateProjectParameterRequest Request { get; set; }
        public AIResult<CoreEditResult> Result { get; private set; }
        public void Execute(UIApplication a)
        {
            uiApp = a; var res = new CoreEditResult();
            try
            {
                var catSet = app.Create.NewCategorySet();
                foreach (var c in Request.Categories)
                {
                    var cat = McpResolveUtils.ResolveCategory(doc, c);
                    if (cat != null) catSet.Insert(cat);
                    else res.Warnings.Add($"Category '{c}' not found.");
                }
                if (catSet.IsEmpty) { Fail(res, "No valid categories to bind."); return; }

                // Use a temporary shared parameter file to define the parameter, then bind as project param.
                string original = app.SharedParametersFilename;
                string tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mcp_proj_{Guid.NewGuid():N}.txt");
                System.IO.File.WriteAllText(tmp, "");
                app.SharedParametersFilename = tmp;

                using (var tx = new Transaction(doc, "Create Project Parameter"))
                {
                    tx.Start();
                    var defFile = app.OpenSharedParameterFile();
                    var group = defFile.Groups.Create("MCP");
                    var opts = new ExternalDefinitionCreationOptions(Request.Name,
#if REVIT2022_OR_GREATER
                        McpParameterUtils.GetSpecTypeId(Request.DataType)
#else
                        McpParameterUtils.GetParameterType(Request.DataType)
#endif
                        );
                    var extDef = group.Definitions.Create(opts) as ExternalDefinition;

                    Binding binding = Request.IsInstance
                        ? (Binding)app.Create.NewInstanceBinding(catSet)
                        : (Binding)app.Create.NewTypeBinding(catSet);

#if REVIT2022_OR_GREATER
                    doc.ParameterBindings.Insert(extDef, binding, McpParameterUtils.GetGroupTypeId(Request.Group));
#else
                    doc.ParameterBindings.Insert(extDef, binding, McpParameterUtils.GetParameterGroup(Request.Group));
#endif
                    res.Count = 1;
                    tx.Commit();
                }
                app.SharedParametersFilename = original ?? "";
                Result = new AIResult<CoreEditResult> { Success = true, Message = $"Created project parameter '{Request.Name}' bound to {catSet.Size} category(ies).", Response = res };
            }
            catch (Exception ex) { Fail(res, ex.Message); }
            finally { _e.Set(); }
        }
        private void Fail(CoreEditResult r, string m) { Result = new AIResult<CoreEditResult> { Success = false, Message = m, Response = r }; _e.Set(); }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Create Project Parameter";
    }

    // ----- get_element_info -----
    public class GetElementInfoEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public ElementIdsRequest Request { get; set; }
        public AIResult<List<ElementInfoSummary>> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var list = new List<ElementInfoSummary>();
            try
            {
                foreach (var id in Request.ElementIds)
                {
                    var info = new ElementInfoSummary { ElementId = id };
                    var el = McpResolveUtils.GetElement(doc, id);
                    if (el == null) { list.Add(info); continue; }
                    info.Found = true; info.Name = el.Name; info.Category = el.Category?.Name;
                    var tid = el.GetTypeId();
                    if (tid != null && tid != ElementId.InvalidElementId)
                    {
                        info.TypeId = tid.GetValue();
                        if (doc.GetElement(tid) is ElementType et) { info.TypeName = et.Name; info.FamilyName = et.FamilyName; }
                    }
                    if (el.LevelId != null && el.LevelId != ElementId.InvalidElementId && doc.GetElement(el.LevelId) is Level lv) info.LevelName = lv.Name;
                    if (el is FamilyInstance fi && fi.Host != null) info.HostId = fi.Host.Id.GetValue();
                    list.Add(info);
                }
                Result = new AIResult<List<ElementInfoSummary>> { Success = true, Message = $"Info for {list.Count(i => i.Found)}/{list.Count} element(s).", Response = list };
            }
            catch (Exception ex) { Result = new AIResult<List<ElementInfoSummary>> { Success = false, Message = ex.Message, Response = list }; }
            finally { _e.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Get Element Info";
    }

    // ----- get_element_geometry -----
    public class GetElementGeometryEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public ElementIdsRequest Request { get; set; }
        public AIResult<List<ElementGeometryInfo>> Result { get; private set; }
        private static JZPoint P(XYZ p) => new JZPoint(McpResolveUtils.FtToMm(p.X), McpResolveUtils.FtToMm(p.Y), McpResolveUtils.FtToMm(p.Z));
        public void Execute(UIApplication app)
        {
            uiApp = app; var list = new List<ElementGeometryInfo>();
            try
            {
                foreach (var id in Request.ElementIds)
                {
                    var g = new ElementGeometryInfo { ElementId = id };
                    var el = McpResolveUtils.GetElement(doc, id);
                    if (el == null) { list.Add(g); continue; }
                    g.Found = true;
                    switch (el.Location)
                    {
                        case LocationPoint lp: g.LocationType = "point"; g.Point = P(lp.Point); break;
                        case LocationCurve lc:
                            g.LocationType = "curve"; g.Start = P(lc.Curve.GetEndPoint(0)); g.End = P(lc.Curve.GetEndPoint(1));
                            g.LengthMm = McpResolveUtils.FtToMm(lc.Curve.Length); break;
                    }
                    var bb = el.get_BoundingBox(null);
                    if (bb != null) { g.BboxMin = P(bb.Min); g.BboxMax = P(bb.Max); }
                    var areaP = el.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
                    if (areaP != null && areaP.HasValue) g.AreaMm2 = areaP.AsDouble() * 304.8 * 304.8;
                    var volP = el.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED);
                    if (volP != null && volP.HasValue) g.VolumeMm3 = volP.AsDouble() * 304.8 * 304.8 * 304.8;
                    list.Add(g);
                }
                Result = new AIResult<List<ElementGeometryInfo>> { Success = true, Message = $"Geometry for {list.Count(i => i.Found)}/{list.Count} element(s).", Response = list };
            }
            catch (Exception ex) { Result = new AIResult<List<ElementGeometryInfo>> { Success = false, Message = ex.Message, Response = list }; }
            finally { _e.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Get Element Geometry";
    }

    // ----- find_elements -----
    public class FindElementsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp; private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _e = new ManualResetEvent(false);
        public FindElementsRequest Request { get; set; }
        public AIResult<CoreEditResult> Result { get; private set; }
        public void Execute(UIApplication app)
        {
            uiApp = app; var res = new CoreEditResult();
            try
            {
                var bics = new List<BuiltInCategory>();
                foreach (var c in Request.Categories) if (McpResolveUtils.TryResolveBuiltInCategory(c, out var b)) bics.Add(b);
                bool contains = (Request.Match ?? "contains").Trim().ToLowerInvariant() != "equals";

                foreach (var bic in bics)
                {
                    var col = Request.ActiveViewOnly
                        ? new FilteredElementCollector(doc, doc.ActiveView.Id).OfCategory(bic).WhereElementIsNotElementType()
                        : new FilteredElementCollector(doc).OfCategory(bic).WhereElementIsNotElementType();
                    foreach (var el in col)
                    {
                        if (res.Ids.Count >= Request.Limit) break;
                        if (string.IsNullOrWhiteSpace(Request.ParameterName)) { res.Ids.Add(el.Id.GetValue()); continue; }
                        var p = el.LookupParameter(Request.ParameterName);
                        if (p == null) continue;
                        string val = p.StorageType == StorageType.String ? (p.AsString() ?? "") : (p.AsValueString() ?? "");
                        bool m = contains ? val.IndexOf(Request.Value ?? "", StringComparison.OrdinalIgnoreCase) >= 0
                                          : val.Equals(Request.Value ?? "", StringComparison.OrdinalIgnoreCase);
                        if (m) res.Ids.Add(el.Id.GetValue());
                    }
                }
                res.Count = res.Ids.Count;
                Result = new AIResult<CoreEditResult> { Success = true, Message = $"Found {res.Count} element(s).", Response = res };
            }
            catch (Exception ex) { Result = new AIResult<CoreEditResult> { Success = false, Message = ex.Message, Response = res }; }
            finally { _e.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _e.Reset(); return _e.WaitOne(t); }
        public string GetName() => "Find Elements";
    }
}
