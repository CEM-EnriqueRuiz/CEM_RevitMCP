using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using CEM_IAModeler_CommandSet.Models.Architecture;
using CEM_IAModeler_CommandSet.Services.Architecture;

namespace CEM_IAModeler_CommandSet.Commands.Architecture
{
    public class CreateRoofCommand : ExternalEventCommandBase
    {
        private CreateRoofEventHandler H => (CreateRoofEventHandler)Handler;
        public override string CommandName => "arch_create_roof";
        public CreateRoofCommand(UIApplication u) : base(new CreateRoofEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<CreateRoofRequest>();
            if (d == null || d.Boundary == null || d.Boundary.Count < 3) throw new ArgumentException("boundary (>=3 points) required");
            H.Request = d; if (RaiseAndWaitForCompletion(30000)) return H.Result; throw new TimeoutException("arch_create_roof timed out");
        }
    }

    public class CreateCurtainWallCommand : ExternalEventCommandBase
    {
        private CreateCurtainWallEventHandler H => (CreateCurtainWallEventHandler)Handler;
        public override string CommandName => "arch_create_curtain_wall";
        public CreateCurtainWallCommand(UIApplication u) : base(new CreateCurtainWallEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<CreateCurtainWallRequest>();
            if (d == null || d.Start == null || d.End == null) throw new ArgumentException("start and end required");
            H.Request = d; if (RaiseAndWaitForCompletion(30000)) return H.Result; throw new TimeoutException("arch_create_curtain_wall timed out");
        }
    }

    public class CreateAreaPlanCommand : ExternalEventCommandBase
    {
        private CreateAreaPlanEventHandler H => (CreateAreaPlanEventHandler)Handler;
        public override string CommandName => "arch_create_area_plan";
        public CreateAreaPlanCommand(UIApplication u) : base(new CreateAreaPlanEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<CreateAreaPlanRequest>() ?? new CreateAreaPlanRequest();
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("arch_create_area_plan timed out");
        }
    }

    public class CreateAreaCommand : ExternalEventCommandBase
    {
        private CreateAreaEventHandler H => (CreateAreaEventHandler)Handler;
        public override string CommandName => "arch_create_area";
        public CreateAreaCommand(UIApplication u) : base(new CreateAreaEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<CreateAreaRequest>();
            if (d == null || d.Points == null || d.Points.Count == 0) throw new ArgumentException("points required");
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("arch_create_area timed out");
        }
    }

    public class CreateSeparatorCommand : ExternalEventCommandBase
    {
        private CreateSeparatorEventHandler H => (CreateSeparatorEventHandler)Handler;
        public override string CommandName => "arch_create_separator";
        public CreateSeparatorCommand(UIApplication u) : base(new CreateSeparatorEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<CreateSeparatorRequest>();
            if (d == null || d.Points == null || d.Points.Count < 2) throw new ArgumentException("points (>=2) required");
            H.Request = d; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("arch_create_separator timed out");
        }
    }

    public class JoinGeometryCommand : ExternalEventCommandBase
    {
        private JoinGeometryEventHandler H => (JoinGeometryEventHandler)Handler;
        public override string CommandName => "arch_join_geometry";
        public JoinGeometryCommand(UIApplication u) : base(new JoinGeometryEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<JoinRequest>();
            if (d == null || d.FirstId == 0 || d.SecondId == 0) throw new ArgumentException("firstId and secondId required");
            H.Request = d; H.Join = true; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("arch_join_geometry timed out");
        }
    }

    public class UnjoinGeometryCommand : ExternalEventCommandBase
    {
        private JoinGeometryEventHandler H => (JoinGeometryEventHandler)Handler;
        public override string CommandName => "arch_unjoin_geometry";
        public UnjoinGeometryCommand(UIApplication u) : base(new JoinGeometryEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<JoinRequest>();
            if (d == null || d.FirstId == 0 || d.SecondId == 0) throw new ArgumentException("firstId and secondId required");
            H.Request = d; H.Join = false; if (RaiseAndWaitForCompletion(20000)) return H.Result; throw new TimeoutException("arch_unjoin_geometry timed out");
        }
    }

    public class CreateStairsCommand : ExternalEventCommandBase
    {
        private CreateStairsEventHandler H => (CreateStairsEventHandler)Handler;
        public override string CommandName => "arch_create_stairs";
        public CreateStairsCommand(UIApplication u) : base(new CreateStairsEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<CreateStairsRequest>();
            if (d == null || d.RunStart == null || d.RunEnd == null) throw new ArgumentException("runStart and runEnd required");
            H.Request = d; if (RaiseAndWaitForCompletion(45000)) return H.Result; throw new TimeoutException("arch_create_stairs timed out");
        }
    }

    public class CreateRailingCommand : ExternalEventCommandBase
    {
        private CreateRailingEventHandler H => (CreateRailingEventHandler)Handler;
        public override string CommandName => "arch_create_railing";
        public CreateRailingCommand(UIApplication u) : base(new CreateRailingEventHandler(), u) { }
        public override object Execute(JObject p, string r)
        {
            var d = p?.ToObject<CreateRailingRequest>();
            if (d == null || d.HostId == 0) throw new ArgumentException("hostId (stairs/ramp) required");
            H.Request = d; if (RaiseAndWaitForCompletion(30000)) return H.Result; throw new TimeoutException("arch_create_railing timed out");
        }
    }
}
