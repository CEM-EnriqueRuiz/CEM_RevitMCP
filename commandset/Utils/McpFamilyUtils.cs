using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace RevitMCPCommandSet.Utils;

/// <summary>
///     Family-domain helpers: an overwriting family load option and structural-type parsing.
/// </summary>
public static class McpFamilyUtils
{
    /// <summary>Parse a friendly structural type name to the enum (defaults NonStructural).</summary>
    public static StructuralType ParseStructuralType(string name)
    {
        switch ((name ?? "").Trim().ToLowerInvariant())
        {
            case "beam": return StructuralType.Beam;
            case "column": return StructuralType.Column;
            case "brace": return StructuralType.Brace;
            case "footing": return StructuralType.Footing;
            default: return StructuralType.NonStructural;
        }
    }
}

/// <summary>
///     Family load options that overwrite existing families and their parameter values,
///     so re-loading an updated .rfa "just works" for the AI.
/// </summary>
public class OverwriteFamilyLoadOptions : IFamilyLoadOptions
{
    private readonly bool _overwrite;
    public OverwriteFamilyLoadOptions(bool overwrite) { _overwrite = overwrite; }

    public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
    {
        overwriteParameterValues = _overwrite;
        return _overwrite;
    }

    public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse,
        out FamilySource source, out bool overwriteParameterValues)
    {
        source = FamilySource.Family;
        overwriteParameterValues = _overwrite;
        return _overwrite;
    }
}
