using Newtonsoft.Json;

namespace CEM_IAModeler_CommandSet.Models.Common;

// ─────────────────────────────────────────────────────────────────────────────
//  coord_manage_worksets  (Workset / WorksetTable / WorksharingUtils)
// ─────────────────────────────────────────────────────────────────────────────
public class ManageWorksetsRequest
{
    /// <summary>"list" | "create" | "set_active" | "assign". Default "list".</summary>
    [JsonProperty("action")] public string Action { get; set; } = "list";

    /// <summary>Workset name (for create / set_active / assign).</summary>
    [JsonProperty("name")] public string Name { get; set; } = "";

    /// <summary>For assign: element ids to move into the named workset.</summary>
    [JsonProperty("elementIds")] public List<long> ElementIds { get; set; } = new();
}

public class WorksetInfo
{
    [JsonProperty("id")] public long Id { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("kind")] public string Kind { get; set; }
    [JsonProperty("isOpen")] public bool IsOpen { get; set; }
    [JsonProperty("isEditable")] public bool IsEditable { get; set; }
}

public class ManageWorksetsResult
{
    [JsonProperty("action")] public string Action { get; set; }
    [JsonProperty("worksetId")] public long WorksetId { get; set; }
    [JsonProperty("assignedCount")] public int AssignedCount { get; set; }
    [JsonProperty("worksets")] public List<WorksetInfo> Worksets { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  coord_link_model  (RevitLinkType/Instance, CADLinkType, IFC)
// ─────────────────────────────────────────────────────────────────────────────
public class LinkModelRequest
{
    /// <summary>"load" | "list" | "reload" | "unload" | "remove". Default "list".</summary>
    [JsonProperty("action")] public string Action { get; set; } = "list";

    /// <summary>Absolute file path for load (.rvt, .ifc, .dwg/.dxf).</summary>
    [JsonProperty("path")] public string Path { get; set; } = "";

    /// <summary>For reload/unload/remove: the link (instance or type) id or name.</summary>
    [JsonProperty("linkId")] public string LinkId { get; set; } = "";
}

public class LinkInfo
{
    [JsonProperty("typeId")] public long TypeId { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("kind")] public string Kind { get; set; }   // Revit | CAD | IFC
    [JsonProperty("status")] public string Status { get; set; }
    [JsonProperty("instanceIds")] public List<long> InstanceIds { get; set; } = new();
}

public class LinkModelResult
{
    [JsonProperty("action")] public string Action { get; set; }
    [JsonProperty("linkTypeId")] public long LinkTypeId { get; set; }
    [JsonProperty("linkInstanceId")] public long LinkInstanceId { get; set; }
    [JsonProperty("links")] public List<LinkInfo> Links { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  coord_purge_unused  (Document.GetUnusedElements)
// ─────────────────────────────────────────────────────────────────────────────
public class PurgeUnusedRequest
{
    /// <summary>
    ///     Optional BuiltInCategory names to limit the purge to (e.g. ["OST_Walls"]).
    ///     Empty = purge unused across all categories.
    /// </summary>
    [JsonProperty("categories")] public List<string> Categories { get; set; } = new();

    /// <summary>When true, only report what would be purged without deleting. Default false.</summary>
    [JsonProperty("dryRun")] public bool DryRun { get; set; } = false;
}

public class PurgeUnusedResult
{
    [JsonProperty("candidateCount")] public int CandidateCount { get; set; }
    [JsonProperty("deletedCount")] public int DeletedCount { get; set; }
    [JsonProperty("dryRun")] public bool DryRun { get; set; }
    [JsonProperty("sample")] public List<string> Sample { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  coord_audit_model  (QA summary — read-only)
// ─────────────────────────────────────────────────────────────────────────────
public class AuditModelRequest
{
    // no parameters; reserved for future scope flags
}

public class AuditModelResult
{
    [JsonProperty("warningCount")] public int WarningCount { get; set; }
    [JsonProperty("topWarnings")] public List<string> TopWarnings { get; set; } = new();
    [JsonProperty("groupTypeCount")] public int GroupTypeCount { get; set; }
    [JsonProperty("groupInstanceCount")] public int GroupInstanceCount { get; set; }
    [JsonProperty("inPlaceFamilyCount")] public int InPlaceFamilyCount { get; set; }
    [JsonProperty("unusedElementCount")] public int UnusedElementCount { get; set; }
    [JsonProperty("linkCount")] public int LinkCount { get; set; }
    [JsonProperty("designOptionCount")] public int DesignOptionCount { get; set; }
    [JsonProperty("worksharing")] public bool Worksharing { get; set; }
    [JsonProperty("notes")] public List<string> Notes { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
//  coord_manage_phases  (Phase / PhaseFilter, element phase params)
// ─────────────────────────────────────────────────────────────────────────────
public class ManagePhasesRequest
{
    /// <summary>"list" | "set_element_phase". Default "list".</summary>
    [JsonProperty("action")] public string Action { get; set; } = "list";

    /// <summary>For set_element_phase: element ids to update.</summary>
    [JsonProperty("elementIds")] public List<long> ElementIds { get; set; } = new();

    /// <summary>Phase name or id to set as "Phase Created". Optional.</summary>
    [JsonProperty("phaseCreated")] public string PhaseCreated { get; set; } = "";

    /// <summary>Phase name or id to set as "Phase Demolished". Optional ("None" to clear).</summary>
    [JsonProperty("phaseDemolished")] public string PhaseDemolished { get; set; } = "";
}

public class PhaseInfo
{
    [JsonProperty("id")] public long Id { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
}

public class ManagePhasesResult
{
    [JsonProperty("action")] public string Action { get; set; }
    [JsonProperty("updatedCount")] public int UpdatedCount { get; set; }
    [JsonProperty("phases")] public List<PhaseInfo> Phases { get; set; } = new();
    [JsonProperty("phaseFilters")] public List<PhaseInfo> PhaseFilters { get; set; } = new();
    [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new();
}
