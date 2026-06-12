using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    /// <summary>
    /// Event handler for creating reference planes (ByLine / ByPoints / ByNormal)
    /// </summary>
    public class CreateReferencePlaneEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<ReferencePlaneData> CreatedInfo { get; private set; }
        public AIResult<List<int>> Result { get; private set; }

        public void SetParameters(List<ReferencePlaneData> data)
        {
            CreatedInfo = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                var createdIds = new List<int>();
                var warnings = new List<string>();

                using (Transaction tx = new Transaction(doc, "Create Reference Plane"))
                {
                    tx.Start();

                    foreach (var info in CreatedInfo)
                    {
                        try
                        {
                            // Resolve target view
                            View view = null;
                            if (info.ViewId > 0)
                                view = doc.GetElement(new ElementId(info.ViewId)) as View;
                            if (view == null)
                                view = doc.ActiveView;
                            if (view == null)
                            {
                                warnings.Add("No valid view available for reference plane creation");
                                continue;
                            }

                            XYZ cutVec = info.CutVector != null
                                ? new XYZ(info.CutVector.X, info.CutVector.Y, info.CutVector.Z).Normalize()
                                : XYZ.BasisZ;

                            ReferencePlane plane = null;
                            string method = (info.CreationMethod ?? "ByLine").Trim();

                            if (method.Equals("ByPoints", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.BubbleEnd == null || info.FreeEnd == null || info.ThirdPoint == null)
                                {
                                    warnings.Add("ByPoints requires bubbleEnd, freeEnd and thirdPoint");
                                    continue;
                                }
                                plane = doc.Create.NewReferencePlane2(
                                    JZPoint.ToXYZ(info.BubbleEnd),
                                    JZPoint.ToXYZ(info.FreeEnd),
                                    JZPoint.ToXYZ(info.ThirdPoint),
                                    view);
                            }
                            else if (method.Equals("ByNormal", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Origin == null || info.Normal == null)
                                {
                                    warnings.Add("ByNormal requires origin and normal");
                                    continue;
                                }

                                XYZ origin = JZPoint.ToXYZ(info.Origin);
                                XYZ normal = new XYZ(info.Normal.X, info.Normal.Y, info.Normal.Z).Normalize();

                                // Build an in-plane direction perpendicular to the normal
                                XYZ inPlane = Math.Abs(normal.DotProduct(XYZ.BasisZ)) > 0.99
                                    ? normal.CrossProduct(XYZ.BasisX).Normalize()
                                    : normal.CrossProduct(XYZ.BasisZ).Normalize();

                                double halfLen = Math.Max(info.Length, 100) / 304.8 / 2.0;
                                XYZ bubble = origin - inPlane * halfLen;
                                XYZ free = origin + inPlane * halfLen;

                                // Cut vector must lie in the plane and not be parallel to the line
                                XYZ planeCutVec = normal.CrossProduct(inPlane).Normalize();

                                plane = doc.Create.NewReferencePlane(bubble, free, planeCutVec, view);
                            }
                            else // ByLine (default)
                            {
                                if (info.BubbleEnd == null || info.FreeEnd == null)
                                {
                                    warnings.Add("ByLine requires bubbleEnd and freeEnd");
                                    continue;
                                }
                                plane = doc.Create.NewReferencePlane(
                                    JZPoint.ToXYZ(info.BubbleEnd),
                                    JZPoint.ToXYZ(info.FreeEnd),
                                    cutVec,
                                    view);
                            }

                            if (plane != null)
                            {
                                if (!string.IsNullOrWhiteSpace(info.Name))
                                {
                                    try { plane.Name = info.Name; }
                                    catch { warnings.Add($"Could not set name '{info.Name}' (possibly duplicated)"); }
                                }
                                createdIds.Add(plane.Id.GetIntValue());
                            }
                        }
                        catch (Exception exItem)
                        {
                            warnings.Add($"Error creating reference plane: {exItem.Message}");
                        }
                    }

                    tx.Commit();
                }

                string message = $"Successfully created {createdIds.Count} reference plane(s).";
                if (warnings.Count > 0)
                    message += "\n\n⚠ Warnings:\n  • " + string.Join("\n  • ", warnings);

                Result = new AIResult<List<int>>
                {
                    Success = true,
                    Message = message,
                    Response = createdIds
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<int>>
                {
                    Success = false,
                    Message = $"Error creating reference planes: {ex.Message}"
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName()
        {
            return "Create Reference Plane";
        }
    }
}
