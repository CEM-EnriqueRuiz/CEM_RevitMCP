using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    /// <summary>
    ///     Generic geometric transform over a batch of elements using ElementTransformUtils.
    /// </summary>
    public class TransformElementsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document doc => uiApp.ActiveUIDocument.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public TransformRequest Request { get; set; }
        public AIResult<TransformResult> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            var res = new TransformResult { Operation = Request.Operation };
            try
            {
                var ids = Request.ElementIds.Select(McpResolveUtils.ToElementId)
                    .Where(id => doc.GetElement(id) != null).ToList();
                if (ids.Count == 0)
                {
                    Result = new AIResult<TransformResult>
                        { Success = false, Message = "None of the elementIds resolved to elements." };
                    _resetEvent.Set();
                    return;
                }

                string op = (Request.Operation ?? "move").Trim().ToLowerInvariant();

                using (var tx = new Transaction(doc, $"Transform ({op})"))
                {
                    tx.Start();
                    switch (op)
                    {
                        case "move":
                            ElementTransformUtils.MoveElements(doc, ids, Vec(Request.Translation));
                            res.AffectedCount = ids.Count;
                            break;

                        case "copy":
                        {
                            var copied = ElementTransformUtils.CopyElements(doc, ids, Vec(Request.Translation));
                            res.NewElementIds = copied.Select(i => i.GetValue()).ToList();
                            res.AffectedCount = copied.Count;
                            break;
                        }

                        case "array":
                        {
                            int n = Math.Max(1, Request.Count);
                            XYZ step = Vec(Request.Translation);
                            for (int k = 1; k <= n; k++)
                            {
                                var copied = ElementTransformUtils.CopyElements(doc, ids, step.Multiply(k));
                                res.NewElementIds.AddRange(copied.Select(i => i.GetValue()));
                            }
                            res.AffectedCount = res.NewElementIds.Count;
                            break;
                        }

                        case "rotate":
                        {
                            XYZ origin = Request.AxisPoint != null ? JZPoint.ToXYZ(Request.AxisPoint) : XYZ.Zero;
                            XYZ dir = Request.AxisDirection != null ? Dir(Request.AxisDirection) : XYZ.BasisZ;
                            Line axis = Line.CreateBound(origin, origin + dir);
                            double rad = Request.Angle * Math.PI / 180.0;
                            ElementTransformUtils.RotateElements(doc, ids, axis, rad);
                            res.AffectedCount = ids.Count;
                            break;
                        }

                        case "mirror":
                        {
                            XYZ origin = Request.MirrorPlaneOrigin != null ? JZPoint.ToXYZ(Request.MirrorPlaneOrigin) : XYZ.Zero;
                            XYZ normal = Request.MirrorPlaneNormal != null ? Dir(Request.MirrorPlaneNormal) : XYZ.BasisX;
                            Plane plane = Plane.CreateByNormalAndOrigin(normal, origin);
                            ElementTransformUtils.MirrorElements(doc, ids, plane, Request.Copy);
                            res.AffectedCount = ids.Count;
                            break;
                        }

                        default:
                            res.Warnings.Add($"Unknown operation '{op}'. Use move/copy/array/rotate/mirror.");
                            break;
                    }
                    tx.Commit();
                }

                Result = new AIResult<TransformResult>
                {
                    Success = res.Warnings.Count == 0,
                    Message = $"{op}: affected {res.AffectedCount} element(s)" +
                              (res.NewElementIds.Count > 0 ? $", created {res.NewElementIds.Count}" : "") +
                              (res.Warnings.Count > 0 ? $". ⚠ {string.Join("; ", res.Warnings)}" : "."),
                    Response = res
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<TransformResult>
                    { Success = false, Message = $"Error during transform: {ex.Message}", Response = res };
            }
            finally { _resetEvent.Set(); }
        }

        private static XYZ Vec(JZPoint p) => p != null ? JZPoint.ToXYZ(p) : XYZ.Zero;

        private static XYZ Dir(JZPoint p)
        {
            var v = new XYZ(p.X, p.Y, p.Z);
            return v.IsZeroLength() ? XYZ.BasisZ : v.Normalize();
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "Transform Elements";
    }
}
