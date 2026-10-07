using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Cemengal;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_RevitAPI_Extended;
using CEM_Rules.Services;
using CEM_SwapManager.Models;
using CEM_SwapManager.Presentation;
using CEM_SwapManager.Services;
using CEM_SwapManager.ViewModels;
using RevitMCPSDK.API.Interfaces;
using AW = Autodesk.Windows;

namespace CEM_IAModeler_CommandSet.Services.Cemengal
{
    // ─────────────────────────────────────────────────────────────────────────
    //  The cem_* tools: the Cemengal add-ins of the sibling CEM_RevitAPI repo, called through their own
    //  code (ProjectReferences), so a change to an add-in reaches the MCP on the next build.
    //
    //  The add-ins' types appear only inside Run bodies. The plugin loads this assembly with
    //  GetTypes(): a field, base type or signature naming an add-in type would make every command of
    //  the set fail to load wherever the add-ins are missing. Inside Run, a missing add-in fails that
    //  one call, inside Execute's try.
    // ─────────────────────────────────────────────────────────────────────────

    public abstract class CemengalEventHandler<TRequest, TResult> : IExternalEventHandler, IWaitableExternalEventHandler
        where TResult : class, new()
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public TRequest Request { get; set; }
        public AIResult<TResult> Result { get; private set; }

