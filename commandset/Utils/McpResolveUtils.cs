using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CEM_IAModeler_CommandSet.Utils;

/// <summary>
///     Generic resolution helpers shared by all command handlers. The AI rarely
///     knows ElementIds; it passes human names ("Generic - 200mm", "Level 1",
///     "OST_Walls"). These helpers accept an id OR a name and resolve robustly,
///     so every tool can be lenient about its input.
///     Version-safe across Revit 2020-2026.
/// </summary>
public static class McpResolveUtils
{
    private const double MM_TO_FT = 1.0 / 304.8;

    /// <summary>mm -> internal feet.</summary>
    public static double MmToFt(double mm) => mm * MM_TO_FT;

    /// <summary>internal feet -> mm.</summary>
    public static double FtToMm(double ft) => ft / MM_TO_FT;

    /// <summary>Resolve an element by integer id (version-safe ElementId construction).</summary>
    public static Element GetElement(Document doc, long id)
    {
#if REVIT2024_OR_GREATER
        return doc.GetElement(new ElementId(id));
#else
        return doc.GetElement(new ElementId((int)id));
#endif
    }

    /// <summary>Build an ElementId from a long, version-safe.</summary>
    public static ElementId ToElementId(long id)
    {
#if REVIT2024_OR_GREATER
        return new ElementId(id);
#else
        return new ElementId((int)id);
#endif
    }

    /// <summary>
    ///     Resolve a BuiltInCategory from a name. Accepts the enum name ("OST_Walls"),
    ///     with or without the "OST_" prefix, case-insensitively.
    /// </summary>
    public static bool TryResolveBuiltInCategory(string name, out BuiltInCategory bic)
    {
        bic = BuiltInCategory.INVALID;
        if (string.IsNullOrWhiteSpace(name)) return false;

        string n = name.Trim();
        if (Enum.TryParse(n, true, out BuiltInCategory parsed)) { bic = parsed; return true; }
        if (Enum.TryParse("OST_" + n, true, out BuiltInCategory prefixed)) { bic = prefixed; return true; }
        return false;
    }

