using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_CSharpExtended.IO;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Models.Family;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;
using DBFamily = Autodesk.Revit.DB.Family;

namespace CEM_IAModeler_CommandSet.Services.Family
{
    internal static class FamilySessionHelpers
    {
        /// <summary>Find an open family document by Title; null if not found.</summary>
        public static Document FindOpenFamily(Application app, string title)
        {
            foreach (Document d in app.Documents)
                if (d.IsFamilyDocument && string.Equals(d.Title, title, StringComparison.OrdinalIgnoreCase))
                    return d;
            return null;
        }

        /// <summary>
        /// The open family document of that file (full path, case-insensitive); null if none. A different family that
        /// only shares the file name is not it.
        /// </summary>
        public static Document FindOpenFamilyByPath(Application app, string path)
        {
            string full = System.IO.Path.GetFullPath(path);
            foreach (Document d in app.Documents)
                if (d.IsFamilyDocument && !string.IsNullOrEmpty(d.PathName) &&
                    string.Equals(System.IO.Path.GetFullPath(d.PathName), full, StringComparison.OrdinalIgnoreCase))
                    return d;
            return null;
        }

        /// <summary>The single open family doc, or the only one matching — for when no title is given.</summary>
        public static Document SingleOpenFamily(Application app)
        {
            Document found = null; int n = 0;
            foreach (Document d in app.Documents)
                if (d.IsFamilyDocument) { found = d; n++; }
            return n == 1 ? found : null;
        }

        /// <summary>An open project document (not a family, not a link) to load the edited family back into.</summary>
        public static Document FindProjectDoc(Application app)
        {
            foreach (Document d in app.Documents)
                if (!d.IsFamilyDocument && !d.IsLinked) return d;
            return null;
        }

        /// <summary>
        /// The document whose loaded family family_open_session edits by name. Not the active document: after an open,
        /// that is the family just opened, and its nested families would be searched instead. An explicit
        /// <paramref name="hostTitle"/> names any open document (a family, to edit one of its nested families); without
        /// it, the active document if it is a project (with two projects open, the first in Application.Documents may
        /// be the wrong one: QA-7), else the open project, else the active document.
        /// </summary>
        public static Document ResolveHost(UIApplication uiApp, string hostTitle)
        {
            var app = uiApp.Application;
            if (string.IsNullOrWhiteSpace(hostTitle))
            {
                var active = uiApp.ActiveUIDocument?.Document;
                if (active != null && !active.IsFamilyDocument) return active;
                return FindProjectDoc(app) ?? active;
            }
            foreach (Document d in app.Documents)
                if (!d.IsLinked && (string.Equals(d.Title, hostTitle.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(d.Title, hostTitle.Trim() + ".rfa", StringComparison.OrdinalIgnoreCase)))
                    return d;
            var open = app.Documents.Cast<Document>().Where(d => !d.IsLinked).Select(d => d.Title);
            throw new ArgumentException($"Host document '{hostTitle}' is not open. Open documents: {string.Join(", ", open)}.");
        }

        /// <summary>
        /// A new path for a temporary copy of a family: <c>Families\&lt;name&gt;_&lt;stamp&gt;\&lt;name&gt;.rfa</c>. The folder
        /// is new for each copy, so no copy is ever overwritten, and the file keeps the family's name, which a project
        /// takes from it on load. Empty copy folders left by the 7-day pruning are removed here, best effort.
        /// </summary>
        public static string NewFamilyCopyPath(string title)
        {
            string name = FileName(title);
            if (string.IsNullOrWhiteSpace(name)) name = "family";
            RemoveEmptyCopyFolders();
            string reserved = CemTempFolder.NewArtifactFile(McpLocalFiles.App, "Families", name, ".rfa");
            string folder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(reserved), System.IO.Path.GetFileNameWithoutExtension(reserved));
            string file = System.IO.Path.Combine(folder, name + ".rfa");
            // SEC-8: the folder's name can be predicted, and CreateDirectory succeeds silently on a planted junction, so it
            // is checked before; and after, with the file, in case one was swapped in meanwhile.
            McpLocalFiles.RefuseReparsePoint(folder);
            System.IO.Directory.CreateDirectory(folder);
            McpLocalFiles.RefuseReparsePoint(folder, file);
            return file;
        }

        private static void RemoveEmptyCopyFolders()
        {
            try
            {
                foreach (var dir in System.IO.Directory.GetDirectories(CemTempFolder.ArtifactFolder(McpLocalFiles.App, "Families")))
                    if (!McpLocalFiles.IsReparsePoint(dir) && !System.IO.Directory.EnumerateFileSystemEntries(dir).Any())
                        try { System.IO.Directory.Delete(dir); } catch { /* in use or gone: the next copy retries */ }
            }
            catch { /* never fails a family call */ }
        }

