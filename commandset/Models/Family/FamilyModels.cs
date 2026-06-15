using Newtonsoft.Json;
using CEM_IAModeler_CommandSet.Models.Common;

namespace CEM_IAModeler_CommandSet.Models.Family;

/// <summary>Load a family file (.rfa) into the project.</summary>
public class FamilyLoadRequest
{
    /// <summary>Absolute path to the .rfa file.</summary>
    [JsonProperty("path")]
    public string Path { get; set; } = "";

    /// <summary>Overwrite the family and its parameter values if it already exists.</summary>
    [JsonProperty("overwrite")]
    public bool Overwrite { get; set; } = true;
}

/// <summary>Place instances of a loaded family type at points (mm).</summary>
public class PlaceInstanceRequest
{
    [JsonProperty("typeName")] public string TypeName { get; set; } = "";
    [JsonProperty("points")] public List<JZPoint> Points { get; set; } = new();
    [JsonProperty("level")] public string Level { get; set; } = "";
    /// <summary>Optional host element id (face/wall/etc.). 0 = no host (level based).</summary>
    [JsonProperty("hostId")] public long HostId { get; set; } = 0;
    /// <summary>Rotation about Z in degrees.</summary>
    [JsonProperty("rotation")] public double Rotation { get; set; } = 0;
    /// <summary>Structural type: "NonStructural" | "Beam" | "Column" | "Brace" | "Footing".</summary>
    [JsonProperty("structuralType")] public string StructuralType { get; set; } = "NonStructural";
}

/// <summary>Query loaded family types, optionally filtered by category/family name.</summary>
public class GetFamilyTypesRequest
{
    /// <summary>Optional BuiltInCategory name filter (e.g. "OST_Doors").</summary>
    [JsonProperty("category")] public string Category { get; set; } = "";
    /// <summary>Optional family name substring filter.</summary>
    [JsonProperty("familyName")] public string FamilyName { get; set; } = "";
    /// <summary>Include each type's editable parameters.</summary>
    [JsonProperty("includeParameters")] public bool IncludeParameters { get; set; } = false;
}

public class FamilyTypeSummary
{
    [JsonProperty("typeId")] public long TypeId { get; set; }
    [JsonProperty("familyName")] public string FamilyName { get; set; }
    [JsonProperty("typeName")] public string TypeName { get; set; }
    [JsonProperty("category")] public string Category { get; set; }
    [JsonProperty("parameters")] public List<string> Parameters { get; set; } = new();
}

/// <summary>Duplicate a family type (project context) with parameter overrides.</summary>
public class CreateFamilyTypeRequest
{
    /// <summary>Source type name/id to duplicate.</summary>
    [JsonProperty("sourceTypeName")] public string SourceTypeName { get; set; } = "";
    /// <summary>New type name.</summary>
    [JsonProperty("newTypeName")] public string NewTypeName { get; set; } = "";
    /// <summary>Parameter overrides (name -> value); mm/deg for length/angle params.</summary>
    [JsonProperty("parameters")] public List<CEM_IAModeler_CommandSet.Models.Common.ParamOverride> Parameters { get; set; } = new();
}

/// <summary>Add a parameter to the family being edited (family-editor context).</summary>
public class FamilyAddParameterRequest
{
    [JsonProperty("name")] public string Name { get; set; } = "";
    /// <summary>Data type: Text, Integer, Number, Length, Area, Volume, Angle, YesNo, Material.</summary>
    [JsonProperty("dataType")] public string DataType { get; set; } = "Text";
    /// <summary>UI group: Data, Text, IdentityData, Dimensions, Construction, General, Materials.</summary>
    [JsonProperty("group")] public string Group { get; set; } = "Data";
    [JsonProperty("isInstance")] public bool IsInstance { get; set; } = false;
    /// <summary>Optional formula to set on the new parameter.</summary>
    [JsonProperty("formula")] public string Formula { get; set; } = "";
}

/// <summary>Add a type to the family being edited (family-editor context).</summary>
public class FamilyAddTypeRequest
{
    [JsonProperty("typeName")] public string TypeName { get; set; } = "";
}

/// <summary>Set a formula on a family parameter (family-editor context).</summary>
public class FamilySetFormulaRequest
{
    [JsonProperty("parameterName")] public string ParameterName { get; set; } = "";
    [JsonProperty("formula")] public string Formula { get; set; } = "";
}

public class FamilyOpResult
{
    [JsonProperty("ids")] public List<long> Ids { get; set; } = new();
    [JsonProperty("count")] public int Count { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}
