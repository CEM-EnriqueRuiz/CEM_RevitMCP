using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_CSharpExtended.Hosting;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Core
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
            string accFile = null;

            try
            {
                var results = new List<SharedParameterResultInfo>();
                var warnings = new List<string>();
                var createdDefinitions = new List<string>();

                DefinitionFile defFile = EnsureSharedParameterFile(warnings, out accFile);
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
                            if (extDef != null) createdDefinitions.Add(info.Name);
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

                if (accFile != null)
                    warnings.Add($"The shared parameter file is on ACC ({accFile}), where an MCP call never writes: this call " +
                                 $"used a local copy ({app.SharedParametersFilename}) and Revit points at the ACC file again. " +
                                 (createdDefinitions.Count > 0
                                     ? $"New definitions ({string.Join(", ", createdDefinitions)}) are in the copy and the project, " +
                                       "not in the team file; add them there from Revit's Shared Parameters dialog if the team needs them."
                                     : "No definition was added; existing ones kept their GUIDs."));

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
                // The team file on ACC stays the configured one; only this call used the copy.
                if (accFile != null)
                {
                    try { app.SharedParametersFilename = accFile; } catch { /* Revit keeps the copy until the user resets it */ }
                }
                _resetEvent.Set();
            }
        }

        /// <summary>
        /// Opens the configured shared parameter file, creating a temporary one if none exists. A configured file on ACC
        /// (shared team data, never written from an MCP call, root decision 0031) is copied to the pruned SharedParameters
        /// folder and the copy is opened: <paramref name="accFile"/> is then the original, which the caller restores.
        /// </summary>
        private DefinitionFile EnsureSharedParameterFile(List<string> warnings, out string accFile)
        {
            accFile = null;
            string current = app.SharedParametersFilename;
            if (!string.IsNullOrWhiteSpace(current) && File.Exists(current) && McpLocalFiles.IsUnderAcc(current))
            {
                string copy = CemTempFolder.NewArtifactFile(McpLocalFiles.App, "SharedParameters", "acc_copy", ".txt");
                File.Copy(current, copy);
                accFile = current;
                app.SharedParametersFilename = copy;
            }
            else if (string.IsNullOrWhiteSpace(current) || !File.Exists(current))
            {
                // One fixed file in the pruned SharedParameters folder (root decision 0031). Revit keeps pointing at it
                // (and remembers it in Revit.ini), so later calls reuse it; once pruned, the next call makes a new one.
                string tempPath = Path.Combine(CemTempFolder.ArtifactFolder(McpLocalFiles.App, "SharedParameters"),
                    "RevitMCP_SharedParameters.txt");
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