        /// <summary>
        /// Make <paramref name="famDoc"/> the active document: the family_* editor tools act on the active document only.
        /// Revit activates a document only by its file (<c>OpenAndActivateDocument</c>), so a family with no file (an
        /// EditFamily of a family loaded in a project) is first saved to the pruned Families temp folder (root decision
        /// 0031), where family_save_session puts a pathless family anyway. Returns the activated document.
        /// </summary>
        public static Document Activate(UIApplication uiApp, Document famDoc, List<string> warnings)
        {
            if (uiApp.ActiveUIDocument?.Document?.Equals(famDoc) == true) return famDoc;
            if (string.IsNullOrEmpty(famDoc.PathName))
            {
                string path;
                try { path = NewFamilyCopyPath(famDoc.Title); }
                catch (CemTempFolderUnsafeException ex)
                {
                    throw new InvalidOperationException(
                        $"The family '{famDoc.Title}' was opened but not activated: its temporary copy was refused, because " +
                        $"{ex.Path} is a junction or link (SEC-8). Nothing was saved. Remove that link, then open the family again. " +
                        "The unsaved family stays open in Revit.", ex);
                }
                famDoc.SaveAs(path, new SaveAsOptions { OverwriteExistingFile = false });
                warnings.Add($"The family had no file, and Revit activates a document only from one: it was saved to {path} " +
                             "(a local temp copy) and activated there. family_save_session saves to that copy and loads it into the project.");
            }
            return uiApp.OpenAndActivateDocument(famDoc.PathName).Document;
        }

