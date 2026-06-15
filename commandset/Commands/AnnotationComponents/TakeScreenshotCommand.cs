using System;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using CEM_IAModeler_CommandSet.Services.AnnotationComponents;
using RevitMCPSDK.API.Base;

namespace CEM_IAModeler_CommandSet.Commands.AnnotationComponents
{
    /// <summary>
    /// 截图命令：抓取Revit主窗口的实时画面，以base64 PNG返回，供AI模型评估当前结果。
    /// Captures a live screenshot of the Revit main window and returns it as a base64 PNG
    /// so the AI model can visually evaluate the current result and decide how to improve it.
    /// </summary>
    public class TakeScreenshotCommand : ExternalEventCommandBase
    {
        private TakeScreenshotEventHandler _handler => (TakeScreenshotEventHandler)Handler;

        public override string CommandName => "take_screenshot";

        public TakeScreenshotCommand(UIApplication uiApp)
            : base(new TakeScreenshotEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                // saveToDisk 可选，默认true：同时把截图存一份到磁盘作为审计记录。
                bool saveToDisk = parameters?["saveToDisk"]?.Value<bool>() ?? true;

                _handler.SetParameters(saveToDisk);

                // 截图很快，但缩放/编码大图也要时间，给30秒裕量。
                if (RaiseAndWaitForCompletion(30000))
                {
                    return _handler.ResultInfo;
                }

                throw new TimeoutException("截图超时 / Screenshot timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"截图失败 / Failed to take screenshot: {ex.Message}", ex);
            }
        }
    }
}
