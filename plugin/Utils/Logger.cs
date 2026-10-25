using CEM_CSharpExtended.Hosting;
using RevitMCPSDK.API.Interfaces;
using System;

namespace CEM_IAModeler.Utils
{
    public class Logger : ILogger
    {
        private LogLevel _currentLogLevel = LogLevel.Info;

        public void Log(LogLevel level, string message, params object[] args)
        {
            if (level < _currentLogLevel)
                return;

            string formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {formattedMessage}";

            // 输出到 Debug 窗口
            // Output to debug window.
            System.Diagnostics.Debug.WriteLine(logEntry);

            // 写入日志文件：本机自清理的临时目录（根决策0031），不写在DLL旁边。失败不抛出异常。
            // Write to today's mcp-yyyyMMdd.log in %TEMP%\Cemengal\RevitMCP (root decision 0031), never next to
            // this DLL, which loads from ACC on colleagues' machines. AppendLog never throws.
            CemTempFolder.AppendLog(McpLocalFiles.App, "mcp", logEntry);
        }

        public void Debug(string message, params object[] args)
        {
            Log(LogLevel.Debug, message, args);
        }

        public void Info(string message, params object[] args)
        {
            Log(LogLevel.Info, message, args);
        }

        public void Warning(string message, params object[] args)
        {
            Log(LogLevel.Warning, message, args);
        }

        public void Error(string message, params object[] args)
        {
            Log(LogLevel.Error, message, args);
        }
    }
}
