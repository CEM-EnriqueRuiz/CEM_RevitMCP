using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CEM_IAModeler_CommandSet.Models.Cemengal
{
    // DTOs of the cem_* tools: the Cemengal add-ins of the sibling CEM_RevitAPI repo, reached through the
    // MCP. They carry plain values only - never a type of the add-ins - so the command set keeps loading
    // (Assembly.GetTypes) even where the add-ins are not deployed.

    // ───────────────────────────── cem_open_tool ─────────────────────────────

    /// <summary>cem_open_tool — press a button of the Cemengal ribbon tab for the user.</summary>
    public class OpenToolRequest
    {
        /// <summary>Button label ("CEM Swap"), or the add-in's name ("CEM_SwapManager", "SwapManager"). Empty = list the buttons.</summary>
        [JsonProperty("tool")] public string Tool { get; set; } = "";
    }

    public class RibbonToolInfo
    {
        [JsonProperty("panel")] public string Panel { get; set; }
        [JsonProperty("label")] public string Label { get; set; }
        [JsonProperty("addin")] public string AddIn { get; set; }
        [JsonProperty("enabled")] public bool Enabled { get; set; }
    }

    public class OpenToolResult
    {
        /// <summary>The label of the button pressed; null when nothing was pressed.</summary>
        [JsonProperty("opened")] public string Opened { get; set; }
        [JsonProperty("tools")] public List<RibbonToolInfo> Tools { get; set; } = new List<RibbonToolInfo>();
    }

    // ───────────────────────────── cem_swap_* ─────────────────────────────

    /// <summary>cem_swap_find_types — the types CEM Swap can take from or place.</summary>
    public class SwapFindTypesRequest
    {
        /// <summary>Text contained in "Category Family Type" (case-insensitive). Empty = all.</summary>
        [JsonProperty("text")] public string Text { get; set; } = "";
        [JsonProperty("placedOnly")] public bool PlacedOnly { get; set; }
        [JsonProperty("max")] public int Max { get; set; } = 100;
    }

    public class SwapTypeInfo
    {
        [JsonProperty("id")] public long Id { get; set; }
        [JsonProperty("category")] public string Category { get; set; }
        [JsonProperty("family")] public string Family { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("placed")] public int Placed { get; set; }
        [JsonProperty("creatable")] public bool Creatable { get; set; }
        [JsonProperty("reason")] public string Reason { get; set; }
    }

    public class SwapFindTypesResult
    {
        [JsonProperty("matching")] public int Matching { get; set; }
        [JsonProperty("types")] public List<SwapTypeInfo> Types { get; set; } = new List<SwapTypeInfo>();
    }

    /// <summary>cem_swap_list_presets — the office presets in SwapPresets.json.</summary>
    public class SwapListPresetsRequest { }

    public class SwapPresetInfo
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("sources")] public List<string> Sources { get; set; } = new List<string>();
        [JsonProperty("target")] public string Target { get; set; }
        [JsonProperty("links")] public List<string> Links { get; set; } = new List<string>();
        [JsonProperty("offset")] public string Offset { get; set; }
        [JsonProperty("placement")] public string Placement { get; set; }
        [JsonProperty("conditions")] public List<string> Conditions { get; set; } = new List<string>();
        [JsonProperty("watches")] public List<string> Watches { get; set; } = new List<string>();
    }

    public class SwapListPresetsResult
    {
        [JsonProperty("path")] public string Path { get; set; }
        [JsonProperty("problem")] public string Problem { get; set; }
        [JsonProperty("presets")] public List<SwapPresetInfo> Presets { get; set; } = new List<SwapPresetInfo>();
    }

    /// <summary>cem_swap_run — a trial on one element, or the run over every candidate.</summary>
    public class SwapRunRequest
    {
        /// <summary>Name of a preset in SwapPresets.json. Either this or <see cref="Swap"/>.</summary>
        [JsonProperty("preset")] public string Preset { get; set; } = "";

        /// <summary>A preset written inline, in the SwapPresets.json shape (sources, target, links, offset, conditions, placement, watches).</summary>
        [JsonProperty("swap")] public JObject Swap { get; set; }

        /// <summary>"trial" (one element, replacing any trial standing) or "all".</summary>
        [JsonProperty("mode")] public string Mode { get; set; } = "trial";

        [JsonProperty("maxPairs")] public int MaxPairs { get; set; } = 50;
    }

    public class SwapValueInfo
    {
        [JsonProperty("target")] public string Target { get; set; }
        [JsonProperty("source")] public string Source { get; set; }
        [JsonProperty("result")] public string Result { get; set; }
        [JsonProperty("landed")] public bool Landed { get; set; }
        [JsonProperty("detail")] public string Detail { get; set; }
    }

    public class SwapPairInfo
    {
        [JsonProperty("oldId")] public long OldId { get; set; }
        [JsonProperty("oldLabel")] public string OldLabel { get; set; }
        [JsonProperty("newIds")] public List<long> NewIds { get; set; } = new List<long>();
        [JsonProperty("refusal")] public string Refusal { get; set; }
        [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new List<string>();
        [JsonProperty("values")] public List<SwapValueInfo> Values { get; set; } = new List<SwapValueInfo>();
    }

    public class SwapRunResult
    {
        [JsonProperty("mode")] public string Mode { get; set; }
        [JsonProperty("committed")] public bool Committed { get; set; }
        [JsonProperty("target")] public string Target { get; set; }

        /// <summary>Old elements the conditions allow (placed, top-level, outside groups, not yet replaced).</summary>
        [JsonProperty("candidates")] public int Candidates { get; set; }
        [JsonProperty("excludedByConditions")] public int ExcludedByConditions { get; set; }
        [JsonProperty("unreadableByConditions")] public int UnreadableByConditions { get; set; }
        [JsonProperty("inGroups")] public int InGroups { get; set; }
        [JsonProperty("alreadyReplaced")] public int AlreadyReplaced { get; set; }

        [JsonProperty("placed")] public int Placed { get; set; }
        [JsonProperty("refused")] public int Refused { get; set; }
        [JsonProperty("withIssues")] public int WithIssues { get; set; }

        /// <summary>The parameter map used, "target ← source", same-name ones marked (auto).</summary>
        [JsonProperty("map")] public List<string> Map { get; set; } = new List<string>();
        [JsonProperty("pairs")] public List<SwapPairInfo> Pairs { get; set; } = new List<SwapPairInfo>();
        [JsonProperty("warnings")] public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>cem_swap_remove_trial — take away the trial standing in the model.</summary>
    public class SwapRemoveTrialRequest { }

    public class SwapRemoveTrialResult
    {
        [JsonProperty("trialElements")] public int TrialElements { get; set; }
        [JsonProperty("removed")] public int Removed { get; set; }
    }

    // ───────────────────────────── cem_rules_apply ─────────────────────────────

    /// <summary>cem_rules_apply — run every CEM Rules rule over the whole model, as the CEM Rules button does.</summary>
    public class RulesApplyRequest
    {
        [JsonProperty("maxDetails")] public int MaxDetails { get; set; } = 30;
    }

    public class RulesApplyResult
    {
        [JsonProperty("errors")] public List<string> Errors { get; set; } = new List<string>();
        [JsonProperty("failedPasses")] public List<string> FailedPasses { get; set; } = new List<string>();
        [JsonProperty("failedElements")] public int FailedElements { get; set; }
        [JsonProperty("nonEditable")] public int NonEditable { get; set; }
        [JsonProperty("missingParameters")] public List<string> MissingParameters { get; set; } = new List<string>();
        [JsonProperty("duplicates")] public List<string> Duplicates { get; set; } = new List<string>();
    }
}
