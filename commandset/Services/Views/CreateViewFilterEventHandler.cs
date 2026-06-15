using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Views
{
    /// <summary>
    /// Event handler for creating a ParameterFilterElement with generic rules
    /// and applying it to a view with graphic overrides.
    /// </summary>
    public class CreateViewFilterEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public ViewFilterCreationInfo CreationInfo { get; private set; }
        public AIResult<object> Result { get; private set; }

        public void SetParameters(ViewFilterCreationInfo data)
        {
            CreationInfo = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                var warnings = new List<string>();

                // 1. Resolve categories
                var categoryIds = new List<ElementId>();
                foreach (string catName in CreationInfo.Categories)
                {
                    if (Enum.TryParse(catName, true, out BuiltInCategory bic))
                        categoryIds.Add(new ElementId(bic));
                    else
                        warnings.Add($"Unknown category '{catName}'");
                }
                if (categoryIds.Count == 0)
                    throw new Exception("No valid categories resolved. Use BuiltInCategory names like OST_Walls");

                // 2. Build rules
                var rules = new List<FilterRule>();
                foreach (var ruleInfo in CreationInfo.Rules ?? new List<FilterRuleInfo>())
                {
                    FilterRule rule = BuildRule(ruleInfo, warnings);
                    if (rule != null)
                        rules.Add(rule);
                }

                long filterId;
                string filterName = CreationInfo.Name;

                using (Transaction tx = new Transaction(doc, $"Create View Filter '{filterName}'"))
                {
                    tx.Start();

                    // 3. Reuse or create the filter element
                    ParameterFilterElement filterElement = new FilteredElementCollector(doc)
                        .OfClass(typeof(ParameterFilterElement))
                        .Cast<ParameterFilterElement>()
                        .FirstOrDefault(f => f.Name.Equals(filterName, StringComparison.OrdinalIgnoreCase));

                    ElementFilter elementFilter = rules.Count > 0
                        ? new ElementParameterFilter(rules)
                        : null;

                    if (filterElement == null)
                    {
                        filterElement = elementFilter != null
                            ? ParameterFilterElement.Create(doc, filterName, categoryIds, elementFilter)
                            : ParameterFilterElement.Create(doc, filterName, categoryIds);
                    }
                    else
                    {
                        // Update existing filter in place
                        filterElement.SetCategories(categoryIds);
                        if (elementFilter != null)
                            filterElement.SetElementFilter(elementFilter);
                        warnings.Add($"Filter '{filterName}' already existed and was updated");
                    }

                    filterId = filterElement.Id.GetValue();

                    // 4. Apply to view
                    if (CreationInfo.ApplyToView)
                    {
                        View view = CreationInfo.ViewId > 0
                            ? doc.GetElement(new ElementId(CreationInfo.ViewId)) as View
                            : doc.ActiveView;

                        if (view == null)
                        {
                            warnings.Add("Target view not found; filter was created but not applied");
                        }
                        else if (view.IsTemplate || !view.AreGraphicsOverridesAllowed())
                        {
                            warnings.Add($"View '{view.Name}' does not allow graphic overrides; filter not applied");
                        }
                        else
                        {
                            if (!view.GetFilters().Contains(filterElement.Id))
                                view.AddFilter(filterElement.Id);

                            view.SetFilterOverrides(filterElement.Id, BuildOverrides(CreationInfo.Overrides));
                            view.SetFilterVisibility(filterElement.Id, CreationInfo.Visible);
                        }
                    }

                    tx.Commit();
                }

                string message = $"Successfully created/updated filter '{filterName}' with {rules.Count} rule(s).";
                if (warnings.Count > 0)
                    message += "\n\n⚠ Warnings:\n  • " + string.Join("\n  • ", warnings);

                Result = new AIResult<object>
                {
                    Success = true,
                    Message = message,
                    Response = new { filterId, name = filterName, ruleCount = rules.Count }
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<object>
                {
                    Success = false,
                    Message = $"Error creating view filter: {ex.Message}"
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        /// <summary>
        /// Builds a single FilterRule from generic rule info
        /// </summary>
        private FilterRule BuildRule(FilterRuleInfo info, List<string> warnings)
        {
            ElementId paramId = McpParameterUtils.ResolveParameterId(doc, info.BuiltInParameter, info.ParameterName);
            if (paramId == ElementId.InvalidElementId)
            {
                warnings.Add($"Could not resolve parameter '{info.BuiltInParameter}{info.ParameterName}' for rule");
                return null;
            }

            string op = (info.Operator ?? "Equals").Replace(" ", "").Trim().ToLowerInvariant();

            try
            {
                // Rules without value
                if (op == "hasvalue") return ParameterFilterRuleFactory.CreateHasValueParameterRule(paramId);
                if (op == "hasnovalue") return ParameterFilterRuleFactory.CreateHasNoValueParameterRule(paramId);

                // Infer value type
                string valueType = (info.ValueType ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(valueType))
                {
                    if (info.Value is bool) valueType = "integer";
                    else if (info.Value is long || info.Value is int) valueType = "integer";
                    else if (info.Value is double || info.Value is float || info.Value is decimal) valueType = "double";
                    else valueType = "string";
                }

                if (valueType == "string")
                {
                    string s = info.Value?.ToString() ?? string.Empty;
                    switch (op)
                    {
#if REVIT2023_OR_GREATER
                        case "equals": return ParameterFilterRuleFactory.CreateEqualsRule(paramId, s);
                        case "notequals": return ParameterFilterRuleFactory.CreateNotEqualsRule(paramId, s);
                        case "contains": return ParameterFilterRuleFactory.CreateContainsRule(paramId, s);
                        case "notcontains": return ParameterFilterRuleFactory.CreateNotContainsRule(paramId, s);
                        case "beginswith": return ParameterFilterRuleFactory.CreateBeginsWithRule(paramId, s);
                        case "endswith": return ParameterFilterRuleFactory.CreateEndsWithRule(paramId, s);
                        case "greaterthan": return ParameterFilterRuleFactory.CreateGreaterRule(paramId, s);
                        case "lessthan": return ParameterFilterRuleFactory.CreateLessRule(paramId, s);
#else
                        case "equals": return ParameterFilterRuleFactory.CreateEqualsRule(paramId, s, false);
                        case "notequals": return ParameterFilterRuleFactory.CreateNotEqualsRule(paramId, s, false);
                        case "contains": return ParameterFilterRuleFactory.CreateContainsRule(paramId, s, false);
                        case "notcontains": return ParameterFilterRuleFactory.CreateNotContainsRule(paramId, s, false);
                        case "beginswith": return ParameterFilterRuleFactory.CreateBeginsWithRule(paramId, s, false);
                        case "endswith": return ParameterFilterRuleFactory.CreateEndsWithRule(paramId, s, false);
                        case "greaterthan": return ParameterFilterRuleFactory.CreateGreaterRule(paramId, s, false);
                        case "lessthan": return ParameterFilterRuleFactory.CreateLessRule(paramId, s, false);
#endif
                        default:
                            warnings.Add($"Operator '{info.Operator}' not supported for strings");
                            return null;
                    }
                }

                if (valueType == "integer" || valueType == "elementid")
                {
                    int i = info.Value is bool bv
                        ? (bv ? 1 : 0)
                        : Convert.ToInt32(info.Value, System.Globalization.CultureInfo.InvariantCulture);

                    if (valueType == "elementid")
                    {
                        var eid = new ElementId(i);
                        switch (op)
                        {
                            case "equals": return ParameterFilterRuleFactory.CreateEqualsRule(paramId, eid);
                            case "notequals": return ParameterFilterRuleFactory.CreateNotEqualsRule(paramId, eid);
                            default:
                                warnings.Add($"Operator '{info.Operator}' not supported for element ids");
                                return null;
                        }
                    }

                    switch (op)
                    {
                        case "equals": return ParameterFilterRuleFactory.CreateEqualsRule(paramId, i);
                        case "notequals": return ParameterFilterRuleFactory.CreateNotEqualsRule(paramId, i);
                        case "greaterthan": return ParameterFilterRuleFactory.CreateGreaterRule(paramId, i);
                        case "greaterorequal": return ParameterFilterRuleFactory.CreateGreaterOrEqualRule(paramId, i);
                        case "lessthan": return ParameterFilterRuleFactory.CreateLessRule(paramId, i);
                        case "lessorequal": return ParameterFilterRuleFactory.CreateLessOrEqualRule(paramId, i);
                        default:
                            warnings.Add($"Operator '{info.Operator}' not supported for integers");
                            return null;
                    }
                }

                // double
                double d = Convert.ToDouble(info.Value, System.Globalization.CultureInfo.InvariantCulture);
                if (info.ConvertMillimeters)
                    d = d / 304.8; // mm -> ft (most numeric model parameters are lengths)
                const double epsilon = 1e-6;

                switch (op)
                {
                    case "equals": return ParameterFilterRuleFactory.CreateEqualsRule(paramId, d, epsilon);
                    case "notequals": return ParameterFilterRuleFactory.CreateNotEqualsRule(paramId, d, epsilon);
                    case "greaterthan": return ParameterFilterRuleFactory.CreateGreaterRule(paramId, d, epsilon);
                    case "greaterorequal": return ParameterFilterRuleFactory.CreateGreaterOrEqualRule(paramId, d, epsilon);
                    case "lessthan": return ParameterFilterRuleFactory.CreateLessRule(paramId, d, epsilon);
                    case "lessorequal": return ParameterFilterRuleFactory.CreateLessOrEqualRule(paramId, d, epsilon);
                    default:
                        warnings.Add($"Operator '{info.Operator}' not supported for numbers");
                        return null;
                }
            }
            catch (Exception ex)
            {
                warnings.Add($"Could not build rule for '{info.ParameterName}{info.BuiltInParameter}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Builds OverrideGraphicSettings from generic override info
        /// </summary>
        private OverrideGraphicSettings BuildOverrides(FilterOverrideInfo info)
        {
            var ogs = new OverrideGraphicSettings();
            if (info == null)
                return ogs;

            if (info.Color != null && info.Color.Length >= 3)
            {
                var color = new Color(
                    (byte)Math.Max(0, Math.Min(255, info.Color[0])),
                    (byte)Math.Max(0, Math.Min(255, info.Color[1])),
                    (byte)Math.Max(0, Math.Min(255, info.Color[2])));

                ogs.SetProjectionLineColor(color);
                ogs.SetCutLineColor(color);
                ogs.SetSurfaceForegroundPatternColor(color);
                ogs.SetCutForegroundPatternColor(color);

                if (info.SolidFill)
                {
                    ElementId solidPatternId = GetSolidFillPatternId();
                    if (solidPatternId != ElementId.InvalidElementId)
                    {
                        ogs.SetSurfaceForegroundPatternId(solidPatternId);
                        ogs.SetCutForegroundPatternId(solidPatternId);
                    }
                }
            }

            if (info.Transparency.HasValue)
                ogs.SetSurfaceTransparency(Math.Max(0, Math.Min(100, info.Transparency.Value)));

            if (info.LineWeight.HasValue)
                ogs.SetProjectionLineWeight(Math.Max(1, Math.Min(16, info.LineWeight.Value)));

            if (info.Halftone)
                ogs.SetHalftone(true);

            return ogs;
        }

        private ElementId GetSolidFillPatternId()
        {
            var pattern = new FilteredElementCollector(doc)
                .OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .FirstOrDefault(p => p.GetFillPattern().IsSolidFill);

            return pattern?.Id ?? ElementId.InvalidElementId;
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName()
        {
            return "Create View Filter";
        }
    }
}
