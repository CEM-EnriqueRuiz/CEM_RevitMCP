using System.Diagnostics;
using System.IO;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_CSharpExtended.Hosting;
using CEM_IAModeler_CommandSet.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Interfaces;

namespace CEM_IAModeler_CommandSet.Commands.ExecuteDynamicCode
{
    /// <summary>
    /// 处理代码执行的外部事件处理器
    /// </summary>
    public class ExecuteCodeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public const string TransactionModeAuto = "auto";
        public const string TransactionModeNone = "none";

        public const string OutcomeOk = "ok";
        public const string OutcomeError = "error";
        public const string OutcomeRolledBack = "rolledBack";

        // 当前调用。A snippet that outlives its timeout keeps its own run, so the next call never mixes with it.
        private volatile ExecutionRun _pending;

        // 设置要执行的代码和参数
        public ExecutionRun SetExecutionParameters(string code, object[] parameters, string transactionMode, string codeFile, string runId)
        {
            var run = new ExecutionRun(code, parameters ?? Array.Empty<object>(),
                transactionMode == TransactionModeNone ? TransactionModeNone : TransactionModeAuto, codeFile, runId);
            // A run abandoned before Revit started it is replaced here, and its event will run this one instead: it
            // never runs, so it gets its "never ran" line now (once, whoever claims it first).
            var previous = _pending;
            _pending = run;
            if (previous != null && previous.TryClaimNeverRan())
                LogLateCompletion(previous, "It never ran: the call had timed out before Revit could start it, and a newer call replaced it.");
            return run;
        }

