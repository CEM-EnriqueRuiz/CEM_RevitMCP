using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CEM_IAModeler.Utils
{
    /// <summary>
    /// 把每一次MCP工具调用追加到一个JSONL审计日志中，便于事后完整评估AI做了什么：
    /// 用了哪个工具(tool)、输入参数(input)、以及该动作产生的结果信息(result/error)。
    /// 与Screenshots并列放在CEMAIModeler目录下的Log文件夹中。
    ///
    /// Appends one JSON line per MCP tool invocation to an audit log, so the whole AI
    /// session can be evaluated afterwards: which tool was used, the input that drove it,
    /// and the result/error message it produced. Lives next to Screenshots.
    ///
    /// 失败绝不影响命令执行（仅写到Debug输出）。Logging never affects command execution.
    /// </summary>
    public static class ActionLogger
    {
        // 与TakeScreenshotEventHandler(Screenshots)保持一致的硬编码根目录。
        // TODO: 改为可配置项（设置/环境变量），目前与Screenshots一样硬编码。
        // Same hardcoded root as Screenshots. TODO: make configurable like Screenshots.
        private const string LogDirectory =
            @"C:\DC\ACCDocs\Cemengal\CMGL-TechnicalOffice\Project Files\01-Shared\02-Software\00-Revit\00-RevitAPI\05-CEMAIModeler\Log";

        // send_code_to_revit的脚本正文不记录（太嘈杂），只保留其余参数。
        // send_code_to_revit script bodies are not recorded (too noisy); the other params are kept.
        private const string SendCodeCommand = "send_code_to_revit";

        private static readonly object _lock = new object();

        /// <summary>
        /// 记录一次工具调用。method=工具名，paramsObject=输入(JObject)，
        /// success=是否成功，resultMessage=结果或错误信息(已是字符串)。
        /// </summary>
        public static void Log(string method, JObject paramsObject, bool success, string resultMessage)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    Directory.CreateDirectory(LogDirectory);

                var entry = new JObject
                {
                    ["ts"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff"),
                    ["tool"] = method ?? "",
                    ["input"] = BuildInputToken(method, paramsObject),
                    ["ok"] = success,
                    ["result"] = Truncate(resultMessage, 8000)
                };

                // 每天一个文件，JSONL（每行一个对象），便于批量分析与排序。
                // One file per day, JSONL (one object per line), easy to analyze in bulk.
                string fileName = $"actions_{DateTime.Now:yyyyMMdd}.jsonl";
                string filePath = Path.Combine(LogDirectory, fileName);

                lock (_lock)
                {
                    File.AppendAllText(filePath, entry.ToString(Formatting.None) + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"写入动作日志失败 / Failed to write action log: {ex.Message}");
            }
        }

        /// <summary>
        /// 对send_code_to_revit不保存脚本正文；其余工具记录完整输入。
        /// </summary>
        private static JToken BuildInputToken(string method, JObject paramsObject)
        {
            if (paramsObject == null)
                return JValue.CreateNull();

            if (string.Equals(method, SendCodeCommand, StringComparison.OrdinalIgnoreCase))
            {
                // 保留除脚本正文外的参数（如transactionMode），脚本正文用占位符替代。
                var redacted = (JObject)paramsObject.DeepClone();
                foreach (var key in new[] { "code", "data" })
                {
                    if (redacted[key] != null)
                        redacted[key] = "<omitted>";
                }
                return redacted;
            }

            return paramsObject;
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            return s.Length <= max ? s : s.Substring(0, max) + $"…(+{s.Length - max} chars)";
        }
    }
}
