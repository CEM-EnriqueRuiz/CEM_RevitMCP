using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Common;

/// <summary>
///     One generic parameter write request. Targets multiple elements at once.
/// </summary>
public class ParameterSetRequest
{
    /// <summary>
    ///     Elements to modify
    /// </summary>
    [JsonProperty("elementIds")]
    public List<int> ElementIds { get; set; } = new();

    /// <summary>
    ///     Parameter name as displayed in Revit (used with LookupParameter).
    ///     Ignored if builtInParameter is provided.
    /// </summary>
    [JsonProperty("parameterName")]
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    ///     Optional BuiltInParameter enum name (e.g. "ALL_MODEL_INSTANCE_COMMENTS").
    ///     Takes priority over parameterName when provided.
    /// </summary>
    [JsonProperty("builtInParameter")]
    public string BuiltInParameter { get; set; } = string.Empty;

    /// <summary>
    ///     Value to set. Strings, numbers and booleans are accepted; the handler
    ///     converts to the parameter's storage type. Numeric Length/Area/Volume
    ///     values are interpreted as mm / mm² / mm³, angles as degrees.
    /// </summary>
    [JsonProperty("value")]
    public object Value { get; set; }

    /// <summary>
    ///     If true the parameter is looked up on the element's type instead of the instance
    /// </summary>
    [JsonProperty("isTypeParameter")]
    public bool IsTypeParameter { get; set; } = false;

    /// <summary>
    ///     If the parameter is not found on the instance, also try the type (and vice versa)
    /// </summary>
    [JsonProperty("fallbackToOtherScope")]
    public bool FallbackToOtherScope { get; set; } = true;
}

/// <summary>
///     Per-element outcome of a parameter write
/// </summary>
public class ParameterSetResult
{
    [JsonProperty("elementId")]
    public int ElementId { get; set; }

    [JsonProperty("parameter")]
    public string Parameter { get; set; }

    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("newValue")]
    public string NewValue { get; set; }
}
