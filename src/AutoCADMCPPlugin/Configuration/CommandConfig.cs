using System.ComponentModel;
using Newtonsoft.Json;

namespace autocad_mcp_plugin.Configuration
{
    public class CommandConfig : INotifyPropertyChanged
    {
        private bool _enabled = true;

        [JsonProperty("commandName")]
        public string CommandName { get; set; }

        [JsonProperty("assemblyPath")]
        public string AssemblyPath { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [JsonProperty("description")]
        public string Description { get; set; } = "";
    }
}
