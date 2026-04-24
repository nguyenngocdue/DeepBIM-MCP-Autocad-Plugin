using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.MCP.Models
{
    /// <summary>
    /// JSON-RPC 2.0 request — mirrors RevitMCPSDK.API.Models.JsonRPC.JsonRPCRequest
    /// </summary>
    public class JsonRPCRequest
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public JToken Params { get; set; }

        public JObject GetParamsObject()
        {
            if (Params == null) return new JObject();
            if (Params is JObject obj) return obj;
            return new JObject();
        }
    }

    /// <summary>
    /// JSON-RPC 2.0 success response
    /// </summary>
    public class JsonRPCSuccessResponse
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("result")]
        public JToken Result { get; set; }

        public string ToJson() => JsonConvert.SerializeObject(this);
    }

    /// <summary>
    /// JSON-RPC 2.0 error response
    /// </summary>
    public class JsonRPCErrorResponse
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("error")]
        public JsonRPCError Error { get; set; }

        public string ToJson() => JsonConvert.SerializeObject(this);
    }

    public class JsonRPCError
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public JToken Data { get; set; }
    }

    /// <summary>
    /// Standard JSON-RPC error codes
    /// </summary>
    public static class JsonRPCErrorCodes
    {
        public const int ParseError     = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams  = -32602;
        public const int InternalError  = -32603;
    }
}
