using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Electrical;

namespace CEM_IAModeler_CommandSet.Utils;

/// <summary>
///     Shared MEP helpers: resolve system types and curve types by name/id, and
///     apply diameters / dimensions after creation. Keeps the mep_* handlers generic
///     and lenient (defaults + warnings instead of hard failures).
/// </summary>
public static class McpMepUtils
{
    /// <summary>Resolve a MechanicalSystemType by name/id, defaulting to the first one.</summary>
    public static ElementId ResolveMechanicalSystemType(Document doc, string nameOrId)
    {
        return ResolveTypeId<MechanicalSystemType>(doc, nameOrId);
    }

    /// <summary>Resolve a PipingSystemType by name/id, defaulting to the first one.</summary>
    public static ElementId ResolvePipingSystemType(Document doc, string nameOrId)
    {
        return ResolveTypeId<PipingSystemType>(doc, nameOrId);
    }

    public static ElementId ResolveDuctType(Document doc, string nameOrId)
        => ResolveTypeId<DuctType>(doc, nameOrId);

    public static ElementId ResolvePipeType(Document doc, string nameOrId)
        => ResolveTypeId<PipeType>(doc, nameOrId);

    public static ElementId ResolveConduitType(Document doc, string nameOrId)
        => ResolveTypeId<ConduitType>(doc, nameOrId);

    public static ElementId ResolveCableTrayType(Document doc, string nameOrId)
        => ResolveTypeId<CableTrayType>(doc, nameOrId);

    /// <summary>
    ///     Generic "resolve a type element by name or id, else first available" used for
    ///     all MEP system/curve types.
    /// </summary>
    public static ElementId ResolveTypeId<T>(Document doc, string nameOrId) where T : Element
    {
        var all = new FilteredElementCollector(doc).OfClass(typeof(T)).Cast<T>().ToList();
        if (all.Count == 0) return ElementId.InvalidElementId;

        if (!string.IsNullOrWhiteSpace(nameOrId))
        {
            if (long.TryParse(nameOrId, out long id))
            {
                var byId = all.FirstOrDefault(e => e.Id.GetValue() == id);
                if (byId != null) return byId.Id;
            }
            var byName = all.FirstOrDefault(e => e.Name.Equals(nameOrId.Trim(), StringComparison.OrdinalIgnoreCase))
                      ?? all.FirstOrDefault(e => e.Name.IndexOf(nameOrId.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);
            if (byName != null) return byName.Id;
        }
        return all.First().Id;
    }

    /// <summary>
    ///     Set a round MEP curve diameter (mm) on a created Duct/Pipe/Conduit, when supported.
    ///     Returns a warning string or null on success.
    /// </summary>
    public static string TrySetDiameter(Element mepCurve, double diameterMm)
    {
        if (diameterMm <= 0) return null;
        double ft = McpResolveUtils.MmToFt(diameterMm);
        try
        {
            Parameter p = mepCurve.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)
                       ?? mepCurve.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM)
                       ?? mepCurve.LookupParameter("Diameter");
            if (p != null && !p.IsReadOnly) { p.Set(ft); return null; }
            return "Could not set diameter (no writable diameter parameter).";
        }
        catch (Exception ex) { return $"Diameter set failed: {ex.Message}"; }
    }

    /// <summary>
    ///     Set rectangular MEP curve width/height (mm) on a Duct/CableTray, when supported.
    /// </summary>
    public static string TrySetWidthHeight(Element mepCurve, double widthMm, double heightMm)
    {
        var warnings = new List<string>();
        if (widthMm > 0)
        {
            try
            {
                Parameter w = mepCurve.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM)
                           ?? mepCurve.LookupParameter("Width");
                if (w != null && !w.IsReadOnly) w.Set(McpResolveUtils.MmToFt(widthMm));
                else warnings.Add("width not writable");
            }
            catch (Exception ex) { warnings.Add($"width: {ex.Message}"); }
        }
        if (heightMm > 0)
        {
            try
            {
                Parameter h = mepCurve.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM)
                           ?? mepCurve.LookupParameter("Height");
                if (h != null && !h.IsReadOnly) h.Set(McpResolveUtils.MmToFt(heightMm));
                else warnings.Add("height not writable");
            }
            catch (Exception ex) { warnings.Add($"height: {ex.Message}"); }
        }
        return warnings.Count > 0 ? string.Join("; ", warnings) : null;
    }
}
