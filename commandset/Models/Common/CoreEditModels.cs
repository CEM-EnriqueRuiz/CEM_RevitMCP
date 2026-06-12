using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Common;

/// <summary>A parameter name/value override, shared by type-duplication tools.</summary>
public class ParamOverride
{
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("value")] public object Value { get; set; }
}

/// <summary>Generic id-list request used by several core editing tools.</summary>
public class ElementIdsRequest
{
    [JsonProperty("elementIds")] public List<long> ElementIds { get; set; } = new();
}

/// <summary>Delete elements by id.</summary>
public class DeleteElementsRequest
{
    [JsonProperty("elementIds")] public List<long> ElementIds { get; set; } = new();
}

/// <summary>Duplicate a type (ElementType.Duplicate) with parameter overrides.</summary>
public class DuplicateTypeRequest
{
    [JsonProperty("sourceTypeName")] public string SourceTypeName { get; set; } = "";
    [JsonProperty("newTypeName")] public string NewTypeName { get; set; } = "";
    [JsonProperty("parameters")] public List<ParamOverride> Parameters { get; set; } = new();
}

/// <summary>Group elements together.</summary>
public class GroupElementsRequest
{
    [JsonProperty("elementIds")] public List<long> ElementIds { get; set; } = new();
    [JsonProperty("name")] public string Name { get; set; } = "";
}

/// <summary>Pin or unpin elements.</summary>
public class PinElementsRequest
{
    [JsonProperty("elementIds")] public List<long> ElementIds { get; set; } = new();
    [JsonProperty("pinned")] public bool Pinned { get; set; } = true;
}

/// <summary>Rename an element (or type) by id.</summary>
public class RenameElementRequest
{
    [JsonProperty("elementId")] public long ElementId { get; set; }
    [JsonProperty("newName")] public string NewName { get; set; } = "";
}

/// <summary>Copy parameter values from a source element to many targets.</summary>
public class CopyParameterValuesRequest
{
    [JsonProperty("sourceId")] public long SourceId { get; set; }
    [JsonProperty("targetIds")] public List<long> TargetIds { get; set; } = new();
    /// <summary>Parameter names to copy. Empty = copy all writable matching instance params.</summary>
    [JsonProperty("parameterNames")] public List<string> ParameterNames { get; set; } = new();
}

/// <summary>Set a parameter on all elements matching a category (and optional value rule).</summary>
public class BulkSetByFilterRequest
{
    [JsonProperty("categories")] public List<string> Categories { get; set; } = new();
    [JsonProperty("parameterName")] public string ParameterName { get; set; } = "";
    [JsonProperty("builtInParameter")] public string BuiltInParameter { get; set; } = "";
    [JsonProperty("value")] public object Value { get; set; }
    /// <summary>Only in the active view.</summary>
    [JsonProperty("activeViewOnly")] public bool ActiveViewOnly { get; set; } = false;
    [JsonProperty("isTypeParameter")] public bool IsTypeParameter { get; set; } = false;
}

/// <summary>List parameters available on a category (from a sample element of that category).</summary>
public class ListParamsForCategoryRequest
{
    [JsonProperty("category")] public string Category { get; set; } = "";
    [JsonProperty("includeType")] public bool IncludeType { get; set; } = true;
}

/// <summary>Create a project parameter bound to categories (no shared file).</summary>
public class CreateProjectParameterRequest
{
    [JsonProperty("name")] public string Name { get; set; } = "";
    [JsonProperty("dataType")] public string DataType { get; set; } = "Text";
    [JsonProperty("group")] public string Group { get; set; } = "Data";
    [JsonProperty("isInstance")] public bool IsInstance { get; set; } = true;
    [JsonProperty("categories")] public List<string> Categories { get; set; } = new();
}

/// <summary>Find elements by category and optional parameter value filter.</summary>
public class FindElementsRequest
{
    [JsonProperty("categories")] public List<string> Categories { get; set; } = new();
    [JsonProperty("parameterName")] public string ParameterName { get; set; } = "";
    [JsonProperty("value")] public string Value { get; set; } = "";
    /// <summary>Match mode: "equals" | "contains". Default contains.</summary>
    [JsonProperty("match")] public string Match { get; set; } = "contains";
    [JsonProperty("activeViewOnly")] public bool ActiveViewOnly { get; set; } = false;
    [JsonProperty("limit")] public int Limit { get; set; } = 500;
}

/// <summary>Generic per-element result list.</summary>
public class CoreEditResult
{
    [JsonProperty("ids")] public List<long> Ids { get; set; } = new();
    [JsonProperty("count")] public int Count { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

/// <summary>Compact identity/placement info for an element.</summary>
public class ElementInfoSummary
{
    [JsonProperty("elementId")] public long ElementId { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("category")] public string Category { get; set; }
    [JsonProperty("familyName")] public string FamilyName { get; set; }
    [JsonProperty("typeName")] public string TypeName { get; set; }
    [JsonProperty("typeId")] public long TypeId { get; set; }
    [JsonProperty("levelName")] public string LevelName { get; set; }
    [JsonProperty("hostId")] public long HostId { get; set; }
    [JsonProperty("found")] public bool Found { get; set; }
}

/// <summary>Geometry summary for an element (mm).</summary>
public class ElementGeometryInfo
{
    [JsonProperty("elementId")] public long ElementId { get; set; }
    [JsonProperty("found")] public bool Found { get; set; }
    [JsonProperty("locationType")] public string LocationType { get; set; }
    [JsonProperty("point")] public JZPoint Point { get; set; }
    [JsonProperty("start")] public JZPoint Start { get; set; }
    [JsonProperty("end")] public JZPoint End { get; set; }
    [JsonProperty("lengthMm")] public double? LengthMm { get; set; }
    [JsonProperty("bboxMin")] public JZPoint BboxMin { get; set; }
    [JsonProperty("bboxMax")] public JZPoint BboxMax { get; set; }
    [JsonProperty("areaMm2")] public double? AreaMm2 { get; set; }
    [JsonProperty("volumeMm3")] public double? VolumeMm3 { get; set; }
}

/// <summary>A parameter descriptor (for category discovery).</summary>
public class ParamDescriptor
{
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("storageType")] public string StorageType { get; set; }
    [JsonProperty("isReadOnly")] public bool IsReadOnly { get; set; }
    [JsonProperty("isType")] public bool IsType { get; set; }
    [JsonProperty("builtIn")] public string BuiltIn { get; set; }
}