        /// <summary>A family's file name from its title, without ".rfa" and invalid characters.</summary>
        public static string FileName(string title)
        {
            string name = title.EndsWith(".rfa", StringComparison.OrdinalIgnoreCase) ? title.Substring(0, title.Length - 4) : title;
            return string.Concat(name.Split(System.IO.Path.GetInvalidFileNameChars()));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  family_open_session — open a family for editing (EditFamily | OpenDocumentFile | active)
    // ─────────────────────────────────────────────────────────────────────────
    public class FamilyOpenSessionEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public FamilyOpenSessionRequest Request { get; set; }
        public AIResult<FamilySessionResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new FamilySessionResult();
            try
            {
                var app = uiApp.Application;
                Document famDoc;

                if (!string.IsNullOrWhiteSpace(Request.FamilyName))
                {
                    // EditFamily on a family loaded in the host: the open project by default, not the active document,
                    // which after an earlier open is a family whose nested families would be searched instead.
                    var host = FamilySessionHelpers.ResolveHost(uiApp, Request.HostDocument);
                    res.HostTitle = host.Title;
                    var fam = new FilteredElementCollector(host)
                        .OfClass(typeof(DBFamily)).Cast<DBFamily>()
                        .FirstOrDefault(f => f.Name.Equals(Request.FamilyName.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (fam == null) throw new ArgumentException($"Family '{Request.FamilyName}' not found in '{host.Title}'. To edit a family nested in another open document, name it with hostDocument.");
                    if (!fam.IsEditable) throw new InvalidOperationException($"Family '{fam.Name}' is not editable (e.g. a system family).");
                    famDoc = host.EditFamily(fam);
                    res.Opened = "editfamily";
                }
                else if (!string.IsNullOrWhiteSpace(Request.FamilyPath))
                {
                    if (!System.IO.File.Exists(Request.FamilyPath))
                        throw new System.IO.FileNotFoundException($"File not found: {Request.FamilyPath}");
                    // Reuse it if that same file is already open, else open it from disk, activated.
                    famDoc = FamilySessionHelpers.FindOpenFamilyByPath(app, Request.FamilyPath)
                             ?? uiApp.OpenAndActivateDocument(Request.FamilyPath).Document;
                    res.Opened = "path";
                }
                else
                {
                    // Use the active document if it is already a family document.
                    if (!doc.IsFamilyDocument)
                        throw new InvalidOperationException("No familyName/familyPath given and the active document is not a family.");
                    famDoc = doc;
                    res.Opened = "active";
                }

                // The family_* editor tools act on the active document only, so the family must be it.
                famDoc = FamilySessionHelpers.Activate(uiApp, famDoc, res.Warnings);

                res.FamilyTitle = famDoc.Title;
                res.IsFamilyDocument = famDoc.IsFamilyDocument;

                Result = new AIResult<FamilySessionResult>
                {
                    Success = true,
                    Message = $"Opened family '{res.FamilyTitle}' for editing ({res.Opened}" +
                              (res.HostTitle != null ? $" in '{res.HostTitle}'" : "") + "); it is now the active document. " +
                              "The family_* editor tools act on it; call family_save_session when done." +
                              (res.Warnings.Count > 0 ? $" ⚠ {string.Join("; ", res.Warnings)}" : ""),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<FamilySessionResult> { Success = false, Message = $"Error opening family: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Family Open Session";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  family_save_session — save / load-back / close
    // ─────────────────────────────────────────────────────────────────────────
    public class FamilySaveSessionEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public FamilySaveSessionRequest Request { get; set; }
        public AIResult<FamilySessionResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new FamilySessionResult();
            try
            {
                var app = uiApp.Application;

                // Resolve the family doc to act on.
                Document famDoc = !string.IsNullOrWhiteSpace(Request.FamilyTitle)
                    ? FamilySessionHelpers.FindOpenFamily(app, Request.FamilyTitle)
                    : FamilySessionHelpers.SingleOpenFamily(app)
                      ?? (uiApp.ActiveUIDocument.Document.IsFamilyDocument ? uiApp.ActiveUIDocument.Document : null);

                if (famDoc == null)
                    throw new InvalidOperationException("No open family document found to save. Open one with family_open_session first.");
                res.FamilyTitle = famDoc.Title;
                res.IsFamilyDocument = true;

                string savedPath = famDoc.PathName;

                // 1) Save: in place on the user's own path; otherwise SaveAs to the pruned Families temp folder
                //    (root decision 0031). That covers a family with no path and one whose file is on ACC: an MCP call
                //    never writes under ACC, where a library .rfa is shared team data. The family's name is its file
                //    name, so the copy keeps it, in a new folder per copy (never overwritten); SaveAs moves the document
                //    onto the copy, which later saves and the load below use.
                if (Request.Save)
                {
                    if (!string.IsNullOrEmpty(savedPath) && !McpLocalFiles.IsUnderAcc(savedPath))
                    {
                        famDoc.Save();
                    }
                    else
                    {
                        string accOriginal = savedPath;
                        string name = !string.IsNullOrEmpty(accOriginal)
                            ? System.IO.Path.GetFileNameWithoutExtension(accOriginal)
                            : famDoc.Title;
                        savedPath = FamilySessionHelpers.NewFamilyCopyPath(name);
                        famDoc.SaveAs(savedPath, new SaveAsOptions { OverwriteExistingFile = false });
                        if (!string.IsNullOrEmpty(accOriginal))
                            res.Warnings.Add($"The family's file is on ACC ({accOriginal}), where an MCP call never writes: " +
                                             $"the ACC original is untouched, and the family was saved as a copy to {savedPath}, " +
                                             "which the open document now uses. To update the shared library, save it there from Revit yourself.");
                    }
                    res.Saved = true;
                    res.SavedPath = savedPath;
                }

                // 2) Load back into the project (overwriting), reusing OverwriteFamilyLoadOptions.
                if (Request.LoadIntoProject)
                {
                    var projectDoc = FamilySessionHelpers.FindProjectDoc(app);
                    if (projectDoc == null)
                        res.Warnings.Add("No open project document to load the family into; skipped load.");
                    else if (string.IsNullOrEmpty(savedPath) || !System.IO.File.Exists(savedPath))
                        res.Warnings.Add("Family has no saved path; cannot load into project. Set save:true.");
                    else
                    {
                        DBFamily loaded = null;
                        bool ok;
                        using (var tx = new Transaction(projectDoc, "Load Family From Session"))
                        {
                            tx.Start();
                            ok = projectDoc.LoadFamily(savedPath, new OverwriteFamilyLoadOptions(true), out loaded);
                            tx.Commit();
                        }
                        if (ok && loaded != null)
                        {
                            res.LoadedIds.Add(loaded.Id.GetValue());
                            foreach (var id in loaded.GetFamilySymbolIds()) res.LoadedIds.Add(id.GetValue());
                            res.LoadedIntoProject = true;
                        }
                        else res.Warnings.Add("LoadFamily returned false (family may be unchanged).");
                    }
                }

                // 3) Close the family doc if requested (never the project doc). Revit cannot close the active document, which
                //    family_open_session made the family, so the project is re-activated first; it needs a file for that.
                if (Request.Close)
                {
                    if (!famDoc.IsFamilyDocument)
                        res.Warnings.Add("Refusing to close a non-family document.");
                    else
                    {
                        if (uiApp.ActiveUIDocument?.Document?.Equals(famDoc) == true)
                        {
                            var projectDoc = FamilySessionHelpers.FindProjectDoc(app);
                            if (projectDoc != null && !string.IsNullOrEmpty(projectDoc.PathName))
                                uiApp.OpenAndActivateDocument(projectDoc.PathName);
                        }
                        if (uiApp.ActiveUIDocument?.Document?.Equals(famDoc) == true)
                            res.Warnings.Add("The family is the active document and no saved project is open to switch to, so it was " +
                                             "left open (Revit cannot close the active document); close it in Revit.");
                        else { famDoc.Close(false); res.Closed = true; }
                    }
                }

                Result = new AIResult<FamilySessionResult>
                {
                    Success = true,
                    Message = $"Family '{res.FamilyTitle}': " +
                              (res.Saved ? $"saved to {res.SavedPath}. " : "not saved. ") +
                              (res.LoadedIntoProject ? $"loaded into project ({res.LoadedIds.Count} id(s)). " : "") +
                              (res.Closed ? "closed. " : "") +
                              (res.Warnings.Count > 0 ? $"⚠ {string.Join("; ", res.Warnings)}" : ""),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<FamilySessionResult> { Success = false, Message = $"Error in family_save_session: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int t = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(t); }
        public string GetName() => "Family Save Session";
    }
}
