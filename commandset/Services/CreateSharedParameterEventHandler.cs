using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    /// <summary>
    /// Event handler for creating shared parameters and binding them to categories.
    /// If no shared parameter file is configured, a temporary one is created so the
    /// command works on any machine without manual setup.
    /// </summary>
    public class CreateSharedParameterEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;
        private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<SharedParameterCreationInfo> CreatedInfo { get; private set; }
        public AIResult<List<SharedParameterResultInfo>> Result { get; private set; }

        public void SetParameters(List<SharedParameterCreationInfo> data)
        {
            CreatedInfo = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                var results = new List<SharedParameterResultInfo>();
                var warnings = new List<string>();

                DefinitionFile defFile = EnsureSharedParameterFile(warnings);
                if (defFile == null)
                    throw new Exception("Could not open or create a shared parameter file");

                foreach (var info in CreatedInfo)
                {
                    if (string.IsNullOrWhiteSpace(info.Name))
                    {
                        warnings.Add("Skipped a parameter with empty name");
                        continue;
                    }

                    try
                    {
                        // 1. Definition group in the shared parameter file
                        string groupName = string.IsNullOrWhiteSpace(info.DefinitionGroup) ? "MCP" : info.DefinitionGroup;
                        DefinitionGroup defGroup = defFile.Groups.get_Item(groupName)
                                                   ?? defFile.Groups.Create(groupName);

                        // 2. Reuse or create the external definition
                        ExternalDefinition extDef = defGroup.Definitions.get_Item(info.Name) as ExternalDefinition;
                        bool definitionExisted = extDef != null;

                        if (extDef == null)
                        {
#if REVIT2022_OR_GREATER
                            var options = new ExternalDefinitionCreationOptions(
                                info.Name, McpParameterUtils.GetSpecTypeId(info.DataType));
#else
                            var options = new ExternalDefinitionCreationOptions(
                                info.Name, McpParameterUtils.GetParameterType(info.DataType));
#endif
                            if (!string.IsNullOrWhiteSpace(info.Guid) &&
                                Guid.TryParse(info.Guid, out Guid explicitGuid))
                            {
                                options.GUID = explicitGuid;
                            }

                            extDef = defGroup.Definitions.Create(options) as ExternalDefinition;
                        }

                        if (extDef == null)
                        {
                            warnings.Add($"Could not create definition '{info.Name}'");
                            continue;
                        }

                        // 3. Build category set
                        CategorySet catSet = app.Create.NewCategorySet();
                        var boundCategories = new List<string>();

                        foreach (string catName in info.Categories ?? new List<string>())
                        {
                            if (!Enum.TryParse(catName, true, out BuiltInCategory bic))
                            {
                                warnings.Add($"Unknown category '{catName}' (use BuiltInCategory names like OST_Walls)");
                                continue;
                            }

                            Category cat = doc.Settings.Categories.get_Item(bic);
                            if (cat == null || !cat.AllowsBoundParameters)
                            {
                                warnings.Add($"Category '{catName}' does not allow bound parameters");
                                continue;
                            }

                            catSet.Insert(cat);
                            boundCategories.Add(catName);
                        }

                        if (catSet.IsEmpty)
                        {
                            warnings.Add($"Parameter '{info.Name}': no valid categories provided, definition created in shared file only");
                            results.Add(new SharedParameterResultInfo
                            {
                                Name = info.Name,
                                Guid = extDef.GUID.ToString(),
                                DataType = info.DataType,
                                IsInstance = info.IsInstance,
                                AlreadyExisted = definitionExisted
                            });
                            continue;
                        }

                        // 4. Bind to the project
                        using (Transaction tx = new Transaction(doc, $"Bind Shared Parameter '{info.Name}'"))
                        {
                            tx.Start();

                            Binding binding = info.IsInstance
                                ? (Binding)app.Create.NewInstanceBinding(catSet)
                                : (Binding)app.Create.NewTypeBinding(catSet);

                            bool inserted;
#if REVIT2024_OR_GREATER
                            ForgeTypeId uiGroup = GetGroupTypeId(info.ParameterGroup);
                            inserted = doc.ParameterBindings.Insert(extDef, binding, uiGroup);
                            if (!inserted)
                                inserted = doc.ParameterBindings.ReInsert(extDef, binding, uiGroup);
#else
                            BuiltInParameterGroup uiGroup = GetBuiltInParameterGroup(info.ParameterGroup);
                            inserted = doc.ParameterBindings.Insert(extDef, binding, uiGroup);
                            if (!inserted)
                                inserted = doc.ParameterBindings.ReInsert(extDef, binding, uiGroup);
#endif
                            if (!inserted)
                                warnings.Add($"Parameter '{info.Name}' could not be (re)bound; it may already exist with a different binding");

                            // 5. Vary between groups (instance parameters only)
                            if (info.IsInstance && info.VaryBetweenGroups)
                            {
                                SharedParameterElement spe = SharedParameterElement.Lookup(doc, extDef.GUID);
                                InternalDefinition internalDef = spe?.GetDefinition();
                                if (internalDef != null)
                                {
                                    try { internalDef.SetAllowVaryBetweenGroups(doc, true); }
                                    catch (Exception exVary)
                                    {
                                        warnings.Add($"'{info.Name}': could not set vary-between-groups ({exVary.Message})");
                                    }
                                }
                            }

                            tx.Commit();
                        }

                        results.Add(new SharedParameterResultInfo
                        {
                            Name = info.Name,
                            Guid = extDef.GUID.ToString(),
                            DataType = info.DataType,
                            IsInstance = info.IsInstance,
                            BoundCategories = boundCategories,
                            AlreadyExisted = definitionExisted
                        });
                    }
                    catch (Exception exItem)
                    {
                        warnings.Add($"Error processing '{info.Name}': {exItem.Message}");
                    }
                }

                string message = $"Successfully processed {results.Count} shared parameter(s).";
                if (warnings.Count > 0)
                    message += "\n\n⚠ Warnings:\n  • " + string.Join("\n  • ", warnings);

                Result = new AIResult<List<SharedParameterResultInfo>>
                {
                    Success = true,
                    Message = message,
                    Response = results
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<SharedParameterResultInfo>>
                {
                    Success = false,
                    Message = $"Error creating shared parameters: {ex.Message}"
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        /// <summary>
        /// Opens the configured shared parameter file, creating a temporary one if none exists
        /// </summary>
        private DefinitionFile EnsureSharedParameterFile(List<string> warnings)
        {
            string current = app.SharedParametersFilename;
            if (string.IsNullOrWhiteSpace(current) || !File.Exists(current))
            {
                string tempPath = Path.Combine(Path.GetTempPath(), "RevitMCP_SharedParameters.txt");
                if (!File.Exists(tempPath))
                    File.WriteAllText(tempPath, string.Empty);

                app.SharedParametersFilename = tempPath;
                warnings.Add($"No shared parameter file was configured. Using temporary file: {tempPath}");
            }

            return app.OpenSharedParameterFile();
        }

#if REVIT2024_OR_GREATER
        private static ForgeTypeId GetGroupTypeId(string groupName)
        {
            switch ((groupName ?? "Data").Replace(" ", "").Trim().ToLowerInvariant())
            {
                case "text": return GroupTypeId.Text;
                case "identitydata": return GroupTypeId.IdentityData;
                case "dimensions":
                case "geometry": return GroupTypeId.Geometry;
                case "construction": return GroupTypeId.Construction;
                case "general": return GroupTypeId.General;
                case "materials": return GroupTypeId.Materials;
                case "data":
                default: return GroupTypeId.Data;
            }
        }
#else
        private static BuiltInParameterGroup GetBuiltInParameterGroup(string groupName)
        {
            switch ((groupName ?? "Data").Replace(" ", "").Trim().ToLowerInvariant())
            {
                case "text": return BuiltInParameterGroup.PG_TEXT;
                case "identitydata": return BuiltInParameterGroup.PG_IDENTITY_DATA;
                case "dimensions":
                case "geometry": return BuiltInParameterGroup.PG_GEOMETRY;
                case "construction": return BuiltInParameterGroup.PG_CONSTRUCTION;
                case "general": return BuiltInParameterGroup.PG_GENERAL;
                case "materials": return BuiltInParameterGroup.PG_MATERIALS;
                case "data":
                default: return BuiltInParameterGroup.PG_DATA;
            }
        }
#endif

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName()
        {
            return "Create Shared Parameter";
        }
    }
}
