using autocad_mcp_plugin.MCP.Interfaces;
using autocad_mcp_plugin.MCP.Models;
using Newtonsoft.Json.Linq;
using System;

namespace autocad_mcp_plugin.Core
{
    /// <summary>
    /// Dispatches JSON-RPC requests to registered IAutoCADCommand.
    /// Mirrors CommandExecutor from Revit plugin — no AutoCAD API dependency.
    /// </summary>
    public class CommandExecutor
    {
        private readonly ICommandRegistry _commandRegistry;
        private readonly ILogger _logger;

        public CommandExecutor(ICommandRegistry commandRegistry, ILogger logger)
        {
            _commandRegistry = commandRegistry;
            _logger = logger;
        }

        public string ExecuteCommand(JsonRPCRequest request)
        {
            try
            {
                if (!_commandRegistry.TryGetCommand(request.Method, out var command))
                {
                    _logger.Warning("Command not found: {0}", request.Method);
                    return CreateErrorResponse(request.Id,
                        JsonRPCErrorCodes.MethodNotFound,
                        $"Method not found: '{request.Method}'");
                }

                _logger.Info("Executing command: {0}", request.Method);

                try
                {
                    object result = command.Execute(request.GetParamsObject(), request.Id);
                    _logger.Info("Command {0} executed successfully.", request.Method);
                    return CreateSuccessResponse(request.Id, result);
                }
                catch (Exception ex)
                {
                    _logger.Error("Command {0} failed: {1}", request.Method, ex.Message);
                    return CreateErrorResponse(request.Id, JsonRPCErrorCodes.InternalError, ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error during command execution: {0}", ex.Message);
                return CreateErrorResponse(request.Id,
                    JsonRPCErrorCodes.InternalError,
                    $"Internal error: {ex.Message}");
            }
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
