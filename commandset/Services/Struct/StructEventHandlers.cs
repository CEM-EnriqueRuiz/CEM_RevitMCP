using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Struct
{
    internal static class StructHelpers
    {
        /// <summary>
        ///     Resolve a structural FamilySymbol by name/id within a category, falling back to the
        ///     first symbol of that category. Activates the symbol (required before placement).
        /// </summary>
        public static FamilySymbol ResolveStructuralSymbol(Document doc, string nameOrId,
            BuiltInCategory category, List<string> warnings)
        {
            var symbols = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(category)
                .Cast<FamilySymbol>()
                .ToList();

            if (symbols.Count == 0) return null;

            FamilySymbol sym = null;
            if (!string.IsNullOrWhiteSpace(nameOrId))
            {
                if (long.TryParse(nameOrId, out long id))
                    sym = symbols.FirstOrDefault(s => s.Id.GetValue() == id);
                sym ??= symbols.FirstOrDefault(s => s.Name.Equals(nameOrId.Trim(), StringComparison.OrdinalIgnoreCase));
                sym ??= symbols.FirstOrDefault(s =>
                    $"{s.FamilyName} : {s.Name}".Equals(nameOrId.Trim(), StringComparison.OrdinalIgnoreCase));
                if (sym == null) warnings.Add($"Type '{nameOrId}' not found in {category}; using first available.");
            }
            sym ??= symbols.First();

            if (!sym.IsActive) { sym.Activate(); doc.Regenerate(); }
            return sym;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  struct_create_beam / struct_create_brace  (line-based)
    // ─────────────────────────────────────────────────────────────────────────
    public class StructLineMemberEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        /// <summary>Beam or Brace.</summary>
        public StructuralType MemberType { get; set; } = StructuralType.Beam;
        public StructLineMemberRequest Request { get; set; }
        public AIResult<StructMemberResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new StructMemberResult();
            try
            {
                if (Request.Start == null || Request.End == null) throw new ArgumentException("start and end are required.");
                XYZ p1 = JZPoint.ToXYZ(Request.Start), p2 = JZPoint.ToXYZ(Request.End);
                if (p1.DistanceTo(p2) < 1e-6) throw new ArgumentException("start and end are coincident.");

                var level = McpResolveUtils.ResolveLevel(doc, Request.Level);
                if (level == null) throw new InvalidOperationException("No level available.");

                var sym = StructHelpers.ResolveStructuralSymbol(doc, Request.TypeName,
                    BuiltInCategory.OST_StructuralFraming, res.Warnings);
                if (sym == null) throw new InvalidOperationException("No Structural Framing type available. Load a beam/brace family first.");

                FamilyInstance fi;
                using (var tx = new Transaction(doc, MemberType == StructuralType.Brace ? "Create Brace" : "Create Beam"))
                {
                    tx.Start();
                    var line = Line.CreateBound(p1, p2);
                    fi = doc.Create.NewFamilyInstance(line, sym, level, MemberType);
                    tx.Commit();
                }

                res.ElementId = fi.Id.GetValue();
                res.TypeName = $"{sym.FamilyName} : {sym.Name}";
                Result = new AIResult<StructMemberResult>
                {
                    Success = true,
                    Message = $"Created {MemberType} '{res.TypeName}' (id {res.ElementId})" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<StructMemberResult> { Success = false, Message = $"Error: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Create Structural Line Member";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  struct_create_column  (point + level)
    // ─────────────────────────────────────────────────────────────────────────
    public class StructColumnEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public StructColumnRequest Request { get; set; }
        public AIResult<StructMemberResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new StructMemberResult();
            try
            {
                if (Request.Location == null) throw new ArgumentException("location is required.");
                var baseLevel = McpResolveUtils.ResolveLevel(doc, Request.BaseLevel);
                if (baseLevel == null) throw new InvalidOperationException("No base level available.");

                var sym = StructHelpers.ResolveStructuralSymbol(doc, Request.TypeName,
                    BuiltInCategory.OST_StructuralColumns, res.Warnings);
                if (sym == null) throw new InvalidOperationException("No Structural Columns type available. Load a column family first.");

                XYZ loc = JZPoint.ToXYZ(Request.Location);

                FamilyInstance fi;
                using (var tx = new Transaction(doc, "Create Structural Column"))
                {
                    tx.Start();
                    fi = doc.Create.NewFamilyInstance(loc, sym, baseLevel, StructuralType.Column);

                    // Optional top level → set the column's Top Level param.
                    if (!string.IsNullOrWhiteSpace(Request.TopLevel))
                    {
                        var topLevel = McpResolveUtils.ResolveLevel(doc, Request.TopLevel);
                        if (topLevel != null)
                        {
                            var topParam = fi.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
                            if (topParam != null && !topParam.IsReadOnly) topParam.Set(topLevel.Id);
                            else res.Warnings.Add("Top level parameter not writable for this column type.");
                        }
                        else res.Warnings.Add($"Top level '{Request.TopLevel}' not found.");
                    }
                    tx.Commit();
                }

                res.ElementId = fi.Id.GetValue();
                res.TypeName = $"{sym.FamilyName} : {sym.Name}";
                Result = new AIResult<StructMemberResult>
                {
                    Success = true,
                    Message = $"Created structural column '{res.TypeName}' (id {res.ElementId})" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<StructMemberResult> { Success = false, Message = $"Error: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Create Structural Column";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  struct_create_foundation  (point + level, isolated footing)
    // ─────────────────────────────────────────────────────────────────────────
    public class StructFoundationEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public StructFoundationRequest Request { get; set; }
        public AIResult<StructMemberResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new StructMemberResult();
            try
            {
                if (Request.Location == null) throw new ArgumentException("location is required.");
                var level = McpResolveUtils.ResolveLevel(doc, Request.Level);
                if (level == null) throw new InvalidOperationException("No level available.");

                var sym = StructHelpers.ResolveStructuralSymbol(doc, Request.TypeName,
                    BuiltInCategory.OST_StructuralFoundation, res.Warnings);
                if (sym == null) throw new InvalidOperationException("No Structural Foundation type available. Load an isolated footing family first.");

                XYZ loc = JZPoint.ToXYZ(Request.Location);

                FamilyInstance fi;
                using (var tx = new Transaction(doc, "Create Structural Foundation"))
                {
                    tx.Start();
                    fi = doc.Create.NewFamilyInstance(loc, sym, level, StructuralType.Footing);
                    tx.Commit();
                }

                res.ElementId = fi.Id.GetValue();
                res.TypeName = $"{sym.FamilyName} : {sym.Name}";
                Result = new AIResult<StructMemberResult>
                {
                    Success = true,
                    Message = $"Created structural foundation '{res.TypeName}' (id {res.ElementId})" +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<StructMemberResult> { Success = false, Message = $"Error: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }
        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Create Structural Foundation";
    }
}
