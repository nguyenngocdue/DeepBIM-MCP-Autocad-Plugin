using autocad_mcp_plugin.Configuration;
using autocad_mcp_plugin.MCP.Interfaces;
using autocad_mcp_plugin.MCP.Models;
using autocad_mcp_plugin.Utils;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace autocad_mcp_plugin.Core
{
    /// <summary>
    /// TCP + HTTP socket server. Ported from Revit SocketService — no Revit API dependency.
    /// Listens for JSON-RPC requests and dispatches via CommandExecutor.
    /// </summary>
    public class SocketService
    {
        private static SocketService _instance;
        public static SocketService Instance =>
            _instance ?? (_instance = new SocketService());

        private TcpListener _listener;
        private Thread _listenerThread;
        private bool _isRunning;
        private int _port = 8180;
        private ICommandRegistry _commandRegistry;
        private ILogger _logger;
        private CommandExecutor _commandExecutor;
        private bool _isInitialized;

        private const int DEFAULT_PORT = 8180;
        private const int MAX_PORT     = 8199;
        private const int HTTP_PORT    = 9180;

        public bool IsRunning => _isRunning;
        public int Port => _port;

        private SocketService()
        {
            _commandRegistry = new AutoCADCommandRegistry();
            _logger = new Logger();
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            DocumentContextQueue.Instance.Initialize(_logger);

            _commandExecutor = new CommandExecutor(_commandRegistry, _logger);

            // Register all IAutoCADCommand implementations from the current assembly directly.
            // This avoids depending on command.json / assembly path resolution.
            RegisterCommandsFromCurrentAssembly();

            // Also try to load external commands via config (for extensibility)
            try
            {
                var configManager = new ConfigurationManager(_logger);
                configManager.LoadConfiguration();
                if (configManager.Config?.Commands != null && configManager.Config.Commands.Count > 0)
                {
                    var commandManager = new CommandManager(_commandRegistry, _logger, configManager);
                    commandManager.LoadCommands();
                }
            }
            catch (System.Exception ex)
            {
                _logger.Warning("External command loading skipped: {0}", ex.Message);
            }

            _isInitialized = true;
            _logger.Info("AutoCAD MCP socket service initialized.");
        }

        private void RegisterCommandsFromCurrentAssembly()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            int count = 0;
            foreach (var type in assembly.GetTypes())
            {
                if (typeof(IAutoCADCommand).IsAssignableFrom(type) &&
                    !type.IsInterface &&
                    !type.IsAbstract)
                {
                    try
                    {
                        var cmd = (IAutoCADCommand)System.Activator.CreateInstance(type);
                        _commandRegistry.RegisterCommand(cmd);
                        _logger.Info("Auto-registered command: {0} ({1})", cmd.CommandName, type.Name);
                        count++;
                    }
                    catch (System.Exception ex)
                    {
                        _logger.Error("Failed to register {0}: {1}", type.FullName, ex.Message);
                    }
                }
            }
            _logger.Info("Auto-registered {0} command(s) from current assembly.", count);
        }

        public void Start()
        {
            if (_isRunning) return;

            int lastPort = TryReadLastPort();
            foreach (int port in GetPortOrder(lastPort))
            {
                try
                {
                    _listener = new TcpListener(IPAddress.Any, port);
                    _listener.Start();
                    _port = port;
                    _isRunning = true;

                    _listenerThread = new Thread(ListenForClients) { IsBackground = true };
                    _listenerThread.Start();

                    SaveLastPort(port);
                    _logger.Info("TCP server listening on port {0}", _port);

                    StartHttp(HTTP_PORT);
                    return;
                }
                catch (SocketException)
                {
                    try { _listener?.Stop(); } catch { }
                    _listener = null;
                }
            }

            throw new Exception($"No available port in range {DEFAULT_PORT}-{MAX_PORT}.");
        }

        public void Stop()
        {
            _isRunning = false;
            try { _listener?.Stop(); } catch { }
            DocumentContextQueue.Instance.Shutdown();
            _logger.Info("AutoCAD MCP socket service stopped.");
        }

        // ── Port management ──────────────────────────────────────────────────

        private int TryReadLastPort()
        {
            try
            {
                string path = PathManager.GetPortFilePath();
                if (File.Exists(path) && int.TryParse(File.ReadAllText(path).Trim(), out int p))
                    return p;
            }
            catch { }
            return DEFAULT_PORT;
        }

        private void SaveLastPort(int port)
        {
            try
            {
                string path = PathManager.GetPortFilePath();
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(path, port.ToString());
            }
            catch { }
        }

        private static System.Collections.Generic.IEnumerable<int> GetPortOrder(int preferred)
        {
            yield return preferred;
            for (int p = DEFAULT_PORT; p <= MAX_PORT; p++)
                if (p != preferred) yield return p;
        }

        // ── TCP listener ─────────────────────────────────────────────────────

        private void ListenForClients()
        {
            while (_isRunning)
            {
                try
                {
                    var client = _listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
                }
                catch { if (_isRunning) _logger.Warning("TCP accept error."); }
            }
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    var buffer = new byte[65536];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) return;

                    string requestJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    _logger.Debug("TCP received: {0}", requestJson);

                    string responseJson = ProcessJsonRPC(requestJson);

                    byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
                    stream.Write(responseBytes, 0, responseBytes.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("TCP client error: {0}", ex.Message);
            }
        }

        // ── HTTP listener ────────────────────────────────────────────────────

        public void StartHttp(int httpPort)
        {
            try
            {
                var tcpHttp = new TcpListener(IPAddress.Any, httpPort);
                tcpHttp.Start();

                var thread = new Thread(() => AcceptHttpConnections(tcpHttp))
                {
                    IsBackground = true,
                    Name = "AcadHttpRawListener"
                };
                thread.Start();

                _logger.Info("HTTP server listening on port {0}", httpPort);
            }
            catch (Exception ex)
            {
                _logger.Warning("Failed to start HTTP server on port {0}: {1}", httpPort, ex.Message);
            }
        }

        private void AcceptHttpConnections(TcpListener tcpHttp)
        {
            while (_isRunning)
            {
                try
                {
                    var client = tcpHttp.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(_ => HandleRawHttpClient(client));
                }
                catch { if (_isRunning) _logger.Warning("HTTP accept error."); }
            }
        }

        private void HandleRawHttpClient(TcpClient client)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    var buffer = new byte[65536];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) return;

                    string raw = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Extract HTTP body (after \r\n\r\n)
                    int bodyStart = raw.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                    string body = bodyStart >= 0 ? raw.Substring(bodyStart + 4) : raw;

                    string responseBody = ProcessJsonRPC(body.Trim());

                    string httpResponse =
                        "HTTP/1.1 200 OK\r\n" +
                        "Content-Type: application/json; charset=utf-8\r\n" +
                        $"Content-Length: {Encoding.UTF8.GetByteCount(responseBody)}\r\n" +
                        "Access-Control-Allow-Origin: *\r\n" +
                        "Connection: close\r\n\r\n" +
                        responseBody;

                    byte[] resp = Encoding.UTF8.GetBytes(httpResponse);
                    stream.Write(resp, 0, resp.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("HTTP client error: {0}", ex.Message);
            }
        }

        // ── JSON-RPC processing ───────────────────────────────────────────────

        private string ProcessJsonRPC(string requestJson)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<JsonRPCRequest>(requestJson);
                if (request == null || string.IsNullOrEmpty(request.Method))
                    return BuildError(null, JsonRPCErrorCodes.InvalidRequest, "Invalid JSON-RPC request");

                return _commandExecutor.ExecuteCommand(request);
            }
            catch (JsonException ex)
            {
                _logger.Error("JSON parse error: {0}", ex.Message);
                return BuildError(null, JsonRPCErrorCodes.ParseError, $"Parse error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.Error("ProcessJsonRPC error: {0}", ex.Message);
                return BuildError(null, JsonRPCErrorCodes.InternalError, ex.Message);
            }
        }

        private static string BuildError(string id, int code, string message)
        {
            var resp = new JsonRPCErrorResponse
            {
                Id = id,
                Error = new JsonRPCError { Code = code, Message = message }
            };
            return resp.ToJson();
        }
    }
}