    /// <summary>
    ///     Resolve a Category on the document by BuiltInCategory name or by display name
    ///     ("Walls", "Mechanical Equipment").
    /// </summary>
    public static Category ResolveCategory(Document doc, string name)
    {
        if (TryResolveBuiltInCategory(name, out BuiltInCategory bic))
        {
            try { return Category.GetCategory(doc, bic); } catch { }
        }

        foreach (Category c in doc.Settings.Categories)
        {
            if (c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return c;
        }
        return null;
    }

    /// <summary>
    ///     Resolve a Level by id, exact name, or nearest elevation when a number is given.
    ///     Falls back to the lowest level so creation tools rarely fail outright.
    /// </summary>
    public static Level ResolveLevel(Document doc, string nameOrId, double? elevationMm = null)
    {
        var levels = new FilteredElementCollector(doc)
            .OfClass(typeof(Level)).Cast<Level>().ToList();
        if (levels.Count == 0) return null;

        if (!string.IsNullOrWhiteSpace(nameOrId))
        {
            // by id
            if (long.TryParse(nameOrId, out long id))
            {
                var byId = levels.FirstOrDefault(l => l.Id.GetValue() == id);
                if (byId != null) return byId;
            }
            // by name
            var byName = levels.FirstOrDefault(l =>
                l.Name.Equals(nameOrId.Trim(), StringComparison.OrdinalIgnoreCase));
            if (byName != null) return byName;
        }

        if (elevationMm.HasValue)
        {
            double targetFt = MmToFt(elevationMm.Value);
            return levels.OrderBy(l => Math.Abs(l.Elevation - targetFt)).First();
        }

        // default: lowest level
        return levels.OrderBy(l => l.Elevation).First();
    }

    /// <summary>
    ///     Resolve a type/symbol ElementId by id or by (type) name within an optional class,
    ///     e.g. a WallType, FloorType, DuctType, FamilySymbol. Matches on the type's Name
    ///     and on "Family : Type" formatting.
    /// </summary>
    public static ElementType ResolveType(Document doc, string nameOrId, Type ofClass = null)
    {
        var collector = new FilteredElementCollector(doc).WhereElementIsElementType();
        if (ofClass != null) collector = collector.OfClass(ofClass);
        var types = collector.Cast<ElementType>().ToList();
        if (types.Count == 0) return null;

        if (!string.IsNullOrWhiteSpace(nameOrId))
        {
            if (long.TryParse(nameOrId, out long id))
            {
                var byId = types.FirstOrDefault(t => t.Id.GetValue() == id);
                if (byId != null) return byId;
            }

            string n = nameOrId.Trim();
            // exact type name
            var byName = types.FirstOrDefault(t => t.Name.Equals(n, StringComparison.OrdinalIgnoreCase));
            if (byName != null) return byName;

            // "Family : Type" form
            var byFull = types.FirstOrDefault(t =>
                $"{t.FamilyName} : {t.Name}".Equals(n, StringComparison.OrdinalIgnoreCase) ||
                $"{t.FamilyName}: {t.Name}".Equals(n, StringComparison.OrdinalIgnoreCase));
            if (byFull != null) return byFull;

            // contains (last resort)
            var byContains = types.FirstOrDefault(t =>
                t.Name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0);
            if (byContains != null) return byContains;
        }

        return null;
    }

    /// <summary>
    ///     Resolve the first type of a class, used as a sensible default when the AI
    ///     omits a type (so creation tools warn rather than fail).
    /// </summary>
    public static ElementType FirstTypeOfClass(Document doc, Type ofClass)
    {
        return new FilteredElementCollector(doc)
            .OfClass(ofClass).WhereElementIsElementType()
            .Cast<ElementType>().FirstOrDefault();
    }

    /// <summary>
    ///     Build a closed CurveLoop from a list of points (mm). Auto-closes the loop if the
    ///     last point differs from the first. Skips zero-length segments. Returns null if
    ///     fewer than 3 distinct points are given.
    /// </summary>
    public static Autodesk.Revit.DB.CurveLoop BuildCurveLoop(
        IList<Models.Common.JZPoint> pts, out string warning)
    {
        warning = null;
        if (pts == null || pts.Count < 3) { warning = "A closed loop needs at least 3 points."; return null; }

        var xyz = pts.Select(Models.Common.JZPoint.ToXYZ).ToList();
        // close if needed
        if (xyz.First().DistanceTo(xyz.Last()) > 1e-6) xyz.Add(xyz.First());

        var loop = new Autodesk.Revit.DB.CurveLoop();
        int added = 0;
        for (int i = 0; i < xyz.Count - 1; i++)
        {
            if (xyz[i].DistanceTo(xyz[i + 1]) < 1e-6) continue; // skip degenerate
            loop.Append(Autodesk.Revit.DB.Line.CreateBound(xyz[i], xyz[i + 1]));
            added++;
        }
        if (added < 3) { warning = "Loop collapsed to fewer than 3 valid segments."; return null; }
        return loop;
    }

    /// <summary>
    ///     Resolve the active or a named view. -1/empty = active view.
    /// </summary>
    public static View ResolveView(Document doc, UIDocument uiDoc, string nameOrId)
    {
        if (string.IsNullOrWhiteSpace(nameOrId) || nameOrId.Trim() == "-1")
            return uiDoc?.ActiveView ?? doc.ActiveView;

        var views = new FilteredElementCollector(doc).OfClass(typeof(View))
            .Cast<View>().Where(v => !v.IsTemplate).ToList();

        if (long.TryParse(nameOrId, out long id))
        {
            var byId = views.FirstOrDefault(v => v.Id.GetValue() == id);
            if (byId != null) return byId;
        }
        return views.FirstOrDefault(v => v.Name.Equals(nameOrId.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
