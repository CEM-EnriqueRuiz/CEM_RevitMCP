using Newtonsoft.Json;

namespace CEM_IAModeler_CommandSet.Models.Common;

/// <summary>
///     Generic information for creating a global parameter, setting its value
///     and associating it to element parameters (parameter association).
/// </summary>
public class GlobalParameterCreationInfo
{
    /// <summary>
    ///     Global parameter name (required; reused if it already exists)
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Data type: Text, Integer, Number, Length, Area, Volume, Angle, YesNo
    /// </summary>
    [JsonProperty("dataType")]
    public string DataType { get; set; } = "Length";

    /// <summary>
    ///     Value to assign. Length/Area/Volume in mm/mm²/mm³, angles in degrees.
    /// </summary>
    [JsonProperty("value")]
    public object Value { get; set; }

    /// <summary>
    ///     Element parameters to drive with this global parameter
    /// </summary>
    [JsonProperty("associations")]
    public List<ParameterAssociationInfo> Associations { get; set; } = new();

    /// <summary>
    ///     Mark as reporting parameter (reads value from the model instead of driving it)
    /// </summary>
    [JsonProperty("isReporting")]
    public bool IsReporting { get; set; } = false;
}

/// <summary>
///     One element-parameter association target
/// </summary>
public class ParameterAssociationInfo
{
    [JsonProperty("elementId")]
    public int ElementId { get; set; }

    /// <summary>
    ///     Parameter name as displayed in Revit
    /// </summary>
    [JsonProperty("parameterName")]
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    ///     Optional BuiltInParameter enum name (takes priority over parameterName)
    /// </summary>
    [JsonProperty("builtInParameter")]
    public string BuiltInParameter { get; set; } = string.Empty;
}

/// <summary>
///     Result of a global parameter operation
/// </summary>
public class GlobalParameterResultInfo
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("alreadyExisted")]
    public bool AlreadyExisted { get; set; }

    [JsonProperty("associatedCount")]
    public int AssociatedCount { get; set; }

    [JsonProperty("associationErrors")]
    public List<string> AssociationErrors { get; set; } = new();
}
