using autocad_mcp_plugin.Configuration;
using autocad_mcp_plugin.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace autocad_mcp_plugin.UI
{
    public partial class CommandSetSettingsPage : Page
    {
        private ObservableCollection<CommandSetInfo> _commandSets;
        private ObservableCollection<CommandConfig>  _currentCommands;

        public CommandSetSettingsPage()
        {
            InitializeComponent();

            _commandSets     = new ObservableCollection<CommandSetInfo>();
            _currentCommands = new ObservableCollection<CommandConfig>();

            CommandSetListBox.ItemsSource  = _commandSets;
            FeaturesListView.ItemsSource   = _currentCommands;

            FeaturesListView.Loaded      += (s, _) => UpdateDescriptionColumnWidth();
            FeaturesListView.SizeChanged += (s, _) => UpdateDescriptionColumnWidth();

            LoadCommandSets();
            if (_commandSets.Count > 0)
                CommandSetListBox.SelectedIndex = 0;
            NoSelectionTextBlock.Visibility = _commandSets.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Layout helpers ────────────────────────────────────────────────────

        private const double OnColWidth  = 44;
        private const double CmdColWidth = 200;

        private void UpdateDescriptionColumnWidth()
        {
            if (FeaturesListView.ActualWidth <= 0) return;
            double remaining = FeaturesListView.ActualWidth - OnColWidth - CmdColWidth - 24;
            DescriptionColumn.Width = Math.Max(200, remaining);
        }

        private void UpdateEnabledCount()
        {
            if (_currentCommands == null || _currentCommands.Count == 0)
            { EnabledCountTextBlock.Text = ""; return; }
            int n = _currentCommands.Count(c => c.Enabled);
            EnabledCountTextBlock.Text = n == _currentCommands.Count ? "All enabled" : $"{n} enabled";
        }

        // ── Search ────────────────────────────────────────────────────────────

        private void ApplySearchFilter()
        {
            var view = CollectionViewSource.GetDefaultView(FeaturesListView.ItemsSource);
            if (view == null) return;
            string q = SearchBox?.Text?.Trim().ToUpperInvariant() ?? "";
            view.Filter = string.IsNullOrEmpty(q)
                ? (Predicate<object>)null
                : obj => obj is CommandConfig c &&
                    ((c.CommandName ?? "").ToUpperInvariant().Contains(q) ||
                     (c.Description  ?? "").ToUpperInvariant().Contains(q));
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplySearchFilter();

        // ── Load ──────────────────────────────────────────────────────────────

        private void LoadCommandSets()
        {
            try
            {
                _commandSets.Clear();

                // command.json lives next to the DLL (or in Commands sub-folder)
                string pluginDir = PathManager.GetPluginDirectoryPath();
                string commandsDir = PathManager.GetCommandsDirectoryPath();

                // Look for sub-directories with command.json (same pattern as Revit plugin)
                var candidateDirs = new[] { commandsDir, pluginDir };
                bool loaded = false;

                foreach (string baseDir in candidateDirs)
                {
                    if (!Directory.Exists(baseDir)) continue;

                    string registryPath = Path.Combine(baseDir, "commandRegistry.json");

                    // Load existing enabled state
                    var savedState = new Dictionary<string, bool>();
                    if (File.Exists(registryPath))
                    {
                        try
                        {
                            var reg = JsonConvert.DeserializeObject<CommandRegistryModel>(
                                File.ReadAllText(registryPath));
                            if (reg?.Commands != null)
                                foreach (var c in reg.Commands)
                                    savedState[c.CommandName] = c.Enabled;
                        }
                        catch { }
                    }

                    // Each sub-directory = one command set
                    foreach (string dir in Directory.GetDirectories(baseDir))
                    {
                        string cmdJsonPath = Path.Combine(dir, "command.json");
                        if (!File.Exists(cmdJsonPath)) continue;

                        var data = JsonConvert.DeserializeObject<CommandJsonModel>(
                            File.ReadAllText(cmdJsonPath));
                        if (data?.Commands == null) continue;

                        var cs = new CommandSetInfo
                        {
                            Name        = data.Name ?? Path.GetFileName(dir),
                            Description = data.Description ?? "",
                            Commands    = new List<CommandConfig>()
                        };

                        foreach (var item in data.Commands)
                        {
                            bool enabled = savedState.TryGetValue(item.CommandName, out bool s) ? s : true;
                            cs.Commands.Add(new CommandConfig
                            {
                                CommandName  = item.CommandName,
                                Description  = item.Description ?? "",
                                AssemblyPath = item.AssemblyPath ?? "",
                                Enabled      = enabled
                            });
                        }

                        if (cs.Commands.Any())
                            _commandSets.Add(cs);
                        loaded = true;
                    }

                    // Also check root command.json (the plugin's own command.json)
                    string rootJson = Path.Combine(baseDir, "command.json");
                    if (File.Exists(rootJson) && !loaded)
                    {
                        var data = JsonConvert.DeserializeObject<CommandJsonModel>(
                            File.ReadAllText(rootJson));
                        if (data?.Commands != null)
                        {
                            var cs = new CommandSetInfo
                            {
                                Name        = data.Name ?? "AutoCADMCPCommandSet",
                                Description = data.Description ?? "Built-in AutoCAD MCP commands",
                                Commands    = new List<CommandConfig>()
                            };
                            foreach (var item in data.Commands)
                            {
                                bool enabled = savedState.TryGetValue(item.CommandName, out bool s) ? s : true;
                                cs.Commands.Add(new CommandConfig
                                {
                                    CommandName  = item.CommandName,
                                    Description  = item.Description ?? "",
                                    AssemblyPath = item.AssemblyPath ?? "",
                                    Enabled      = enabled
                                });
                            }
                            if (cs.Commands.Any())
                            {
                                _commandSets.Add(cs);
                                loaded = true;
                            }
                        }
                    }

                    if (loaded) break;
                }

                if (_commandSets.Count == 0)
                {
                    NoSelectionTextBlock.Text = "No command sets found.\n\nExpected: command.json next to the plugin DLL\nor in the Commands sub-folder.";
                    NoSelectionTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading command sets: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Selection ─────────────────────────────────────────────────────────

        private void CommandSetListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UnsubscribeEvents();
            _currentCommands.Clear();
            var selected = CommandSetListBox.SelectedItem as CommandSetInfo;
            if (selected != null)
            {
                NoSelectionTextBlock.Visibility   = Visibility.Collapsed;
                FeaturesHeaderTextBlock.Text      = selected.Name;
                foreach (var cmd in selected.Commands)
                {
                    _currentCommands.Add(cmd);
                    cmd.PropertyChanged += Cmd_PropertyChanged;
                }
                ApplySearchFilter();
                UpdateEnabledCount();
            }
            else
            {
                NoSelectionTextBlock.Visibility = Visibility.Visible;
                FeaturesHeaderTextBlock.Text    = "Commands";
                EnabledCountTextBlock.Text      = "";
            }
        }

        private void UnsubscribeEvents()
        {
            foreach (var cmd in _currentCommands.OfType<CommandConfig>())
                cmd.PropertyChanged -= Cmd_PropertyChanged;
        }

        private void Cmd_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CommandConfig.Enabled))
                UpdateEnabledCount();
        }

        // ── Button handlers ───────────────────────────────────────────────────

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            int idx = CommandSetListBox.SelectedIndex;
            LoadCommandSets();
            if (idx >= 0 && idx < _commandSets.Count)
                CommandSetListBox.SelectedIndex = idx;
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cmd in _currentCommands) cmd.Enabled = true;
            FeaturesListView.Items.Refresh();
            UpdateEnabledCount();
        }

        private void UnselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cmd in _currentCommands) cmd.Enabled = false;
            FeaturesListView.Items.Refresh();
            UpdateEnabledCount();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string registryPath = PathManager.GetCommandRegistryFilePath();
                string dir = Path.GetDirectoryName(registryPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var registry = new CommandRegistryModel { Commands = new List<CommandConfig>() };
                foreach (var cs in _commandSets)
                    foreach (var cmd in cs.Commands)
                        if (cmd.Enabled)
                            registry.Commands.Add(new CommandConfig
                            {
                                CommandName  = cmd.CommandName,
                                AssemblyPath = cmd.AssemblyPath,
                                Description  = cmd.Description,
                                Enabled      = true
                            });

                File.WriteAllText(registryPath,
                    JsonConvert.SerializeObject(registry, Formatting.Indented));

                MessageBox.Show($"Saved {registry.Commands.Count} enabled command(s).\n\nRestart AutoCAD or run MCPSTART to apply.",
                    "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dir = PathManager.GetPluginDirectoryPath();
                if (Directory.Exists(dir))
                    Process.Start("explorer.exe", dir);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening folder: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // ── Helper models (scoped to this file) ──────────────────────────────────

    public class CommandSetInfo
    {
        public string              Name        { get; set; }
        public string              Description { get; set; }
        public List<CommandConfig> Commands    { get; set; } = new List<CommandConfig>();
    }

    public class CommandRegistryModel
    {
        [JsonProperty("commands")]
        public List<CommandConfig> Commands { get; set; } = new List<CommandConfig>();
    }

    public class CommandJsonModel
    {
        [JsonProperty("name")]        public string               Name        { get; set; }
        [JsonProperty("description")] public string               Description { get; set; }
        [JsonProperty("commands")]    public List<CommandItemModel> Commands  { get; set; } = new List<CommandItemModel>();
    }

    public class CommandItemModel
    {
        [JsonProperty("commandName")]  public string CommandName  { get; set; }
        [JsonProperty("description")]  public string Description  { get; set; }
        [JsonProperty("assemblyPath")] public string AssemblyPath { get; set; }
    }
}
