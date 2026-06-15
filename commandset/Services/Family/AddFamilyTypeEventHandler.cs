using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Models.Family;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Family
{
    /// <summary>
    ///     Adds a new type to the family being edited (FamilyManager.NewType). Family-editor only.
    /// </summary>
    public class AddFamilyTypeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public FamilyAddTypeRequest Request { get; set; }
        public AIResult<FamilyOpResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new FamilyOpResult();
            try
            {
                if (!doc.IsFamilyDocument)
                {
                    Result = new AIResult<FamilyOpResult>
                    {
                        Success = false,
                        Message = "family_add_type only works inside the Family Editor (open an .rfa first).",
                        Response = res
                    };
                    _resetEvent.Set();
                    return;
                }

                FamilyManager fm = doc.FamilyManager;
                using (var tx = new Transaction(doc, "Add Family Type"))
                {
                    tx.Start();
                    FamilyType ft = fm.NewType(Request.TypeName);
                    if (ft != null) res.Count = 1;
                    tx.Commit();
                }

                Result = new AIResult<FamilyOpResult>
                {
                    Success = res.Count > 0,
                    Message = res.Count > 0 ? $"Added family type '{Request.TypeName}'." : "Type was not added.",
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<FamilyOpResult> { Success = false, Message = $"Error adding family type: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Add Family Type";
    }
}
