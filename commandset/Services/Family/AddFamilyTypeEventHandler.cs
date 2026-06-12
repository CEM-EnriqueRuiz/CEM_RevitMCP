using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Family
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
