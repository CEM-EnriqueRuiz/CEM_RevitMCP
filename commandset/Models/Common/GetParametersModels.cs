using Newtonsoft.Json;

namespace CEM_IAModeler_CommandSet.Models.Common;

/// <summary>
///     Request to read parameters from one or more elements.
/// </summary>
public class GetParametersRequest
{
    /// <summary>Elements to read.</summary>
    [JsonProperty("elementIds")]
    public List<long> ElementIds { get; set; } = new();

    /// <summary>
    ///     Optional whitelist of parameter names to return. Empty = return all.
    /// </summary>
    [JsonProperty("parameterNames")]
    public List<string> ParameterNames { get; set; } = new();

    /// <summary>Include type parameters in addition to instance parameters.</summary>
    [JsonProperty("includeType")]
    public bool IncludeType { get; set; } = true;

    /// <summary>Include parameters that have no value.</summary>
    [JsonProperty("includeEmpty")]
    public bool IncludeEmpty { get; set; } = false;
}

/// <summary>One parameter value read back.</summary>
public class ParameterValueInfo
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("value")]
    public string Value { get; set; }

    [JsonProperty("storageType")]
    public string StorageType { get; set; }

    [JsonProperty("isReadOnly")]
    public bool IsReadOnly { get; set; }

    [JsonProperty("isType")]
    public bool IsType { get; set; }

    [JsonProperty("builtIn")]
    public string BuiltIn { get; set; }
}

/// <summary>All parameters read from a single element.</summary>
public class ElementParametersInfo
{
    [JsonProperty("elementId")]
    public long ElementId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("typeName")]
    public string TypeName { get; set; }

    [JsonProperty("found")]
    public bool Found { get; set; }

    [JsonProperty("parameters")]
    public List<ParameterValueInfo> Parameters { get; set; } = new();
}
