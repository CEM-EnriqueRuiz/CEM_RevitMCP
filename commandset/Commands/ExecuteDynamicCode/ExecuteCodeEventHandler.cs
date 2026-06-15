using System.IO;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
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

        // 当AI模型找不到合适的工具而退回到"C#脚本食谱"(send_code_to_revit)时，
        // 每次执行前都会把收到的代码保存为txt文件，便于日后将这些临时操作
        // 沉淀为专用工具。
        // TODO: 将该路径改为可配置项（设置/环境变量）。目前按需求硬编码到MCP仓库目录。
        private const string RecipesLogDirectory =
            @"C:\DC\ACCDocs\Cemengal\CMGL-TechnicalOffice\Project Files\01-Shared\02-Software\00-Revit\00-RevitAPI\05-CEMAIModeler\Recipes";

        // 代码执行参数
        private string _generatedCode;
        private object[] _executionParameters;
        private string _transactionMode = TransactionModeAuto;

        // 执行结果信息
        public ExecutionResultInfo ResultInfo { get; private set; }

        // 状态同步对象
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        // 设置要执行的代码和参数
        public void SetExecutionParameters(string code, object[] parameters = null, string transactionMode = TransactionModeAuto)
        {
            _generatedCode = code;
            _executionParameters = parameters ?? Array.Empty<object>();
            _transactionMode = transactionMode == TransactionModeNone ? TransactionModeNone : TransactionModeAuto;
            TaskCompleted = false;
            _resetEvent.Reset();
        }

        // 等待执行完成 - IWaitableExternalEventHandler接口实现
        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public void Execute(UIApplication app)
        {
            try
            {
                var doc = app.ActiveUIDocument.Document;
                ResultInfo = new ExecutionResultInfo();

                // 在编译/执行前，把这次AI生成的代码食谱保存为txt文件，留作日后提炼专用工具。
                SaveRecipeToFile(_generatedCode);

                object result;
                if (_transactionMode == TransactionModeNone)
                {
                    result = CompileAndExecuteCode(
                        code: _generatedCode,
                        doc: doc,
                        parameters: _executionParameters
                    );
                }
                else
                {
                    using (var transaction = new Transaction(doc, "执行AI代码"))
                    {
                        transaction.Start();

                        result = CompileAndExecuteCode(
                            code: _generatedCode,
                            doc: doc,
                            parameters: _executionParameters
                        );

                        transaction.Commit();
                    }
                }

                ResultInfo.Success = true;
                ResultInfo.Result = JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                ResultInfo.Success = false;
                ResultInfo.ErrorMessage = $"执行失败: {ex.Message}";
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        /// <summary>
        /// 把AI生成的C#代码食谱保存为txt文件。每次退回到send_code_to_revit时调用，
        /// 用于积累这些临时操作，便于日后封装为专用工具。
        /// 保存失败不会影响代码的正常执行（仅记录到调试输出）。
        /// </summary>
        private void SaveRecipeToFile(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                    return;

                if (!Directory.Exists(RecipesLogDirectory))
                    Directory.CreateDirectory(RecipesLogDirectory);

                // 文件名带时间戳，确保每次执行都是独立文件，且按时间排序。
                string fileName = $"recipe_{DateTime.Now:yyyyMMdd_HHmmss_fff}.txt";
                string filePath = Path.Combine(RecipesLogDirectory, fileName);

                File.WriteAllText(filePath, code);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存AI代码食谱失败: {ex.Message}");
            }
        }

        private object CompileAndExecuteCode(string code, Document doc, object[] parameters)
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
                    throw new Exception($"代码编译错误:\n{errors}");
                }

                // 反射调用执行方法
                ms.Seek(0, SeekOrigin.Begin);
                var assembly = Assembly.Load(ms.ToArray());
                var executorType = assembly.GetType("AIGeneratedCode.CodeExecutor");
                var executeMethod = executorType.GetMethod("Execute");

                return executeMethod.Invoke(null, new object[] { doc, parameters });
            }
        }

        public string GetName()
        {
            return "执行AI代码";
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
    }
}
