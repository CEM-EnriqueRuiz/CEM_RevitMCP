using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Models.Mep;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Mep
{
    /// <summary>
    ///     Places a family instance (MEP equipment / fixture / device) at points on a level.
    ///     Generic: works for any loadable family type the AI names.
    /// </summary>
    public class PlaceMepEquipmentEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public MepEquipmentRequest Request { get; set; }
        public AIResult<MepInfoResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new MepInfoResult();
            try
            {
                var symbol = McpResolveUtils.ResolveType(doc, Request.TypeName) as FamilySymbol;
                if (symbol == null)
                {
                    Result = new AIResult<MepInfoResult> { Success = false, Message = $"Family type '{Request.TypeName}' not found." };
                    _resetEvent.Set();
                    return;
                }
                Level level = McpResolveUtils.ResolveLevel(doc, Request.Level);

                using (var tx = new Transaction(doc, "Place MEP Equipment"))
                {
                    tx.Start();
                    if (!symbol.IsActive) { symbol.Activate(); doc.Regenerate(); }

                    double rot = Request.Rotation * Math.PI / 180.0;

                    foreach (var jp in Request.Points)
                    {
                        try
                        {
                            XYZ pt = JZPoint.ToXYZ(jp);
                            FamilyInstance fi = level != null
                                ? doc.Create.NewFamilyInstance(pt, symbol, level, StructuralType.NonStructural)
                                : doc.Create.NewFamilyInstance(pt, symbol, StructuralType.NonStructural);

                            if (fi != null)
                            {
                                if (Math.Abs(rot) > 1e-9)
                                {
                                    var axis = Line.CreateBound(pt, pt + XYZ.BasisZ);
                                    ElementTransformUtils.RotateElement(doc, fi.Id, axis, rot);
                                }
                                res.CreatedIds.Add(fi.Id.GetValue());
                            }
                        }
                        catch (Exception ex) { res.Warnings.Add($"Point failed: {ex.Message}"); }
                    }
                    tx.Commit();
                }

                res.Count = res.CreatedIds.Count;
                Result = new AIResult<MepInfoResult>
                {
                    Success = res.Count > 0,
                    Message = $"Placed {res.Count} instance(s) of '{symbol.FamilyName} : {symbol.Name}'" +
                              (res.Warnings.Count > 0 ? $". ⚠ {res.Warnings.Count} note(s): {res.Warnings[0]}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<MepInfoResult> { Success = false, Message = $"Error placing equipment: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Place MEP Equipment";
    }
}
