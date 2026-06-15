using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Architecture;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Architecture
{
    /// <summary>
    ///     Creates rooms at points (mm) on a level via NewRoom(Level, UV), or auto-places
    ///     rooms in all enclosed regions. Optional names and tagging.
    /// </summary>
    public class CreateRoomEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public CreateRoomRequest Request { get; set; }
        public AIResult<ArchCreateResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new ArchCreateResult();
            try
            {
                Level level = McpResolveUtils.ResolveLevel(doc, Request.Level);
                if (level == null) { Fail("No level available."); return; }

                using (var tx = new Transaction(doc, "Create Room"))
                {
                    tx.Start();
                    if (Request.Points != null && Request.Points.Count > 0)
                    {
                        for (int i = 0; i < Request.Points.Count; i++)
                        {
                            var jp = Request.Points[i];
                            try
                            {
                                var uv = new UV(McpResolveUtils.MmToFt(jp.X), McpResolveUtils.MmToFt(jp.Y));
                                Room room = doc.Create.NewRoom(level, uv);
                                if (room != null)
                                {
                                    if (Request.Names != null && i < Request.Names.Count && !string.IsNullOrWhiteSpace(Request.Names[i]))
                                    {
                                        try { room.Name = Request.Names[i]; } catch { }
                                    }
                                    res.CreatedIds.Add(room.Id.GetValue());
                                    if (Request.Tag) TryTag(room);
                                }
                            }
                            catch (Exception ex) { res.Warnings.Add($"Point failed: {ex.Message}"); }
                        }
                    }
                    else
                    {
                        try
                        {
                            Phase phase = doc.Phases.Size > 0 ? doc.Phases.get_Item(doc.Phases.Size - 1) : null;
                            var created = phase != null
                                ? doc.Create.NewRooms2(level, phase)
                                : doc.Create.NewRooms2(level);
                            {
                                foreach (var id in created)
                                {
                                    res.CreatedIds.Add(id.GetValue());
                                    if (Request.Tag && doc.GetElement(id) is Room rm) TryTag(rm);
                                }
                            }
                        }
                        catch (Exception ex) { res.Warnings.Add($"Auto-place failed: {ex.Message}"); }
                    }
                    tx.Commit();
                }

                res.CreatedCount = res.CreatedIds.Count;
                Result = new AIResult<ArchCreateResult>
                {
                    Success = res.CreatedCount > 0,
                    Message = $"Created {res.CreatedCount} room(s)" +
                              (res.Warnings.Count > 0 ? $". ⚠ {res.Warnings.Count} note(s): {res.Warnings[0]}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<ArchCreateResult> { Success = false, Message = $"Error creating rooms: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }

            void Fail(string msg)
            {
                Result = new AIResult<ArchCreateResult> { Success = false, Message = msg, Response = res };
                _resetEvent.Set();
            }
        }

        private void TryTag(Room room)
        {
            try
            {
                var loc = (room.Location as LocationPoint)?.Point;
                if (loc == null) return;
                var uv = new UV(loc.X, loc.Y);
                doc.Create.NewRoomTag(new LinkElementId(room.Id), uv, doc.ActiveView.Id);
            }
            catch { /* best-effort */ }
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Create Room";
    }
}
