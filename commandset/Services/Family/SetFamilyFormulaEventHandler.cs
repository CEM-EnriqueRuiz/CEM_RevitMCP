using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using CEM_IAModeler_CommandSet.Models.Family;
using CEM_IAModeler_CommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Services.Family
{
    /// <summary>
    ///     Sets a formula on a family parameter (FamilyManager.SetFormula). Family-editor only.
    /// </summary>
    public class SetFamilyFormulaEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public FamilySetFormulaRequest Request { get; set; }
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
                        Message = "family_set_formula only works inside the Family Editor (open an .rfa first).",
                        Response = res
                    };
                    _resetEvent.Set();
                    return;
                }

                FamilyManager fm = doc.FamilyManager;
                FamilyParameter fp = fm.get_Parameter(Request.ParameterName);
                if (fp == null)
                {
                    Result = new AIResult<FamilyOpResult> { Success = false, Message = $"Family parameter '{Request.ParameterName}' not found.", Response = res };
                    _resetEvent.Set();
                    return;
                }

                using (var tx = new Transaction(doc, "Set Family Formula"))
                {
                    tx.Start();
                    // Empty formula clears it
                    fm.SetFormula(fp, string.IsNullOrWhiteSpace(Request.Formula) ? null : Request.Formula);
                    tx.Commit();
                }

                res.Count = 1;
                Result = new AIResult<FamilyOpResult>
                {
                    Success = true,
                    Message = $"Set formula on '{Request.ParameterName}'.",
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<FamilyOpResult> { Success = false, Message = $"Error setting formula: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Set Family Formula";
    }
}
