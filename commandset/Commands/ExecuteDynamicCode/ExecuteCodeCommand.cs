using System.IO;
using System.Text;
using Autodesk.Revit.UI;
using CEM_CSharpExtended.IO;
using CEM_IAModeler_CommandSet.Utils;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;

namespace CEM_IAModeler_CommandSet.Commands.ExecuteDynamicCode
{
    /// <summary>
    /// 处理代码执行的命令类
    /// </summary>
    public class ExecuteCodeCommand : ExternalEventCommandBase
    {
        /// <summary>How long the call waits for Revit (1 minute). The snippet is not stopped when it expires.</summary>
        public const int TimeoutMs = 60000;

        private ExecuteCodeEventHandler _handler => (ExecuteCodeEventHandler)Handler;

        public override string CommandName => "send_code_to_revit";

        public ExecuteCodeCommand(UIApplication uiApp)
            : base(new ExecuteCodeEventHandler(), uiApp)
        {
        }

        /// <summary>
        /// Writes the exact code to <c>%TEMP%\Cemengal\RevitMCP\code\send_code_&lt;stamp&gt;_&lt;runId&gt;.cs</c> (decision 0027,
        /// root 0031: the self-cleaning local temp folder, never ACC). Fail-soft: null when it could not write, and the
        /// code still runs.
        /// </summary>
        private static string TraceCode(string code, string runId)
        {
            try
            {
                string stamped = CemTempFolder.NewArtifactFile(McpLocalFiles.App, "code", "send_code", ".cs");
                string codeFile = Path.Combine(Path.GetDirectoryName(stamped), Path.GetFileNameWithoutExtension(stamped) + "_" + runId + ".cs");
                File.WriteAllText(codeFile, code ?? "", new UTF8Encoding(false));
                return codeFile;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"send_code_to_revit: could not write the code file: {ex.Message}");
                return null;
            }
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                // 参数验证
                if (!parameters.ContainsKey("code"))
                {
                    throw new ArgumentException("Missing required parameter: 'code'");
                }

                // 解析代码和参数
                string code = parameters["code"].Value<string>();
                JArray parametersArray = parameters["parameters"] as JArray;
                object[] executionParameters = parametersArray?.ToObject<object[]>() ?? Array.Empty<object>();
                string transactionMode = parameters["transactionMode"]?.Value<string>() ?? ExecuteCodeEventHandler.TransactionModeAuto;

                // 记录将要执行的代码，再设置执行参数。Trace the code, then hand the run to the handler.
                string runId = Guid.NewGuid().ToString("N").Substring(0, 8);
                string codeFile = TraceCode(code, runId);
                var run = _handler.SetExecutionParameters(code, executionParameters, transactionMode, codeFile, runId);

                // 触发外部事件并等待完成。A run that completes in the race with the timeout still returns its result.
                if (RaiseAndWaitForCompletion(TimeoutMs) || !run.TryAbandon())
                {
                    return run.Result;
                }

                throw new TimeoutException(
                    $"代码执行超时 / No answer from Revit within {TimeoutMs / 1000} s (run {runId}, code {codeFile ?? "not traced"}). " +
                    (run.Started
                        ? "The code is STILL RUNNING in Revit and may go on changing the model: its real outcome is appended to the " +
                          "action log as a late completion when it ends. Check the model before you retry."
                        : "Revit had not started it (busy, or a dialog is open), and it will not run now."));
            }
            catch (Exception ex)
            {
                throw new Exception($"执行代码失败: {ex.Message}", ex);
            }
        }
    }
}
