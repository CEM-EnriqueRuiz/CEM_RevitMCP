using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Models.JsonRPC;
using RevitMCPSDK.API.Interfaces;
using CEM_IAModeler.Configuration;
using CEM_IAModeler.Security;
using CEM_IAModeler.Utils;

namespace CEM_IAModeler.Core
{
    public class SocketService
    {
        /// <summary>
        /// Interface the listener binds to. CEM divergence from upstream (which binds IPAddress.Any):
        /// the socket accepts arbitrary C# through send_code_to_revit, so it must never be reachable from
        /// the network. The TS server connects to 127.0.0.1 explicitly
        /// (server/src/utils/ConnectionManager.ts) so it does not depend on how 'localhost' resolves.
        /// Other local accounts can still reach it; each request must carry the listener's token
        /// (decision 0022, <see cref="McpAuthToken"/>).
        /// </summary>
        public static readonly IPAddress BindAddress = IPAddress.Loopback;

        private static SocketService _instance;
        private TcpListener _listener;
        private Thread _listenerThread;
        private bool _isRunning;
        private int _port = 8080;
        private UIApplication _uiApp;
        // The UIApplication whose ApplicationClosing calls Stop (decision 0022); null when not subscribed.
        private UIApplication _closingSource;
        private ICommandRegistry _commandRegistry;
        private ILogger _logger;
        private CommandExecutor _commandExecutor;

        public static SocketService Instance
        {
            get
            {
                if(_instance == null)
                    _instance = new SocketService();
                return _instance;
            }
        }

        /// <summary>
        /// True if the singleton has already been created. Lets callers (e.g. shutdown)
        /// check state without lazily instantiating the service.
        /// </summary>
        public static bool HasInstance => _instance != null;

        private SocketService()
        {
            _commandRegistry = new RevitCommandRegistry();
            _logger = new Logger();
        }

        public bool IsRunning => _isRunning;

        public int Port
        {
            get => _port;
            set => _port = value;
        }

        // 初始化
        // Initialization.
        public void Initialize(UIApplication uiApp)
        {
            _uiApp = uiApp;
            HookShutdown(uiApp);

            // 初始化事件管理器
            // Initialize ExternalEventManager
            ExternalEventManager.Instance.Initialize(uiApp, _logger);

            // 记录当前 Revit 版本
            // Get the current Revit version.
            var versionAdapter = new RevitMCPSDK.API.Utils.RevitVersionAdapter(_uiApp.Application);
            string currentVersion = versionAdapter.GetRevitVersion();
            _logger.Info("当前 Revit 版本: {0}\nCurrent Revit version: {0}", currentVersion);



            // 创建命令执行器
            // Create CommandExecutor
            _commandExecutor = new CommandExecutor(_commandRegistry, _logger);

            // 加载配置并注册命令
            // Load configuration and register commands.
            ConfigurationManager configManager = new ConfigurationManager(_logger);
            configManager.LoadConfiguration();
            

            //// 从配置中读取服务端口
            //// Read the service port from the configuration.
            //if (configManager.Config.Settings.Port > 0)
            //{
            //    _port = configManager.Config.Settings.Port;
            //}
            _port = 8080; // 固定端口号 - Hard-wired port number.

            // 加载命令
            // Load command.
            CommandManager commandManager = new CommandManager(
                _commandRegistry, _logger, configManager, _uiApp);
            commandManager.LoadCommands();

            _logger.Info("Socket service initialized on port {0}", _port);
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _isRunning = true;
                _listener = new TcpListener(BindAddress, _port);
                // CEM (SEC-5.2, CWE-605): no other socket, this user's included, may bind our port while we listen.
                // Set on each new listener before it binds; Stop drops it, so the ribbon's stop/start sets it again.
                _listener.ExclusiveAddressUse = true;
                _listener.Start();
            }
            catch (Exception)
            {
                _isRunning = false;
                return;
            }

            // CEM (decision 0022): a fresh token for this listener, once the port is ours (a second Revit that cannot
            // bind must not replace the first one's token) and before any client is accepted. Without it nobody could
            // call, so the listener stops and says why.
            try
            {
                McpAuthToken.Issue();
            }
            catch (Exception ex)
            {
                _isRunning = false;
                _listener.Stop();
                _listener = null;
                _logger.Error("The MCP token file could not be written ({0}); the server is not started.", ex.Message);
                throw new InvalidOperationException(
                    $"The MCP server did not start: its token file {McpAuthToken.FilePath} could not be written ({ex.Message}).", ex);
            }

            _listenerThread = new Thread(ListenForClients)
            {
                IsBackground = true
            };
            _listenerThread.Start();
        }

        public void Stop()
        {
            UnhookShutdown();
            if (!_isRunning) return;

            try
            {
                _isRunning = false;

                _listener?.Stop();
                _listener = null;

                // CEM (decision 0022): every request is refused from now on, and the file goes.
                McpAuthToken.Revoke();

                if(_listenerThread!=null && _listenerThread.IsAlive)
                {
                    _listenerThread.Join(1000);
                }
            }
            catch (Exception)
            {
                // log error
            }
        }

