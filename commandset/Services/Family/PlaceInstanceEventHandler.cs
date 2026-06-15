using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Models.Family;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Family
{
    /// <summary>
    ///     Places instances of a loaded family type at points (mm). Uses the level-based
    ///     overload, or a host-based overload when a hostId is supplied.
    /// </summary>
    public class PlaceInstanceEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public PlaceInstanceRequest Request { get; set; }
        public AIResult<FamilyOpResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new FamilyOpResult();
            try
            {
                var symbol = McpResolveUtils.ResolveType(doc, Request.TypeName) as FamilySymbol;
                if (symbol == null)
                {
                    Result = new AIResult<FamilyOpResult> { Success = false, Message = $"Family type '{Request.TypeName}' not found." };
                    _resetEvent.Set();
                    return;
                }

                Level level = McpResolveUtils.ResolveLevel(doc, Request.Level);
                Element host = Request.HostId != 0 ? McpResolveUtils.GetElement(doc, Request.HostId) : null;
                StructuralType st = McpFamilyUtils.ParseStructuralType(Request.StructuralType);
                double rot = Request.Rotation * Math.PI / 180.0;

                using (var tx = new Transaction(doc, "Place Family Instance"))
                {
                    tx.Start();
                    if (!symbol.IsActive) { symbol.Activate(); doc.Regenerate(); }

                    foreach (var jp in Request.Points)
                    {
                        try
                        {
                            XYZ pt = JZPoint.ToXYZ(jp);
                            FamilyInstance fi;
                            if (host != null)
                                fi = doc.Create.NewFamilyInstance(pt, symbol, host, st);
                            else if (level != null)
                                fi = doc.Create.NewFamilyInstance(pt, symbol, level, st);
                            else
                                fi = doc.Create.NewFamilyInstance(pt, symbol, st);

                            if (fi != null)
                            {
                                if (Math.Abs(rot) > 1e-9)
                                {
                                    var axis = Line.CreateBound(pt, pt + XYZ.BasisZ);
                                    ElementTransformUtils.RotateElement(doc, fi.Id, axis, rot);
                                }
                                res.Ids.Add(fi.Id.GetValue());
                            }
                        }
                        catch (Exception ex) { res.Warnings.Add($"Point failed: {ex.Message}"); }
                    }
                    tx.Commit();
                }

                res.Count = res.Ids.Count;
                Result = new AIResult<FamilyOpResult>
                {
                    Success = res.Count > 0,
                    Message = $"Placed {res.Count} instance(s) of '{symbol.FamilyName} : {symbol.Name}'" +
                              (res.Warnings.Count > 0 ? $". ⚠ {res.Warnings.Count} note(s): {res.Warnings[0]}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<FamilyOpResult> { Success = false, Message = $"Error placing instances: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Place Family Instance";
    }
}