        public void Execute(UIApplication app)
        {
            try
            {
                Result = Run(app);
            }
            catch (Exception ex)
            {
                Result = new AIResult<TResult> { Success = false, Message = $"Error: {ex.Message}", Response = new TResult() };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        protected abstract AIResult<TResult> Run(UIApplication app);

        protected static AIResult<TResult> Done(string message, TResult response) =>
            new AIResult<TResult> { Success = true, Message = message, Response = response };

        protected static AIResult<TResult> Refused(string message, TResult response) =>
            new AIResult<TResult> { Success = false, Message = message, Response = response };

        protected static Document ProjectDocument(UIApplication app)
        {
            Document doc = app.ActiveUIDocument?.Document ?? throw new InvalidOperationException("No model is open in Revit.");
            if (doc.IsFamilyDocument)
                throw new InvalidOperationException("The Cemengal tools work in a project, not in a family document.");
            return doc;
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public abstract string GetName();
    }

    // ───────────────────────────── cem_open_tool ─────────────────────────────

    /// <summary>Presses a Cemengal ribbon button, found by its label or add-in, exactly as the user would.</summary>
    public class OpenToolEventHandler : CemengalEventHandler<OpenToolRequest, OpenToolResult>
    {
        private const string TabName = "Cemengal";

        protected override AIResult<OpenToolResult> Run(UIApplication app)
        {
            var res = new OpenToolResult();
            List<(RibbonToolInfo Info, AW.RibbonItem Item)> buttons = CemengalButtons();
            res.Tools = buttons.Select(button => button.Info).ToList();

            if (buttons.Count == 0)
                return Refused("There is no Cemengal tab in this Revit: CEM_RibbonUI is not loaded.", res);
            if (string.IsNullOrWhiteSpace(Request?.Tool))
                return Done($"{buttons.Count} Cemengal tools. Give one as 'tool' to open it.", res);

            string wanted = Normalize(Request.Tool);
            var matches = buttons.Where(button => Keys(button.Info).Any(key => key == wanted)).ToList();
            if (matches.Count == 0)
                matches = buttons.Where(button => Keys(button.Info).Any(key => key.Contains(wanted))).ToList();

            if (matches.Count != 1)
                return Refused(matches.Count == 0
                    ? $"No Cemengal tool matches '{Request.Tool}'. Tools: {string.Join(", ", res.Tools.Select(t => t.Label))}."
                    : $"'{Request.Tool}' matches several tools: {string.Join(", ", matches.Select(m => m.Info.Label))}. Name one.", res);

            var (info, item) = matches[0];
            if (item.Id.IndexOf("MCPServiceConnection", StringComparison.OrdinalIgnoreCase) >= 0)
                return Refused($"'{info.Label}' opens and closes this MCP connection; pressing it would cut the call. The user presses it.", res);
            if (!info.Enabled)
                return Refused($"'{info.Label}' is disabled: the Cemengal license is not valid. The user activates it from the CEM_License panel.", res);

            RevitCommandId command = RevitCommandId.LookupCommandId(item.Id);
            if (command == null || !app.CanPostCommand(command))
                return Refused($"Revit cannot post '{info.Label}' (command id '{item.Id}').", res);

            app.PostCommand(command);
            res.Opened = info.Label;
            return Done($"Pressed '{info.Label}' ({info.AddIn}). It opens when this call returns. Most Cemengal windows are modal: " +
                        "until the user closes it, MCP calls time out. CEM Swap is modeless.", res);
        }

        private static List<(RibbonToolInfo, AW.RibbonItem)> CemengalButtons()
        {
            var buttons = new List<(RibbonToolInfo, AW.RibbonItem)>();
            AW.RibbonTab tab = AW.ComponentManager.Ribbon?.Tabs.FirstOrDefault(t => t.Id == TabName || t.Title == TabName);
            if (tab == null) return buttons;

            foreach (AW.RibbonPanel panel in tab.Panels)
                foreach (AW.RibbonItem item in Flatten(panel.Source.Items))
                {
                    if (!(item is AW.RibbonButton) || string.IsNullOrEmpty(item.Id)) continue;
                    buttons.Add((new RibbonToolInfo
                    {
                        Panel = panel.Source.Title,
                        Label = (item.Text ?? item.Name ?? "").Replace("\r", " ").Replace("\n", " ").Trim(),
                        AddIn = AddInOf(item.Id),
                        Enabled = item.IsEnabled
                    }, item));
                }

            return buttons;
        }

        private static IEnumerable<AW.RibbonItem> Flatten(IEnumerable<AW.RibbonItem> items)
        {
            foreach (AW.RibbonItem item in items)
            {
                yield return item;
                if (item is AW.RibbonRowPanel row)
                    foreach (AW.RibbonItem inner in Flatten(row.Items)) yield return inner;
            }
        }

        // An API button's id ends with its command class: "CustomCtrl_%CustomCtrl_%Cemengal%Swap%CEM_SwapManager.Commands.CEM_SwapManager_Command".
        private static string AddInOf(string id)
        {
            string command = id.Substring(id.LastIndexOf('%') + 1);
            int dot = command.IndexOf('.');
            return dot > 0 ? command.Substring(0, dot) : command;
        }

        private static IEnumerable<string> Keys(RibbonToolInfo info)
        {
            yield return Normalize(info.Label);
            yield return Normalize(info.AddIn);
            if (info.AddIn != null && info.AddIn.StartsWith("CEM_", StringComparison.OrdinalIgnoreCase))
                yield return Normalize(info.AddIn.Substring(4));
        }

        private static string Normalize(string text) =>
            new string((text ?? "").Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

        public override string GetName() => "Cemengal: Open Tool";
    }

    // ───────────────────────────── cem_swap_* ─────────────────────────────

    /// <summary>The types CEM Swap lists in its browsers: what can be replaced, and what can be placed.</summary>
    public class SwapFindTypesEventHandler : CemengalEventHandler<SwapFindTypesRequest, SwapFindTypesResult>
    {
        protected override AIResult<SwapFindTypesResult> Run(UIApplication app)
        {
            var xdoc = new XDocument(ProjectDocument(app), lightMode: true);
            string text = Request?.Text?.Trim() ?? "";

            var matching = TypeCatalog.Read(xdoc)
                .Where(entry => !(Request?.PlacedOnly ?? false) || entry.Count > 0)
                .Where(entry => text.Length == 0 ||
                                $"{entry.Category} {entry.Family} {entry.Type}".IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(entry => entry.Category).ThenBy(entry => entry.Family).ThenBy(entry => entry.Type)
                .ToList();

            var res = new SwapFindTypesResult
            {
                Matching = matching.Count,
                Types = matching.Take(Math.Max(1, Request?.Max ?? 100)).Select(entry => new SwapTypeInfo
                {
                    Id = entry.Id,
                    Category = entry.Category,
                    Family = entry.Family,
                    Type = entry.Type,
                    Placed = entry.Count,
                    Creatable = entry.Creatable,
                    Reason = entry.Reason
                }).ToList()
            };

            return Done($"{res.Matching} type(s) match" + (res.Types.Count < res.Matching ? $"; showing {res.Types.Count}." : ".") +
                        " A target must be creatable; sources need placed elements.", res);
        }

        public override string GetName() => "Cemengal: Swap Find Types";
    }

    /// <summary>The office presets in SwapPresets.json, as the CEM Swap window lists them.</summary>
    public class SwapListPresetsEventHandler : CemengalEventHandler<SwapListPresetsRequest, SwapListPresetsResult>
    {
        protected override AIResult<SwapListPresetsResult> Run(UIApplication app)
        {
            var res = new SwapListPresetsResult { Path = SwapPresetStore.DefaultPath };
            IReadOnlyList<SwapPreset> presets = SwapPresetStore.Load(SwapPresetStore.DefaultPath, out string problem);
            res.Problem = problem;
            res.Presets = presets.Select(CemSwap.Describe).ToList();

            return problem == null
                ? Done($"{res.Presets.Count} preset(s) in SwapPresets.json.", res)
                : Refused($"SwapPresets.json could not be read: {problem}", res);
        }

        public override string GetName() => "Cemengal: Swap List Presets";
    }

    /// <summary>
    /// A CEM Swap run, resolved as the window resolves it: the target and source types by name, the
    /// candidates the conditions allow, and the parameter map laid out by the window's own
    /// ParameterMapViewModel. A trial replaces one element (the selected one when it is a candidate) and
    /// replaces any trial standing; a full run removes the trial and replaces every candidate. Old
    /// elements are never deleted here.
    /// </summary>
    public class SwapRunEventHandler : CemengalEventHandler<SwapRunRequest, SwapRunResult>
    {
        protected override AIResult<SwapRunResult> Run(UIApplication app)
        {
            bool all = string.Equals(Request?.Mode, "all", StringComparison.OrdinalIgnoreCase);
            var res = new SwapRunResult { Mode = all ? "all" : "trial" };

            ProjectDocument(app);
            SwapPreset preset = CemSwap.ReadPreset(Request?.Preset, Request?.Swap, out string presetProblem);
            if (preset == null) return Refused(presetProblem, res);

            var ui = new XUIDocument(app.ActiveUIDocument);
            XDocument xdoc = ui.Document;
            List<TypeEntry> entries = TypeCatalog.Read(xdoc);

            TypeEntry target = preset.Target == null ? null : TypeCatalog.Find(entries, preset.Target);
            res.Target = preset.Target?.ToString();
            if (target == null) return Refused($"The new type '{preset.Target}' is not loaded in this model.", res);
            if (!target.Creatable) return Refused($"CEM Swap cannot place '{preset.Target}': {target.Reason}", res);

            var sources = new List<TypeEntry>();
            foreach (TypeKey source in preset.Sources)
            {
                TypeEntry entry = TypeCatalog.Find(entries, source);
                if (entry == null) res.Warnings.Add($"Type to replace '{source}' is not in this model.");
                else sources.Add(entry);
            }
            if (sources.Count == 0) return Refused("None of the types to replace is in this model.", res);

            SwapCandidates candidates = SwapSources.OfTypes(xdoc, sources);
            var olds = new List<XElement>();
            foreach (XElement element in candidates.Elements)
            {
                bool? allowed = SwapSources.Allows(element, preset.Conditions);
                if (allowed == true) olds.Add(element);
                else if (allowed == false) res.ExcludedByConditions++;
                else res.UnreadableByConditions++;
            }
            res.Candidates = olds.Count;
            res.InGroups = candidates.InGroups;
            res.AlreadyReplaced = candidates.Covered;
            if (olds.Count == 0)
                return Done($"Nothing to replace: {res.ExcludedByConditions} excluded by the conditions, {res.UnreadableByConditions} unreadable, " +
                            $"{res.InGroups} in groups, {res.AlreadyReplaced} already replaced.", res);

            XElementType targetType = TypeCatalog.Resolve(xdoc, target);
            List<ParameterLink> links = CemSwap.Map(xdoc, candidates, sources, targetType, preset.Links);
            res.Map = links.Select(CemSwap.Describe).ToList();
            var plan = new SwapPlan(targetType, links, preset.Offset, preset.Watches, preset.Placement);

            // The user's own pick, when it is one of the elements being replaced; otherwise the first one.
            var selected = new HashSet<ElementId>(app.ActiveUIDocument.Selection.GetElementIds());
            XElement sample = olds.FirstOrDefault(old => selected.Contains(old.Id)) ?? olds[0];
            List<XElement> chosen = all ? olds : new List<XElement> { sample };

            string key = CemSwap.DocumentKey(xdoc.InternalDocument);
            TypeValueLedger ledger = CemSwap.Ledger(key);
            List<string> standing = SwapLinks.ReadAll(xdoc).Where(link => link.Trial).Select(link => link.NewUniqueId).ToList();
            SwapTrial discard = standing.Count == 0 ? null : new SwapTrial(standing, CemSwap.TrialLedger(key) ?? new TypeValueLedger());
            TypeValueLedger used = all ? ledger : ledger.ForTrial();

            SwapRunReport report = SwapRunner.Run(xdoc, plan, chosen, discard, used, !all, all ? TransactionNames.Swap : TransactionNames.Trial);
            if (report.Committed) CemSwap.SetTrialLedger(key, all ? null : used);

            res.Committed = report.Committed;
            res.Placed = report.Placed;
            res.Refused = report.Refused;
            res.WithIssues = report.Pairs.Count(pair => pair.HasIssues);
            res.Pairs = report.Pairs.Take(Math.Max(1, Request?.MaxPairs ?? 50)).Select(CemSwap.Describe).ToList();
            if (res.Pairs.Count < report.Pairs.Count) res.Warnings.Add($"Showing {res.Pairs.Count} of {report.Pairs.Count} pairs.");

            if (!report.Committed)
                return Refused("Revit did not commit the run: nothing changed in the model.", res);

            if (!all)
            {
                // As the window does: select and show the trial with the element it stands over.
                try { ui.ShowElements(chosen.Concat(report.Pairs.SelectMany(pair => pair.NewUniqueIds).Select(xdoc.GetElement).OfType<XElement>())); }
                catch (Exception ex) { res.Warnings.Add($"The trial was placed but could not be shown: {ex.Message}"); }

                SwapPair pair = report.Pairs.FirstOrDefault();
                return Done(pair == null || pair.NewUniqueIds.Count == 0
                        ? $"Trial refused on {sample.Name}: {pair?.Refusal}"
                        : $"Trial placed over {pair.OldLabel} and selected" + (pair.HasIssues ? ", with issues (see pairs)" : "") +
                          $". {olds.Count} candidate(s) in total. Check it, then cem_swap_run mode 'all', or cem_swap_remove_trial." +
                          CemSwap.WarningsText(res.Warnings), res);
            }

            return Done($"Replaced {res.Placed} of {chosen.Count} ({res.Refused} refused, {res.WithIssues} with issues). " +
                        "The old elements are still in the model: the user reviews the pairs and deletes them in CEM Swap (cem_open_tool 'CEM Swap')." +
                        CemSwap.WarningsText(res.Warnings), res);
        }

        public override string GetName() => "Cemengal: Swap Run";
    }

    /// <summary>Takes away the trial standing in the model and puts back the type values it changed.</summary>
    public class SwapRemoveTrialEventHandler : CemengalEventHandler<SwapRemoveTrialRequest, SwapRemoveTrialResult>
    {
        protected override AIResult<SwapRemoveTrialResult> Run(UIApplication app)
        {
            var xdoc = new XDocument(ProjectDocument(app), lightMode: true);
            var res = new SwapRemoveTrialResult();
            List<string> trial = SwapLinks.ReadAll(xdoc).Where(link => link.Trial).Select(link => link.NewUniqueId).ToList();
            res.TrialElements = trial.Count;
            if (trial.Count == 0) return Done("There is no CEM Swap trial in the model.", res);

            string key = CemSwap.DocumentKey(xdoc.InternalDocument);
            TypeValueLedger ledger = CemSwap.TrialLedger(key);
            IEnumerable<TypeValueSnapshot> restore = ledger?.Originals.Values ?? Enumerable.Empty<TypeValueSnapshot>();
            res.Removed = ElementRemover.DeleteReplacements(xdoc, trial, restore, TransactionNames.DiscardTrial).Count;
            if (res.Removed == trial.Count) CemSwap.SetTrialLedger(key, null);

            return res.Removed == trial.Count
                ? Done($"Removed the trial ({res.Removed} element(s))" + (ledger == null ? "." : " and put back the type values it changed."), res)
                : Refused($"Removed {res.Removed} of {trial.Count} trial element(s); Revit refused the rest.", res);
        }

        public override string GetName() => "Cemengal: Swap Remove Trial";
    }

    /// <summary>What the cem_swap_* handlers share. Statics only: its signatures name add-in types, so it is never a field type.</summary>
    internal static class CemSwap
    {
        // One job ledger and one trial ledger per open model, across calls, as the window keeps them per
        // session. Held as object so no static field names an add-in type.
        private static readonly Dictionary<string, object> Ledgers = new Dictionary<string, object>();

        public static string DocumentKey(Document doc) => string.IsNullOrEmpty(doc.PathName) ? doc.Title : doc.PathName;

        public static TypeValueLedger Ledger(string key)
        {
            if (Ledgers.TryGetValue("job|" + key, out object held) && held is TypeValueLedger ledger) return ledger;
            ledger = new TypeValueLedger();
            Ledgers["job|" + key] = ledger;
            return ledger;
        }

        public static TypeValueLedger TrialLedger(string key) =>
            Ledgers.TryGetValue("trial|" + key, out object held) ? held as TypeValueLedger : null;

        public static void SetTrialLedger(string key, TypeValueLedger ledger)
        {
            if (ledger == null) Ledgers.Remove("trial|" + key);
            else Ledgers["trial|" + key] = ledger;
        }

        /// <summary>A preset by name from SwapPresets.json, or one written inline, read by the store's own parser.</summary>
        public static SwapPreset ReadPreset(string name, Newtonsoft.Json.Linq.JObject inline, out string problem)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                IReadOnlyList<SwapPreset> presets = SwapPresetStore.Load(SwapPresetStore.DefaultPath, out problem);
                SwapPreset preset = presets.FirstOrDefault(p => p.Name == name)
                                    ?? presets.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
                if (preset == null)
                    problem = $"No preset '{name}' in SwapPresets.json" + (problem != null ? $" ({problem})" : "") +
                              $". Presets: {string.Join(", ", presets.Select(p => p.Name))}.";
                return preset;
            }

            if (inline != null)
            {
                var copy = (Newtonsoft.Json.Linq.JObject)inline.DeepClone();
                if (copy["name"] == null && copy["Name"] == null) copy["name"] = "MCP";
                SwapPreset preset = SwapPresetStore.Parse("[" + copy.ToString(Newtonsoft.Json.Formatting.None) + "]", out problem).FirstOrDefault();
                if (preset == null) problem = $"The swap could not be read: {problem}";
                else if (preset.Target == null || preset.Sources.Count == 0) { problem = "The swap needs 'sources' and a 'target'."; return null; }
                return preset;
            }

            problem = "Give a preset name (cem_swap_list_presets) or a swap with sources and a target.";
            return null;
        }

        /// <summary>
        /// The map the window would run: its ParameterMapViewModel laid out with the preset's links, the
        /// old side read off the first candidate and the new side off a probe placed beside it and rolled
        /// back (SwapSetupViewModel.RefreshMap).
        /// </summary>
        public static List<ParameterLink> Map(XDocument xdoc, SwapCandidates candidates, List<TypeEntry> sources,
            XElementType target, IEnumerable<ParameterLink> chosen)
        {
            XElement sample = candidates.Elements.FirstOrDefault()
                              ?? sources.Select(entry => TypeCatalog.Resolve(xdoc, entry)).OfType<XElementType>()
                                  .SelectMany(type => type.GetInstances()).FirstOrDefault();
            List<ParameterInfo> fromOld = sample == null ? new List<ParameterInfo>() : ParameterCatalog.Of(sample);
            List<ParameterInfo> toNew = sample == null ? new List<ParameterInfo>() : ParameterCatalog.ProbeTarget(xdoc, target, sample);

            var map = new ParameterMapViewModel();
            map.Load(fromOld, toNew, chosen);
            return map.ToLinks();
        }

        public static string Describe(ParameterLink link)
        {
            string from = link.IsExpression ? $"= {link.Expression}"
                : link.IsConstant ? $"= \"{link.Constant}\""
                : $"{link.Source} ({link.SourceScope})";
            return $"{link.Target} ({link.TargetScope}) ← {from}" + (link.IsAuto ? " (same name)" : "");
        }

        public static SwapPresetInfo Describe(SwapPreset preset) => new SwapPresetInfo
        {
            Name = preset.Name,
            Sources = preset.Sources.Select(source => source.ToString()).ToList(),
            Target = preset.Target?.ToString(),
            Links = preset.Links.Select(Describe).ToList(),
            Offset = $"X {preset.Offset.X} mm, Y {preset.Offset.Y} mm, Z {preset.Offset.Z} mm, rotation {preset.Offset.RotationDegrees}°",
            Placement = preset.Placement.Mode == SwapPlacementMode.OpeningCentres
                ? $"one on each hole of the old element ({preset.Placement.ExpectedCount} expected)"
                : "at the old element's origin",
            Conditions = preset.Conditions
                .Select(c => $"{c.Action} when {c.Parameter} ({c.Scope}) {c.Comparison} \"{c.Value}\"").ToList(),
            Watches = preset.Watches.Select(w => $"{w.Source}: {w.Message}").ToList()
        };

        public static SwapPairInfo Describe(SwapPair pair) => new SwapPairInfo
        {
            OldId = pair.OldId,
            OldLabel = pair.OldLabel,
            NewIds = pair.NewIds.ToList(),
            Refusal = pair.Refusal,
            Warnings = pair.Warnings.ToList(),
            Values = pair.Outcomes.Select(outcome => new SwapValueInfo
            {
                Target = outcome.Target,
                Source = outcome.Source,
                Result = outcome.Result.ToString(),
                Landed = outcome.Landed,
                Detail = outcome.Detail
            }).ToList()
        };

        public static string WarningsText(List<string> warnings) =>
            warnings.Count == 0 ? "" : $" ⚠ {string.Join("; ", warnings)}";
    }

    // ───────────────────────────── cem_rules_apply ─────────────────────────────

    /// <summary>Runs every CEM Rules rule over the whole model, as the CEM Rules button does, and returns its report.</summary>
    public class RulesApplyEventHandler : CemengalEventHandler<RulesApplyRequest, RulesApplyResult>
    {
        protected override AIResult<RulesApplyResult> Run(UIApplication app)
        {
            // The whole document, as the command loads it: ApplyToModel refuses a light one.
            var xdoc = new XDocument(ProjectDocument(app));
            RuleExecutionReport report = new RuleExecutionService(xdoc).ApplyToModel();
            int max = Math.Max(1, Request?.MaxDetails ?? 30);

            var res = new RulesApplyResult
            {
                Errors = report.Errors.Take(max).ToList(),
                FailedPasses = report.FailedPasses.ToList(),
                FailedElements = report.FailedElements.Count,
                NonEditable = report.NonEditable.Count,
                MissingParameters = report.MissingParameters.Take(max)
                    .Select(m => $"{m.FamilyName} : {m.TypeName} lacks {string.Join(", ", m.MissingParameters)}").ToList(),
                Duplicates = report.Duplicates.Take(max)
                    .Select(d => $"{d.ParameterName} on {d.ElementId} ({d.FamilyName} : {d.TypeName}): \"{d.OriginalValue}\" → \"{d.ResolvedValue}\"").ToList()
            };

            string summary = $"CEM Rules ran over the model: {report.Errors.Count} error(s), {report.FailedPasses.Count} failed pass(es), " +
                             $"{report.FailedElements.Count} failed element(s), {report.MissingParameters.Count} type(s) missing parameters, " +
                             $"{report.Duplicates.Count} unique value(s) suffixed _N.";
            string owned = XElement.DescribeOwnedByOthers(report.NonEditable, "CEM_Rules");
            if (!string.IsNullOrEmpty(owned)) summary += " " + owned;

            return report.FailedPasses.Count == 0 ? Done(summary, res) : Refused(summary, res);
        }

        public override string GetName() => "Cemengal: CEM Rules Apply";
    }
}
