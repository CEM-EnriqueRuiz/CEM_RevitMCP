using Autodesk.Revit.DB;

namespace CEM_IAModeler_CommandSet.Utils;

/// <summary>
///     Helpers shared by the generic parameter/filter commands:
///     data type mapping, unit conversion and parameter resolution.
///     Designed to work across Revit 2020-2026 via conditional compilation.
/// </summary>
public static class McpParameterUtils
{
    private const double MM_TO_FT = 1.0 / 304.8;

#if REVIT2022_OR_GREATER
    /// <summary>
    ///     Maps a friendly data type name to a ForgeTypeId spec (Revit 2022+)
    /// </summary>
    public static ForgeTypeId GetSpecTypeId(string dataType)
    {
        switch ((dataType ?? "Text").Trim().ToLowerInvariant())
        {
            case "text": return SpecTypeId.String.Text;
            case "url": return SpecTypeId.String.Url;
            case "integer": return SpecTypeId.Int.Integer;
            case "number": return SpecTypeId.Number;
            case "length": return SpecTypeId.Length;
            case "area": return SpecTypeId.Area;
            case "volume": return SpecTypeId.Volume;
            case "angle": return SpecTypeId.Angle;
            case "yesno":
            case "boolean": return SpecTypeId.Boolean.YesNo;
            case "material": return SpecTypeId.Reference.Material;
            default: return SpecTypeId.String.Text;
        }
    }
    /// <summary>
    ///     Maps a friendly UI group name to a GroupTypeId (Revit 2022+).
    /// </summary>
    public static ForgeTypeId GetGroupTypeId(string group)
    {
        switch ((group ?? "Data").Trim().ToLowerInvariant())
        {
            case "text": return GroupTypeId.Text;
            case "identitydata":
            case "identity": return GroupTypeId.IdentityData;
            case "dimensions": return GroupTypeId.Geometry;
            case "construction": return GroupTypeId.Construction;
            case "materials": return GroupTypeId.Materials;
            case "general": return GroupTypeId.General;
            case "data":
            default: return GroupTypeId.Data;
        }
    }
#else
    /// <summary>
    ///     Maps a friendly UI group name to a BuiltInParameterGroup (Revit 2020/2021).
    /// </summary>
    public static BuiltInParameterGroup GetParameterGroup(string group)
    {
        switch ((group ?? "Data").Trim().ToLowerInvariant())
        {
            case "text": return BuiltInParameterGroup.PG_TEXT;
            case "identitydata":
            case "identity": return BuiltInParameterGroup.PG_IDENTITY_DATA;
            case "dimensions": return BuiltInParameterGroup.PG_GEOMETRY;
            case "construction": return BuiltInParameterGroup.PG_CONSTRUCTION;
            case "materials": return BuiltInParameterGroup.PG_MATERIALS;
            case "general": return BuiltInParameterGroup.PG_GENERAL;
            case "data":
            default: return BuiltInParameterGroup.PG_DATA;
        }
    }

    /// <summary>
    ///     Maps a friendly data type name to a ParameterType (Revit 2020/2021)
    /// </summary>
    public static ParameterType GetParameterType(string dataType)
    {
        switch ((dataType ?? "Text").Trim().ToLowerInvariant())
        {
            case "text": return ParameterType.Text;
            case "url": return ParameterType.URL;
            case "integer": return ParameterType.Integer;
            case "number": return ParameterType.Number;
            case "length": return ParameterType.Length;
            case "area": return ParameterType.Area;
            case "volume": return ParameterType.Volume;
            case "angle": return ParameterType.Angle;
            case "yesno":
            case "boolean": return ParameterType.YesNo;
            case "material": return ParameterType.Material;
            default: return ParameterType.Text;
        }
    }
#endif

    /// <summary>
    ///     Converts a user-facing numeric value (mm, mm², mm³, degrees) to Revit
    ///     internal units based on a friendly data type name.
    /// </summary>
    public static double ToInternalUnits(double value, string dataType)
    {
        switch ((dataType ?? "").Trim().ToLowerInvariant())
        {
            case "length": return value * MM_TO_FT;
            case "area": return value * MM_TO_FT * MM_TO_FT;
            case "volume": return value * MM_TO_FT * MM_TO_FT * MM_TO_FT;
            case "angle": return value * Math.PI / 180.0;
            default: return value;
        }
    }

