using Newtonsoft.Json;

namespace CEM_IAModeler_CommandSet.Models.Common;

// ─────────────────────────────────────────────────────────────────────────────
//  viz_create_material  (Material.Create + shading graphics)
//  Domain: Material (DB) + AppearanceAssetElement (DB.Visual)
// ─────────────────────────────────────────────────────────────────────────────
public class CreateMaterialRequest
{
    /// <summary>Material name. Reused if it already exists (idempotent).</summary>
    [JsonProperty("name")] public string Name { get; set; } = "";

    /// <summary>Shading color as [r,g,b] 0-255. Optional.</summary>
    [JsonProperty("colorRgb")] public List<int> ColorRgb { get; set; }

    /// <summary>Surface transparency 0-100. Optional.</summary>
    [JsonProperty("transparency")] public int? Transparency { get; set; }

    /// <summary>Shininess 0-128. Optional.</summary>
    [JsonProperty("shininess")] public int? Shininess { get; set; }

    /// <summary>Smoothness 0-100. Optional.</summary>
    [JsonProperty("smoothness")] public int? Smoothness { get; set; }

    /// <summary>Surface foreground fill pattern name or id. Optional.</summary>
    [JsonProperty("surfacePatternName")] public string SurfacePatternName { get; set; } = "";

    /// <summary>Cut foreground fill pattern name or id. Optional.</summary>
    [JsonProperty("cutPatternName")] public string CutPatternName { get; set; } = "";

    /// <summary>Existing appearance asset (AppearanceAssetElement) name to assign for rendering. Optional.</summary>
    [JsonProperty("appearanceAssetName")] public string AppearanceAssetName { get; set; } = "";
}

public class MaterialResult
{
    [JsonProperty("materialId")] public long MaterialId { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("applied")] public List<string> Applied { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  viz_set_material_appearance  (edit shading graphics of existing material[s])
// ─────────────────────────────────────────────────────────────────────────────
public class SetMaterialAppearanceRequest
{
    /// <summary>Material name or id to modify.</summary>
    [JsonProperty("material")] public string Material { get; set; } = "";

    [JsonProperty("colorRgb")] public List<int> ColorRgb { get; set; }
    [JsonProperty("transparency")] public int? Transparency { get; set; }
    [JsonProperty("shininess")] public int? Shininess { get; set; }
    [JsonProperty("smoothness")] public int? Smoothness { get; set; }
    [JsonProperty("surfacePatternName")] public string SurfacePatternName { get; set; } = "";
    [JsonProperty("cutPatternName")] public string CutPatternName { get; set; } = "";
    [JsonProperty("appearanceAssetName")] public string AppearanceAssetName { get; set; } = "";
}

// ─────────────────────────────────────────────────────────────────────────────
//  viz_list_materials  (discovery)
// ─────────────────────────────────────────────────────────────────────────────
public class ListMaterialsRequest
{
    /// <summary>Optional case-insensitive substring to filter material names.</summary>
    [JsonProperty("nameFilter")] public string NameFilter { get; set; } = "";
}

public class MaterialInfo
{
    [JsonProperty("id")] public long Id { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("colorRgb")] public List<int> ColorRgb { get; set; }
    [JsonProperty("transparency")] public int Transparency { get; set; }
    [JsonProperty("class")] public string MaterialClass { get; set; }
}

public class ListMaterialsResult
{
    [JsonProperty("count")] public int Count { get; set; }
    [JsonProperty("materials")] public List<MaterialInfo> Materials { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  viz_assign_material  (assign a material to elements, by type param or paint)
// ─────────────────────────────────────────────────────────────────────────────
public class AssignMaterialRequest
{
    /// <summary>Material name or id to assign.</summary>
    [JsonProperty("material")] public string Material { get; set; } = "";

    /// <summary>Element ids to assign the material to.</summary>
    [JsonProperty("elementIds")] public List<long> ElementIds { get; set; } = new();

    /// <summary>
    ///     Mode: "parameter" (set the element/type "Material" parameter, default) or
    ///     "paint" (Document.Paint all faces of the element with the material).
    /// </summary>
    [JsonProperty("mode")] public string Mode { get; set; } = "parameter";
}

public class AssignMaterialResult
{
    [JsonProperty("materialId")] public long MaterialId { get; set; }
    [JsonProperty("assignedCount")] public int AssignedCount { get; set; }
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}
