using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Autodesk.Revit.UI;
using CEM_IAModeler_CommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;
// System.Drawing 与 Autodesk.Revit.DB 都有 Rectangle/Color 等同名类型，
// 这里用别名明确指向 GDI 类型，避免歧义。
using Bitmap = System.Drawing.Bitmap;
using Graphics = System.Drawing.Graphics;
using Rectangle = System.Drawing.Rectangle;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using CopyPixelOperation = System.Drawing.CopyPixelOperation;

namespace CEM_IAModeler_CommandSet.Services.AnnotationComponents
{
    /// <summary>
    /// 截图外部事件处理器：在Revit的UI线程上对Revit主窗口做一次实时屏幕抓取(GDI)，
    /// 把结果编码为PNG(base64)返回，供AI模型评估当前操作结果。
    /// Captures a live GDI screen grab of the Revit main window (ribbon, panels,
    /// selection highlights and all) on the UI thread and returns it as a base64 PNG.
    /// </summary>
    public class TakeScreenshotEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        // 单张截图的最大边长（像素）。超过则等比缩小，避免base64负载过大撑爆socket。
        // Max longest-edge in pixels; larger captures are scaled down to keep the
        // base64 payload reasonable over the socket transport.
        private const int MaxEdge = 1920;

        // 可选：把每张截图另存为带时间戳的PNG，作为审计/调试记录。
        // 留空则不落盘。TODO: 改为可配置项。
        // Optional on-disk copy (audit trail). Empty = don't save. TODO: make configurable.
        private const string ScreenshotSaveDirectory =
            @"C:\DC\ACCDocs\Cemengal\CMGL-TechnicalOffice\Project Files\01-Shared\02-Software\00-Revit\00-RevitAPI\05-CEMAIModeler\Screenshots";

        // 输入参数
        private bool _saveToDisk;

        // 执行结果
        public ScreenshotResult ResultInfo { get; private set; }

        // 状态同步对象
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(bool saveToDisk)
        {
            _saveToDisk = saveToDisk;
            TaskCompleted = false;
            _resetEvent.Reset();
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public void Execute(UIApplication app)
        {
            ResultInfo = new ScreenshotResult();
            try
            {
                Rectangle bounds = GetRevitWindowBounds();

                using (var raw = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb))
                {
                    using (var g = Graphics.FromImage(raw))
                    {
                        g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, raw.Size, CopyPixelOperation.SourceCopy);
                    }

                    using (var final = ScaleIfNeeded(raw))
                    using (var ms = new MemoryStream())
                    {
                        final.Save(ms, ImageFormat.Png);
                        byte[] png = ms.ToArray();

                        ResultInfo.ImageBase64 = Convert.ToBase64String(png);
                        ResultInfo.MimeType = "image/png";
                        ResultInfo.Width = final.Width;
                        ResultInfo.Height = final.Height;
                        ResultInfo.Success = true;

                        if (_saveToDisk)
                            ResultInfo.SavedPath = TrySavePng(png);
                    }
                }
            }
            catch (Exception ex)
            {
                ResultInfo.Success = false;
                ResultInfo.ErrorMessage = $"截图失败 / Screenshot failed: {ex.Message}";
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        /// <summary>
        /// 取Revit主窗口的屏幕矩形；取不到则回退到整个虚拟桌面。
        /// Returns the Revit main-window rectangle, falling back to the whole virtual screen.
        /// </summary>
        private static Rectangle GetRevitWindowBounds()
        {
            try
            {
                IntPtr hwnd = Process.GetCurrentProcess().MainWindowHandle;
                if (hwnd != IntPtr.Zero && GetWindowRect(hwnd, out RECT r))
                {
                    int w = r.Right - r.Left;
                    int h = r.Bottom - r.Top;
                    if (w > 0 && h > 0)
                        return new Rectangle(r.Left, r.Top, w, h);
                }
            }
            catch
            {
                // fall through to virtual screen
            }

            return new Rectangle(
                GetSystemMetrics(SM_XVIRTUALSCREEN),
                GetSystemMetrics(SM_YVIRTUALSCREEN),
                GetSystemMetrics(SM_CXVIRTUALSCREEN),
                GetSystemMetrics(SM_CYVIRTUALSCREEN));
        }

        /// <summary>等比缩小到 MaxEdge 以内；已够小则直接克隆返回。</summary>
        private static Bitmap ScaleIfNeeded(Bitmap source)
        {
            int longest = Math.Max(source.Width, source.Height);
            if (longest <= MaxEdge)
                return (Bitmap)source.Clone();

            double scale = (double)MaxEdge / longest;
            int newW = Math.Max(1, (int)Math.Round(source.Width * scale));
            int newH = Math.Max(1, (int)Math.Round(source.Height * scale));

            var scaled = new Bitmap(newW, newH, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(scaled))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(source, 0, 0, newW, newH);
            }
            return scaled;
        }

        private static string TrySavePng(byte[] png)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ScreenshotSaveDirectory))
                    return string.Empty;

                if (!Directory.Exists(ScreenshotSaveDirectory))
                    Directory.CreateDirectory(ScreenshotSaveDirectory);

                string fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
                string filePath = Path.Combine(ScreenshotSaveDirectory, fileName);
                File.WriteAllBytes(filePath, png);
                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存截图失败 / Failed to save screenshot: {ex.Message}");
                return string.Empty;
            }
        }

        public string GetName()
        {
            return "截取Revit窗口截图 / Take Revit window screenshot";
        }

        #region Win32

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;

        #endregion
    }
}
