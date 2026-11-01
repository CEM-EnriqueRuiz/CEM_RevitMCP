using System;
using CEM_CSharpExtended.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CEM_IAModeler.Utils
{
    /// <summary>
    /// 把每一次MCP工具调用追加到一个JSONL审计日志中，便于事后完整评估AI做了什么：
    /// 用了哪个工具(tool)、输入参数(input)、以及该动作产生的结果信息(result/error)。
    /// 写入本机自清理的临时目录 %TEMP%\Cemengal\RevitMCP（根决策0031）。
    ///
    /// Appends one JSON line per MCP tool invocation to an audit log, so the whole AI
    /// session can be evaluated afterwards: which tool was used, the input that drove it,
    /// and the result/error message it produced. Lives in the local, self-cleaning
    /// %TEMP%\Cemengal\RevitMCP (root decision 0031), next to Screenshots\.
    ///
    /// 失败绝不影响命令执行（仅写到Debug输出）。Logging never affects command execution.
    /// </summary>
    public static class ActionLogger
    {
        // send_code_to_revit的脚本正文写在code\文件中（决策0027），日志只指向它。
        // send_code_to_revit script bodies go to their own file in code\ (decision 0027): the line points at it through
        // the result's codeFile, and keeps the other params.
        private const string SendCodeCommand = "send_code_to_revit";

        /// <summary>
        /// 记录一次工具调用。method=工具名，paramsObject=输入(JObject)，
        /// success=是否成功，resultMessage=结果或错误信息(已是字符串)。
        /// </summary>
        public static void Log(string method, JObject paramsObject, bool success, string resultMessage)
        {
            try
            {
                var entry = new JObject
                {
                    ["ts"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff"),
                    ["tool"] = method ?? "",
                    ["input"] = BuildInputToken(method, paramsObject),
                    ["ok"] = success,
                    ["result"] = Truncate(resultMessage, 8000)
                };

                // 每天一个文件，JSONL（每行一个对象），便于批量分析与排序。
                // One file per day, JSONL (one object per line), easy to analyze in bulk:
                // actions-yyyyMMdd.jsonl. AppendLog is thread-safe and never throws.
                if (!CemTempFolder.AppendLog(McpLocalFiles.App, "actions", entry.ToString(Formatting.None), ".jsonl"))
                    System.Diagnostics.Debug.WriteLine("写入动作日志失败 / Failed to write action log.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"写入动作日志失败 / Failed to write action log: {ex.Message}");
            }
        }

        /// <summary>
        /// 对send_code_to_revit不在日志中记录脚本正文（它在result.codeFile文件里）；其余工具记录完整输入。
        /// </summary>
        private static JToken BuildInputToken(string method, JObject paramsObject)
        {
            if (paramsObject == null)
                return JValue.CreateNull();

            if (string.Equals(method, SendCodeCommand, StringComparison.OrdinalIgnoreCase))
            {
                // 保留除脚本正文外的参数（如transactionMode），脚本正文用指向codeFile的占位符替代。
                var redacted = (JObject)paramsObject.DeepClone();
                foreach (var key in new[] { "code", "data" })
                {
                    if (redacted[key] != null)
                        redacted[key] = "<see result.codeFile>";
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
