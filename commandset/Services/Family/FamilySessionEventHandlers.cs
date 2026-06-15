using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
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

        /// <summary>The single open family doc, or the only one matching — for when no title is given.</summary>
        public static Document SingleOpenFamily(Application app)
        {
            Document found = null; int n = 0;
            foreach (Document d in app.Documents)
                if (d.IsFamilyDocument) { found = d; n++; }
            return n == 1 ? found : null;
        }

        /// <summary>A non-family (project) document to load the edited family back into.</summary>
        public static Document FindProjectDoc(Application app)
        {
            foreach (Document d in app.Documents)
                if (!d.IsFamilyDocument) return d;
            return null;
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
                    // EditFamily on a family already loaded in the active project.
                    var fam = new FilteredElementCollector(doc)
                        .OfClass(typeof(DBFamily)).Cast<DBFamily>()
                        .FirstOrDefault(f => f.Name.Equals(Request.FamilyName.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (fam == null) throw new ArgumentException($"Family '{Request.FamilyName}' not found in the active project.");
                    if (!fam.IsEditable) throw new InvalidOperationException($"Family '{fam.Name}' is not editable (e.g. a system family).");
                    famDoc = doc.EditFamily(fam);
                    res.Opened = "editfamily";
                }
                else if (!string.IsNullOrWhiteSpace(Request.FamilyPath))
                {
                    if (!System.IO.File.Exists(Request.FamilyPath))
                        throw new System.IO.FileNotFoundException($"File not found: {Request.FamilyPath}");
                    // Reuse if already open, else open from disk.
                    famDoc = FamilySessionHelpers.FindOpenFamily(app, System.IO.Path.GetFileNameWithoutExtension(Request.FamilyPath))
                             ?? app.OpenDocumentFile(Request.FamilyPath);
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

                res.FamilyTitle = famDoc.Title;
                res.IsFamilyDocument = famDoc.IsFamilyDocument;

                Result = new AIResult<FamilySessionResult>
                {
                    Success = true,
                    Message = $"Opened family '{res.FamilyTitle}' for editing ({res.Opened}). " +
                              "Subsequent family_* editor tools act on the active family document; call family_save_session when done.",
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

                // 1) Save (in place if it has a path, else SaveAs to %TEMP%).
                if (Request.Save)
                {
                    if (!string.IsNullOrEmpty(savedPath))
                    {
                        famDoc.Save();
                    }
                    else
                    {
                        string safe = string.Concat(famDoc.Title.Split(System.IO.Path.GetInvalidFileNameChars()));
                        savedPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), safe + ".rfa");
                        famDoc.SaveAs(savedPath, new SaveAsOptions { OverwriteExistingFile = true });
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

                // 3) Close the family doc if requested (never the project doc).
                if (Request.Close)
                {
                    if (!famDoc.IsFamilyDocument)
                        res.Warnings.Add("Refusing to close a non-family document.");
                    else { famDoc.Close(false); res.Closed = true; }
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
