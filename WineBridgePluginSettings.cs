using System.Collections.Generic;
using Playnite.SDK;
using Playnite.SDK.Data;
using WineBridgePlugin.Models;
using WineBridgePlugin.Patchers;

namespace WineBridgePlugin
{
    public class WineBridgePluginSettings : ObservableObject
    {
        private string _trackingDirectoryWine = @"Z:\tmp";
        private string _trackingDirectoryLinux = "/tmp";

        private bool _steamIntegrationEnabled = true;
        private string _steamInstallationPathWine = @"Z:\home\user\.local\share\Steam";
        private string _steamExecutablePathLinux = "steam";

        private bool _debugLoggingEnabled;

        public string TrackingDirectoryWine
        {
            get => _trackingDirectoryWine;
            set => SetValue(ref _trackingDirectoryWine, value);
        }

        public string TrackingDirectoryLinux
        {
            get => _trackingDirectoryLinux;
            set => SetValue(ref _trackingDirectoryLinux, value);
        }

        public bool SteamIntegrationEnabled
        {
            get => _steamIntegrationEnabled;
            set => SetValue(ref _steamIntegrationEnabled, value);
        }

        public string SteamInstallationPathWine
        {
            get => _steamInstallationPathWine;
            set => SetValue(ref _steamInstallationPathWine, value);
        }

        public string SteamExecutablePathLinux
        {
            get => _steamExecutablePathLinux;
            set => SetValue(ref _steamExecutablePathLinux, value);
        }

        public bool DebugLoggingEnabled
        {
            get => _debugLoggingEnabled;
            set => SetValue(ref _debugLoggingEnabled, value);
        }
    }

    public class PatchingStatuses
    {
        public string SystemPatchingState { get; set; }
        public string PlaynitePatchingState { get; set; }
        public string SteamPatchingState { get; set; }
    }

    public class WineBridgePluginSettingsViewModel : ObservableObject, ISettings
    {
        private readonly WineBridgePlugin _plugin;
        private WineBridgePluginSettings EditingClone { get; set; }

        private WineBridgePluginSettings _settings;

        public WineBridgePluginSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged();
            }
        }

        public PatchingStatuses PatchingStatuses { get; set; }

        public WineBridgePluginSettingsViewModel(WineBridgePlugin plugin)
        {
            _plugin = plugin;

            var savedSettings = plugin.LoadPluginSettings<WineBridgePluginSettings>();

            Settings = savedSettings ?? new WineBridgePluginSettings();
        }

        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
            PatchingStatuses = new PatchingStatuses
            {
                SystemPatchingState = TranslatePatchingState(SystemPatcher.State),
                PlaynitePatchingState = TranslatePatchingState(PlaynitePatcher.State),
                SteamPatchingState = TranslatePatchingState(SteamPatcher.State)
            };
        }

        private static string TranslatePatchingState(PatchingState state)
        {
            return ResourceProvider.GetString($"LOC_Yalgrin_WineBridge_PatchingState_{state}");
        }

        public void CancelEdit()
        {
            Settings = EditingClone;
        }

        public void EndEdit()
        {
            _plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}