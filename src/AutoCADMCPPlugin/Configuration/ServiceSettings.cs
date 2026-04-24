using Newtonsoft.Json;

namespace autocad_mcp_plugin.Configuration
{
    public class ServiceSettings
    {
        [JsonProperty("logLevel")]
        public string LogLevel { get; set; } = "Info";

        [JsonProperty("port")]
        public int Port { get; set; } = 8180;
    }
}