        // CEM (decision 0022): the host may never call the plugin's OnShutdown (CEM_RibbonUI has none), which left the
        // token file behind after Revit closed. Revit's own closing event stops the listener and revokes the token
        // whatever the host does; the OnShutdown path then finds it stopped. One subscription at a time, removed on Stop.
        private void HookShutdown(UIApplication uiApp)
        {
            if (_closingSource != null) return;
            uiApp.ApplicationClosing += OnApplicationClosing;
            _closingSource = uiApp;
        }

        private void UnhookShutdown()
        {
            var source = _closingSource;
            if (source == null) return;
            _closingSource = null;
            try { source.ApplicationClosing -= OnApplicationClosing; }
            catch (Exception ex)
            {
                // Outside an API context Revit refuses the change; the handler's Stop is a no-op once stopped.
                _logger.Warning("The Revit closing handler could not be removed: {0}", ex.Message);
            }
        }

        private void OnApplicationClosing(object sender, ApplicationClosingEventArgs e) => Stop();

        private void ListenForClients()
        {
            try
            {
                while (_isRunning)
                {
                    TcpClient client = _listener.AcceptTcpClient();

                    Thread clientThread = new Thread(HandleClientCommunication)
                    {
                        IsBackground = true
                    };
                    clientThread.Start(client);
                }
            }
            catch (SocketException)
            {
                
            }
            catch(Exception)
            {
                // log
            }
        }

        private void HandleClientCommunication(object clientObj)
        {
            TcpClient tcpClient = (TcpClient)clientObj;
            NetworkStream stream = tcpClient.GetStream();

            try
            {
                byte[] buffer = new byte[8192];

                while (_isRunning && tcpClient.Connected)
                {
                    // 读取客户端消息
                    // Read client messages.
                    int bytesRead = 0;

                    try
                    {
                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                    }
                    catch (IOException)
                    {
                        // 客户端断开连接
                        // Client disconnected.
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        // 客户端断开连接
                        // Client disconnected.
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    // CEM: the size only; the request carries the token (decision 0022).
                    System.Diagnostics.Trace.WriteLine($"收到消息 / Received message: {bytesRead} bytes");

                    string response = ProcessJsonRPCRequest(message);

                    // 发送响应
                    // Send response.
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseData, 0, responseData.Length);
                }
            }
            catch(Exception)
            {
                // log
            }
            finally
            {
                tcpClient.Close();
            }
        }

        private string ProcessJsonRPCRequest(string requestJson)
        {
            JsonRPCRequest request;

            try
            {
                // 解析JSON-RPC请求
                // Parse JSON-RPC requests.
                request = JsonConvert.DeserializeObject<JsonRPCRequest>(requestJson);

                // 验证请求格式是否有效
                // Verify that the request format is valid.
                if (request == null || !request.IsValid())
                {
                    return CreateErrorResponse(
                        null,
                        JsonRPCErrorCodes.InvalidRequest,
                        "Invalid JSON-RPC request"
                    );
                }

                // CEM divergence (decision 0022, SEC-5): only a request with this listener's token runs. A missing
                // or wrong one gets Unauthorized with no detail, and is logged by method only. The token is then
                // removed, so no command, log or audit line ever holds it.
                var parameters = request.Params as JObject;
                var presented = parameters?[McpAuthToken.Param] as JValue;
                if (!McpAuthToken.Matches(presented?.Value as string))
                {
                    _logger.Warning("Unauthorized request refused (missing or wrong token): {0}", Shorten(request.Method));
                    return CreateErrorResponse(request.Id, JsonRPCErrorCodes.Unauthorized, "Unauthorized");
                }
                parameters.Remove(McpAuthToken.Param);

                // CEM divergence: dispatch through CommandExecutor (the single chokepoint) so every
                // call, including send_code_to_revit, reaches the ActionLogger audit log. Upstream
                // executed the command directly here, which bypassed the log.
                return _commandExecutor.ExecuteCommand(request);
            }
            catch (JsonException)
            {
                // JSON解析错误
                // JSON parsing error.
                return CreateErrorResponse(
                    null,
                    JsonRPCErrorCodes.ParseError,
                    "Invalid JSON"
                );
            }
            catch (Exception ex)
            {
                // 处理请求时的其他错误
                // Catch other errors produced when processing requests.
                return CreateErrorResponse(
                    null,
                    JsonRPCErrorCodes.InternalError,
                    $"Internal error: {ex.Message}"
                );
            }
        }

        // A refused caller's method name: escaped first (caller text, CWE-117; the Logger escapes it too), then kept
        // short for the log.
        private static string Shorten(string method)
        {
            if (method == null) return "(none)";
            string escaped = LogText.Escape(method);
            return escaped.Length <= 80 ? escaped : escaped.Substring(0, 80) + "…";
        }

        private string CreateSuccessResponse(string id, object result)
        {
            var response = new JsonRPCSuccessResponse
            {
                Id = id,
                Result = result is JToken jToken ? jToken : JToken.FromObject(result)
            };

            return response.ToJson();
        }

        private string CreateErrorResponse(string id, int code, string message, object data = null)
        {
            var response = new JsonRPCErrorResponse
            {
                Id = id,
                Error = new JsonRPCError
                {
                    Code = code,
                    Message = message,
                    Data = data != null ? JToken.FromObject(data) : null
                }
            };

            return response.ToJson();
        }
    }
}