        // 等待执行完成 - IWaitableExternalEventHandler接口实现。No Reset here: a run that completed before the wait began
        // would be lost as a false timeout.
        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            var run = _pending;
            return run != null && run.Done.WaitOne(timeoutMilliseconds);
        }

        public void Execute(UIApplication app)
        {
            var run = _pending;
            if (run == null) return;
            if (!run.TryStart())
            {
                // Abandoned before Revit got to it: the caller was told it timed out, so it does not run late.
                if (run.TryClaimNeverRan()) LogLateCompletion(run, "It never ran: the call had already timed out before Revit could start it.");
                return;
            }

            var info = run.Result;
            var watch = Stopwatch.StartNew();
            bool transactionStarted = false;
            try
            {
                var doc = app.ActiveUIDocument.Document;
                info.DocumentTitle = doc.Title;
                info.IsFamilyDocument = doc.IsFamilyDocument;

                // Compile first: a compile error runs nothing, so it has nothing to roll back.
                var execute = Compile(run.Code);

                object result;
                if (run.TransactionMode == TransactionModeNone)
                {
                    result = Invoke(execute, doc, run.Parameters);
                    info.Outcome = OutcomeOk;
                }
                else
                {
                    using (var transaction = new Transaction(doc, "执行AI代码"))
                    {
                        transaction.Start();
                        transactionStarted = true;

                        result = Invoke(execute, doc, run.Parameters);

                        if (transaction.Commit() != TransactionStatus.Committed)
                        {
                            info.Outcome = OutcomeRolledBack;
                            info.ErrorMessage = "Revit rolled the transaction back on commit (a failure it could not resolve): " +
                                                "the snippet's changes to the active document are undone.";
                        }
                        else info.Outcome = OutcomeOk;
                    }
                }

                info.Success = info.Outcome == OutcomeOk;
                info.Result = JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                var cause = ex is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : ex;
                info.Success = false;
                if (transactionStarted)
                {
                    // Disposing the uncommitted transaction rolls it back.
                    info.Outcome = OutcomeRolledBack;
                    info.ErrorMessage = $"执行失败: {cause.Message} The transaction was rolled back: the snippet's changes to the active document are undone.";
                }
                else
                {
                    info.Outcome = OutcomeError;
                    info.ErrorMessage = run.TransactionMode == TransactionModeNone && !(cause is CompilationFailedException)
                        ? $"执行失败: {cause.Message} transactionMode 'none': nothing was rolled back; what the snippet committed stays."
                        : $"执行失败: {cause.Message}";
                }
            }
            finally
            {
                info.DurationMs = watch.ElapsedMilliseconds;
                if (run.Complete())
                    LogLateCompletion(run, $"The call was reported as timed out after {ExecuteCodeCommand.TimeoutMs / 1000} s; " +
                                           $"the code went on running and ended after {info.DurationMs / 1000.0:0.#} s.");
                run.Done.Set();
            }
        }

        /// <summary>
        /// The action-log line for a run whose call had already timed out (decision 0027), in the
        /// <c>{ts, tool, input, ok, result}</c> shape of <c>ActionLogger</c>, flagged <c>late</c>. Never throws.
        /// </summary>
        private static void LogLateCompletion(ExecutionRun run, string note)
        {
            try
            {
                var result = JObject.FromObject(run.Result);
                result["note"] = note;
                var entry = new JObject
                {
                    ["ts"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff"),
                    ["tool"] = "send_code_to_revit",
                    ["input"] = new JObject { ["codeFile"] = run.Result.CodeFile, ["transactionMode"] = run.TransactionMode },
                    ["ok"] = run.Result.Success,
                    ["result"] = result,
                    ["late"] = true
                };
                CemTempFolder.AppendLog(McpLocalFiles.App, "actions", entry.ToString(Formatting.None), ".jsonl");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"send_code_to_revit: could not log the late completion: {ex.Message}");
            }
        }

        private static MethodInfo Compile(string code)
        {
            // 包装代码以规范入口点
            var wrappedCode = $@"
using System;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;

namespace AIGeneratedCode
{{
    public static class CodeExecutor
    {{
        public static object Execute(Document document, object[] parameters)
        {{
            // 用户代码入口
            {code}
        }}
    }}
}}";

            var syntaxTree = CSharpSyntaxTree.ParseText(wrappedCode);

            // 添加必要的程序集引用（引用所有已加载的程序集）
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .ToList();

            // 编译代码
            var compilation = CSharpCompilation.Create(
                "AIGeneratedCode",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);

                // 处理编译结果
                if (!result.Success)
                {
                    var errors = string.Join("\n", result.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .Select(d => $"Line {d.Location.GetLineSpan().StartLinePosition.Line}: {d.GetMessage()}"));
                    throw new CompilationFailedException($"代码编译错误:\n{errors}");
                }

                // 反射取得执行方法
                ms.Seek(0, SeekOrigin.Begin);
                var assembly = Assembly.Load(ms.ToArray());
                var executorType = assembly.GetType("AIGeneratedCode.CodeExecutor");
                return executorType.GetMethod("Execute");
            }
        }

        private static object Invoke(MethodInfo execute, Document doc, object[] parameters) =>
            execute.Invoke(null, new object[] { doc, parameters });

        public string GetName()
        {
            return "执行AI代码";
        }
    }

    /// <summary>The snippet did not compile; nothing ran.</summary>
    public class CompilationFailedException : Exception
    {
        public CompilationFailedException(string message) : base(message) { }
    }

    /// <summary>
    /// One send_code_to_revit call. Its state is guarded by one lock, so the command's timeout and the handler's start and
    /// completion agree: a run abandoned before it starts never runs, and one abandoned while running is logged late.
    /// </summary>
    public class ExecutionRun
    {
        private readonly object _gate = new object();

        public ExecutionRun(string code, object[] parameters, string transactionMode, string codeFile, string runId)
        {
            Code = code;
            Parameters = parameters;
            TransactionMode = transactionMode;
            Result = new ExecutionResultInfo { RunId = runId, CodeFile = codeFile, TransactionMode = transactionMode };
        }

        public string Code { get; }
        public object[] Parameters { get; }
        public string TransactionMode { get; }
        public ExecutionResultInfo Result { get; }
        public ManualResetEvent Done { get; } = new ManualResetEvent(false);

        public bool Started { get { lock (_gate) return _started; } }
        public bool Abandoned { get { lock (_gate) return _abandoned; } }

        private bool _started, _completed, _abandoned, _neverRanClaimed;

        /// <summary>
        /// True once, for a run abandoned before it started: the caller logs its "never ran" line. The handler (its
        /// event fired) and the next call (it replaced the run) may both try; only the first gets true.
        /// </summary>
        public bool TryClaimNeverRan()
        {
            lock (_gate)
            {
                if (_started || !_abandoned || _neverRanClaimed) return false;
                _neverRanClaimed = true;
                return true;
            }
        }

        /// <summary>The handler takes the run; false when it already ran or the call was abandoned first.</summary>
        public bool TryStart()
        {
            lock (_gate)
            {
                if (_started || _abandoned) return false;
                _started = true;
                return true;
            }
        }

        /// <summary>The handler is done; true when the call had already timed out, so this completion is late.</summary>
        public bool Complete()
        {
            lock (_gate)
            {
                _completed = true;
                return _abandoned;
            }
        }

        /// <summary>The call timed out; false when the run completed in the meantime, so its result can be returned.</summary>
        public bool TryAbandon()
        {
            lock (_gate)
            {
                if (_completed) return false;
                _abandoned = true;
                return true;
            }
        }
    }

    // 执行结果数据结构
    public class ExecutionResultInfo
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>"ok" | "error" (nothing to roll back, or transactionMode none) | "rolledBack".</summary>
        [JsonProperty("outcome")]
        public string Outcome { get; set; }

        [JsonProperty("runId")]
        public string RunId { get; set; }

        /// <summary>The exact code run, under %TEMP%\Cemengal\RevitMCP\code (decision 0027); null if it could not be written.</summary>
        [JsonProperty("codeFile")]
        public string CodeFile { get; set; }

        [JsonProperty("transactionMode")]
        public string TransactionMode { get; set; }

        /// <summary>The active document the snippet ran on.</summary>
        [JsonProperty("documentTitle")]
        public string DocumentTitle { get; set; }

        [JsonProperty("isFamilyDocument")]
        public bool IsFamilyDocument { get; set; }

        [JsonProperty("durationMs")]
        public long DurationMs { get; set; }
    }
}
