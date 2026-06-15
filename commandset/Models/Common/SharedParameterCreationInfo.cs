using Newtonsoft.Json;

namespace CEM_IAModeler_CommandSet.Models.Common;

/// <summary>
///     Generic information for creating a shared parameter and binding it to categories
/// </summary>
public class SharedParameterCreationInfo
{
    /// <summary>
    ///     Parameter name (required)
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Data type: Text, Integer, Number, Length, Area, Volume, Angle, YesNo, Material, URL
    /// </summary>
    [JsonProperty("dataType")]
    public string DataType { get; set; } = "Text";

    /// <summary>
    ///     Group name inside the shared parameter file (created if missing)
    /// </summary>
    [JsonProperty("definitionGroup")]
    public string DefinitionGroup { get; set; } = "MCP";

    /// <summary>
    ///     UI group where the parameter appears in the properties palette:
    ///     Data, Text, IdentityData, Dimensions, Construction, General, Materials
    /// </summary>
    [JsonProperty("parameterGroup")]
    public string ParameterGroup { get; set; } = "Data";

    /// <summary>
    ///     True = instance binding, false = type binding
    /// </summary>
    [JsonProperty("isInstance")]
    public bool IsInstance { get; set; } = true;

    /// <summary>
    ///     Categories to bind the parameter to (BuiltInCategory names, e.g. "OST_Walls")
    /// </summary>
    [JsonProperty("categories")]
    public List<string> Categories { get; set; } = new();

    /// <summary>
    ///     Allow values to vary between group instances (instance parameters only)
    /// </summary>
    [JsonProperty("varyBetweenGroups")]
    public bool VaryBetweenGroups { get; set; } = false;

    /// <summary>
    ///     Optional explicit GUID. If empty a new one is generated (or an
    ///     existing definition with the same name in the group is reused).
    /// </summary>
    [JsonProperty("guid")]
    public string Guid { get; set; } = string.Empty;
}

/// <summary>
///     Result of a shared parameter creation/binding
/// </summary>
public class SharedParameterResultInfo
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("guid")]
    public string Guid { get; set; }

    [JsonProperty("dataType")]
    public string DataType { get; set; }

    [JsonProperty("isInstance")]
    public bool IsInstance { get; set; }

    [JsonProperty("boundCategories")]
    public List<string> BoundCategories { get; set; } = new();

    [JsonProperty("alreadyExisted")]
    public bool AlreadyExisted { get; set; }
}
