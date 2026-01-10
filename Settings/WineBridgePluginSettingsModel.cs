using System.Collections.Generic;
using System.Windows.Input;
using Playnite.SDK;
using Playnite.SDK.Data;
using WineBridgePlugin.Models;
using WineBridgePlugin.Patchers;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin
{
    public class WineBridgePluginSettingsModel : ObservableObject
    {
        private string _trackingDirectoryLinux;
        private bool _setScriptExecutePermissions = true;
        private bool _redirectExplorerCallsToLinux = true;
        private bool _redirectProtocolCallsToLinux = true;

        private bool _steamIntegrationEnabled;
        private string _steamDataPathLinux;
        private string _steamExecutablePathLinux;

        private bool _heroicGogIntegrationEnabled;
        private bool _heroicAmazonIntegrationEnabled;
        private bool _heroicEpicIntegrationEnabled;
        private string _heroicDataPathLinux;
        private string _heroicExecutablePathLinux;

        private bool _debugLoggingEnabled;

        public string TrackingDirectoryLinux
        {
            get => _trackingDirectoryLinux;
            set => SetValue(ref _trackingDirectoryLinux, value);
        }

        public bool SetScriptExecutePermissions
        {
            get => _setScriptExecutePermissions;
            set => SetValue(ref _setScriptExecutePermissions, value);
        }

        public bool RedirectExplorerCallsToLinux
        {
            get => _redirectExplorerCallsToLinux;
            set => SetValue(ref _redirectExplorerCallsToLinux, value);
        }

        public bool RedirectProtocolCallsToLinux
        {
            get => _redirectProtocolCallsToLinux;
            set => SetValue(ref _redirectProtocolCallsToLinux, value);
        }

        public bool SteamIntegrationEnabled
        {
            get => _steamIntegrationEnabled;
            set => SetValue(ref _steamIntegrationEnabled, value);
        }

        public string SteamDataPathLinux
        {
            get => _steamDataPathLinux;
            set => SetValue(ref _steamDataPathLinux, value);
        }

        public string SteamExecutablePathLinux
        {
            get => _steamExecutablePathLinux;
            set => SetValue(ref _steamExecutablePathLinux, value);
        }


        public bool HeroicGogIntegrationEnabled
        {
            get => _heroicGogIntegrationEnabled;
            set => SetValue(ref _heroicGogIntegrationEnabled, value);
        }

        public bool HeroicAmazonIntegrationEnabled
        {
            get => _heroicAmazonIntegrationEnabled;
            set => SetValue(ref _heroicAmazonIntegrationEnabled, value);
        }

        public bool HeroicEpicIntegrationEnabled
        {
            get => _heroicEpicIntegrationEnabled;
            set => SetValue(ref _heroicEpicIntegrationEnabled, value);
        }

        public string HeroicDataPathLinux
        {
            get => _heroicDataPathLinux;
            set => SetValue(ref _heroicDataPathLinux, value);
        }

        public string HeroicExecutablePathLinux
        {
            get => _heroicExecutablePathLinux;
            set => SetValue(ref _heroicExecutablePathLinux, value);
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
        public string GogPatchingState { get; set; }
        public string AmazonPatchingState { get; set; }
        public string EpicPatchingState { get; set; }
    }

    public class WineBridgePluginSettingsViewModel : ObservableObject, ISettings
    {
        private readonly WineBridgePlugin _plugin;
        private WineBridgePluginSettingsModel EditingClone { get; set; }

        private WineBridgePluginSettingsModel _settings;

        public WineBridgePluginSettingsModel Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged();
            }
        }

        public PatchingStatuses PatchingStatuses { get; set; }
        public ICommand AutoDetectSteam { get; private set; }
        public ICommand AutoDetectHeroic { get; private set; }

        public WineBridgePluginSettingsViewModel(WineBridgePlugin plugin)
        {
            _plugin = plugin;

            var savedSettings = plugin.LoadPluginSettings<WineBridgePluginSettingsModel>();

            Settings = savedSettings ?? new WineBridgePluginSettingsModel();
            FillSettingsWithDefaults();

            AutoDetectSteam = new RelayCommand(DoAutoDetectSteam);
            AutoDetectHeroic = new RelayCommand(DoAutoDetectHeroic);
        }

        private void FillSettingsWithDefaults()
        {
            if (Settings.TrackingDirectoryLinux == null)
            {
                Settings.TrackingDirectoryLinux = "/tmp";
            }

            if (Settings.SteamDataPathLinux == null || Settings.SteamExecutablePathLinux == null)
            {
                var foundConfiguration = DefaultSettingFinder.SteamConfiguration;
                if (foundConfiguration != null)
                {
                    Settings.SteamDataPathLinux = foundConfiguration.DataPath;
                    Settings.SteamExecutablePathLinux = foundConfiguration.ExecutablePath;
                }
            }

            if (Settings.HeroicDataPathLinux == null || Settings.HeroicExecutablePathLinux == null)
            {
                var foundConfiguration = DefaultSettingFinder.HeroicConfiguration;
                if (foundConfiguration != null)
                {
                    Settings.HeroicDataPathLinux = foundConfiguration.DataPath;
                    Settings.HeroicExecutablePathLinux = foundConfiguration.ExecutablePath;
                }
            }
        }

        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
            PatchingStatuses = new PatchingStatuses
            {
                SystemPatchingState = TranslatePatchingState(SystemPatcher.State),
                PlaynitePatchingState = TranslatePatchingState(PlaynitePatcher.State),
                SteamPatchingState = TranslatePatchingState(SteamPatcher.State),
                GogPatchingState = TranslatePatchingState(GogPatcher.State),
                AmazonPatchingState = TranslatePatchingState(AmazonPatcher.State),
                EpicPatchingState = TranslatePatchingState(EpicPatcher.State)
            };
        }

        private static string TranslatePatchingState(PatchingState state)
        {
            return ResourceProvider.GetString($"LOC_Yalgrin_WineBridge_PatchingState_{state}");
        }

        private void DoAutoDetectSteam()
        {
            var foundConfiguration = DefaultSettingFinder.SteamConfiguration;
            Settings.SteamDataPathLinux = foundConfiguration.DataPath;
            Settings.SteamExecutablePathLinux = foundConfiguration.ExecutablePath;

            switch (foundConfiguration.Type)
            {
                case "Native":
                    _plugin.PlayniteApi.Dialogs.ShowMessage(string.Format(
                        ResourceProvider.GetString("LOC_Yalgrin_WineBridge_Messages_FoundNativeConfiguration"),
                        foundConfiguration.DataPath));
                    break;
                case "Flatpak":
                    _plugin.PlayniteApi.Dialogs.ShowMessage(string.Format(
                        ResourceProvider.GetString("LOC_Yalgrin_WineBridge_Messages_FoundFlatpakConfiguration"),
                        foundConfiguration.DataPath));
                    break;
                default:
                    _plugin.PlayniteApi.Dialogs.ShowErrorMessage(string.Format(
                        ResourceProvider.GetString("LOC_Yalgrin_WineBridge_Messages_ConfigurationNotFoundPlaceholder"),
                        foundConfiguration.DataPath));
                    break;
            }
        }

        private void DoAutoDetectHeroic()
        {
            var foundConfiguration = DefaultSettingFinder.HeroicConfiguration;
            Settings.HeroicDataPathLinux = foundConfiguration.DataPath;
            Settings.HeroicExecutablePathLinux = foundConfiguration.ExecutablePath;

            switch (foundConfiguration.Type)
            {
                case "Native":
                    _plugin.PlayniteApi.Dialogs.ShowMessage(string.Format(
                        ResourceProvider.GetString("LOC_Yalgrin_WineBridge_Messages_FoundNativeConfiguration"),
                        foundConfiguration.DataPath));
                    break;
                case "Flatpak":
                    _plugin.PlayniteApi.Dialogs.ShowMessage(string.Format(
                        ResourceProvider.GetString("LOC_Yalgrin_WineBridge_Messages_FoundFlatpakConfiguration"),
                        foundConfiguration.DataPath));
                    break;
                default:
                    _plugin.PlayniteApi.Dialogs.ShowErrorMessage(string.Format(
                        ResourceProvider.GetString("LOC_Yalgrin_WineBridge_Messages_ConfigurationNotFoundPlaceholder"),
                        foundConfiguration.DataPath));
                    break;
            }
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