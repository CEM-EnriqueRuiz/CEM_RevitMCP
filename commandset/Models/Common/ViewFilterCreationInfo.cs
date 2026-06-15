using Newtonsoft.Json;

namespace CEM_IAModeler_CommandSet.Models.Common;

/// <summary>
///     Generic information for creating a ParameterFilterElement and applying
///     it to a view with graphic overrides.
/// </summary>
public class ViewFilterCreationInfo
{
    /// <summary>
    ///     Filter name (required, must be unique; existing filter with the
    ///     same name will be reused and re-applied)
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Categories the filter applies to (BuiltInCategory names, e.g. "OST_Walls")
    /// </summary>
    [JsonProperty("categories")]
    public List<string> Categories { get; set; } = new();

    /// <summary>
    ///     Filter rules, combined with logical AND
    /// </summary>
    [JsonProperty("rules")]
    public List<FilterRuleInfo> Rules { get; set; } = new();

    /// <summary>
    ///     View to apply the filter to. -1 = active view. Set applyToView=false to only create the filter.
    /// </summary>
    [JsonProperty("viewId")]
    public int ViewId { get; set; } = -1;

    /// <summary>
    ///     Whether to apply the filter to a view (otherwise it is only created in the project)
    /// </summary>
    [JsonProperty("applyToView")]
    public bool ApplyToView { get; set; } = true;

    /// <summary>
    ///     Element visibility for elements matching the filter in the view
    /// </summary>
    [JsonProperty("visible")]
    public bool Visible { get; set; } = true;

    /// <summary>
    ///     Graphic overrides for matching elements
    /// </summary>
    [JsonProperty("overrides")]
    public FilterOverrideInfo Overrides { get; set; }
}

/// <summary>
///     One filter rule
/// </summary>
public class FilterRuleInfo
{
    /// <summary>
    ///     Parameter name as displayed in Revit (project/shared parameters are resolved by name)
    /// </summary>
    [JsonProperty("parameterName")]
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    ///     Optional BuiltInParameter enum name (takes priority over parameterName)
    /// </summary>
    [JsonProperty("builtInParameter")]
    public string BuiltInParameter { get; set; } = string.Empty;

    /// <summary>
    ///     Operator: Equals, NotEquals, Contains, NotContains, BeginsWith, EndsWith,
    ///     GreaterThan, GreaterOrEqual, LessThan, LessOrEqual, HasValue, HasNoValue
    /// </summary>
    [JsonProperty("operator")]
    public string Operator { get; set; } = "Equals";

    /// <summary>
    ///     Comparison value (string, number or integer). Numeric lengths are in mm.
    /// </summary>
    [JsonProperty("value")]
    public object Value { get; set; }

    /// <summary>
    ///     Value type hint: "string", "double", "integer", "elementid". Inferred when omitted.
    /// </summary>
    [JsonProperty("valueType")]
    public string ValueType { get; set; } = string.Empty;

    /// <summary>
    ///     For double values: treat the value as millimeters and convert to feet. Default true.
    /// </summary>
    [JsonProperty("convertMillimeters")]
    public bool ConvertMillimeters { get; set; } = true;
}

/// <summary>
///     Graphic override settings for filtered elements
/// </summary>
public class FilterOverrideInfo
{
    /// <summary>
    ///     RGB color [r,g,b] applied to projection lines and surface/cut patterns
    /// </summary>
    [JsonProperty("color")]
    public int[] Color { get; set; }

    /// <summary>
    ///     Fill the surface with a solid pattern in the override color
    /// </summary>
    [JsonProperty("solidFill")]
    public bool SolidFill { get; set; } = true;

    /// <summary>
    ///     Surface transparency 0-100
    /// </summary>
    [JsonProperty("transparency")]
    public int? Transparency { get; set; }

    /// <summary>
    ///     Projection line weight 1-16
    /// </summary>
    [JsonProperty("lineWeight")]
    public int? LineWeight { get; set; }

    /// <summary>
    ///     Render matching elements as halftone
    /// </summary>
    [JsonProperty("halftone")]
    public bool Halftone { get; set; } = false;
}