    /// <summary>
    ///     Converts a user-facing numeric value to internal units based on
    ///     the parameter's own definition (used when writing existing parameters).
    /// </summary>
    public static double ToInternalUnits(double value, Parameter param)
    {
        try
        {
#if REVIT2023_OR_GREATER
            ForgeTypeId dt = param.Definition.GetDataType();
            if (dt.Equals(SpecTypeId.Length)) return value * MM_TO_FT;
            if (dt.Equals(SpecTypeId.Area)) return value * MM_TO_FT * MM_TO_FT;
            if (dt.Equals(SpecTypeId.Volume)) return value * MM_TO_FT * MM_TO_FT * MM_TO_FT;
            if (dt.Equals(SpecTypeId.Angle)) return value * Math.PI / 180.0;
#else
            switch (param.Definition.ParameterType)
            {
                case ParameterType.Length: return value * MM_TO_FT;
                case ParameterType.Area: return value * MM_TO_FT * MM_TO_FT;
                case ParameterType.Volume: return value * MM_TO_FT * MM_TO_FT * MM_TO_FT;
                case ParameterType.Angle: return value * Math.PI / 180.0;
            }
#endif
        }
        catch
        {
            // Fall through: treat as raw number
        }
        return value;
    }

    /// <summary>
    ///     Resolves a parameter on an element by BuiltInParameter enum name or display name,
    ///     optionally falling back to the element's type (or instance) scope.
    /// </summary>
    /// <returns>The parameter, or null if not found</returns>
    public static Parameter ResolveParameter(
        Document doc,
        Element element,
        string builtInParameterName,
        string parameterName,
        bool preferType,
        bool fallbackToOtherScope)
    {
        Element typeElement = null;
        ElementId typeId = element.GetTypeId();
        if (typeId != ElementId.InvalidElementId)
            typeElement = doc.GetElement(typeId);

        Element primary = preferType ? typeElement : element;
        Element secondary = preferType ? element : typeElement;

        Parameter param = FindOnElement(primary, builtInParameterName, parameterName);
        if (param == null && fallbackToOtherScope)
            param = FindOnElement(secondary, builtInParameterName, parameterName);

        return param;
    }

    private static Parameter FindOnElement(Element element, string builtInParameterName, string parameterName)
    {
        if (element == null)
            return null;

        // BuiltInParameter takes priority
        if (!string.IsNullOrWhiteSpace(builtInParameterName) &&
            Enum.TryParse(builtInParameterName, true, out BuiltInParameter bip))
        {
            Parameter p = element.get_Parameter(bip);
            if (p != null)
                return p;
        }

        if (!string.IsNullOrWhiteSpace(parameterName))
            return element.LookupParameter(parameterName);

        return null;
    }

    /// <summary>
    ///     Resolves a parameter ElementId usable in filter rules:
    ///     BuiltInParameter enum name, or project/shared/global parameter display name.
    /// </summary>
    public static ElementId ResolveParameterId(Document doc, string builtInParameterName, string parameterName)
    {
        if (!string.IsNullOrWhiteSpace(builtInParameterName) &&
            Enum.TryParse(builtInParameterName, true, out BuiltInParameter bip))
        {
            return new ElementId(bip);
        }

        if (!string.IsNullOrWhiteSpace(parameterName))
        {
            // Project / shared parameters live as ParameterElement instances
            var paramElement = new FilteredElementCollector(doc)
                .OfClass(typeof(ParameterElement))
                .Cast<ParameterElement>()
                .FirstOrDefault(pe =>
                    pe.GetDefinition() != null &&
                    pe.GetDefinition().Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase));

            if (paramElement != null)
                return paramElement.Id;

            // Last resort: try matching a BuiltInParameter by its enum name
            if (Enum.TryParse(parameterName, true, out BuiltInParameter bip2))
                return new ElementId(bip2);
        }

        return ElementId.InvalidElementId;
    }

    /// <summary>
    ///     Writes a value to a parameter, converting to the proper storage type
    ///     and internal units. Returns a human readable error or null on success.
    /// </summary>
    public static string SetParameterValue(Document doc, Parameter param, object value)
    {
        if (param == null) return "Parameter not found";
        if (param.IsReadOnly) return $"Parameter '{param.Definition.Name}' is read-only";

        try
        {
            switch (param.StorageType)
            {
                case StorageType.String:
                    param.Set(value?.ToString() ?? string.Empty);
                    break;

                case StorageType.Integer:
                    if (value is bool b)
                        param.Set(b ? 1 : 0);
                    else if (bool.TryParse(value?.ToString(), out bool b2))
                        param.Set(b2 ? 1 : 0);
                    else
                        param.Set(Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture));
                    break;

                case StorageType.Double:
                    double raw = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                    param.Set(ToInternalUnits(raw, param));
                    break;

                case StorageType.ElementId:
                    int idValue = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
                    param.Set(new ElementId(idValue));
                    break;

                default:
                    return $"Unsupported storage type: {param.StorageType}";
            }
            return null;
        }
        catch (Exception ex)
        {
            return $"Failed to set '{param.Definition.Name}': {ex.Message}";
        }
    }
}
