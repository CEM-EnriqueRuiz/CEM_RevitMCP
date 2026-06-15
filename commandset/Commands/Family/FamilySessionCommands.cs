using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Family;
using CEM_IAModeler_CommandSet.Services.Family;

namespace CEM_IAModeler_CommandSet.Commands.Family
{
    public class FamilyOpenSessionCommand : ExternalEventCommandBase
    {
        private FamilyOpenSessionEventHandler H => (FamilyOpenSessionEventHandler)Handler;
        public override string CommandName => "family_open_session";
        public FamilyOpenSessionCommand(UIApplication u) : base(new FamilyOpenSessionEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            H.Request = p?.ToObject<FamilyOpenSessionRequest>() ?? new FamilyOpenSessionRequest();
            if (RaiseAndWaitForCompletion(60000)) return H.Result;
            throw new TimeoutException("family_open_session timed out");
        }
    }

    public class FamilySaveSessionCommand : ExternalEventCommandBase
    {
        private FamilySaveSessionEventHandler H => (FamilySaveSessionEventHandler)Handler;
        public override string CommandName => "family_save_session";
        public FamilySaveSessionCommand(UIApplication u) : base(new FamilySaveSessionEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            H.Request = p?.ToObject<FamilySaveSessionRequest>() ?? new FamilySaveSessionRequest();
            if (RaiseAndWaitForCompletion(60000)) return H.Result;
            throw new TimeoutException("family_save_session timed out");
        }
    }
}
