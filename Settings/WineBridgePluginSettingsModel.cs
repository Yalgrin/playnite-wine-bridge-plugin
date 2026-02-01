using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Playnite.SDK;
using Playnite.SDK.Data;
using WineBridgePlugin.Models;
using WineBridgePlugin.Patchers;
using WineBridgePlugin.Utils;

namespace WineBridgePlugin.Settings
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

        private bool _lutrisGogIntegrationEnabled;
        private bool _lutrisAmazonIntegrationEnabled;
        private bool _lutrisEpicIntegrationEnabled;
        private bool _lutrisEaIntegrationEnabled;
        private bool _lutrisBattleNetIntegrationEnabled;
        private bool _lutrisItchIoIntegrationEnabled;
        private string _lutrisDataPathLinux;
        private string _lutrisExecutablePathLinux;

        private ObservableCollection<WineBridgeEmulatorConfig> _emulatorConfigs;

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

        public bool LutrisGogIntegrationEnabled
        {
            get => _lutrisGogIntegrationEnabled;
            set => SetValue(ref _lutrisGogIntegrationEnabled, value);
        }

        public bool LutrisAmazonIntegrationEnabled
        {
            get => _lutrisAmazonIntegrationEnabled;
            set => SetValue(ref _lutrisAmazonIntegrationEnabled, value);
        }

        public bool LutrisEpicIntegrationEnabled
        {
            get => _lutrisEpicIntegrationEnabled;
            set => SetValue(ref _lutrisEpicIntegrationEnabled, value);
        }

        public bool LutrisEaIntegrationEnabled
        {
            get => _lutrisEaIntegrationEnabled;
            set => SetValue(ref _lutrisEaIntegrationEnabled, value);
        }

        public bool LutrisBattleNetIntegrationEnabled
        {
            get => _lutrisBattleNetIntegrationEnabled;
            set => SetValue(ref _lutrisBattleNetIntegrationEnabled, value);
        }

        public bool LutrisItchIoIntegrationEnabled
        {
            get => _lutrisItchIoIntegrationEnabled;
            set => SetValue(ref _lutrisItchIoIntegrationEnabled, value);
        }

        public string LutrisDataPathLinux
        {
            get => _lutrisDataPathLinux;
            set => SetValue(ref _lutrisDataPathLinux, value);
        }

        public string LutrisExecutablePathLinux
        {
            get => _lutrisExecutablePathLinux;
            set => SetValue(ref _lutrisExecutablePathLinux, value);
        }

        public ObservableCollection<WineBridgeEmulatorConfig> EmulatorConfigs
        {
            get => _emulatorConfigs;
            set => SetValue(ref _emulatorConfigs, value);
        }

        public bool DebugLoggingEnabled
        {
            get => _debugLoggingEnabled;
            set => SetValue(ref _debugLoggingEnabled, value);
        }
    }

    public class WineBridgeEmulatorConfig : ObservableObject
    {
        private string _emulatorId;
        private string _linuxPath;

        public string EmulatorId
        {
            get => _emulatorId;
            set => SetValue(ref _emulatorId, value);
        }

        public string LinuxPath
        {
            get => _linuxPath;
            set => SetValue(ref _linuxPath, value);
        }
    }

    public class EmulatorDescriptor
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class PatchingStatuses
    {
        public string SystemPatchingState { get; set; }
        public string PlaynitePatchingState { get; set; }
        public string SteamPatchingState { get; set; }
        public string GogPatchingState { get; set; }
        public string AmazonPatchingState { get; set; }
        public string EpicPatchingState { get; set; }
        public string EaPatchingState { get; set; }
        public string BattleNetPatchingState { get; set; }
        public string ItchIoPatchingState { get; set; }
    }

    public class WineBridgePluginSettingsViewModel : ObservableObject, ISettings
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

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

        public List<EmulatorDescriptor> EmulatorDescriptors { get; private set; }

        public ICommand AutoDetectSteam { get; private set; }
        public ICommand AutoDetectHeroic { get; private set; }
        public ICommand AutoDetectLutris { get; private set; }
        public ICommand AddEmulatorConfig { get; private set; }
        public ICommand RemoveEmulatorConfig { get; private set; }

        public WineBridgePluginSettingsViewModel(WineBridgePlugin plugin)
        {
            _plugin = plugin;

            var savedSettings = plugin.LoadPluginSettings<WineBridgePluginSettingsModel>();

            Settings = savedSettings ?? new WineBridgePluginSettingsModel();
            try
            {
                FillSettingsWithDefaults();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to fill settings with default values!");
            }

            AutoDetectSteam = new RelayCommand(DoAutoDetectSteam);
            AutoDetectHeroic = new RelayCommand(DoAutoDetectHeroic);
            AutoDetectLutris = new RelayCommand(DoAutoDetectLutris);
            AddEmulatorConfig = new RelayCommand(() =>
            {
                Logger.Debug("Adding a new emulator config...");
                if (Settings.EmulatorConfigs == null)
                {
                    Settings.EmulatorConfigs = new ObservableCollection<WineBridgeEmulatorConfig>();
                }

                Settings.EmulatorConfigs.Add(new WineBridgeEmulatorConfig());
            });
            RemoveEmulatorConfig = new RelayCommand<WineBridgeEmulatorConfig>((emulatorConfig) =>
            {
                Logger.Debug($"Removing emulator config: {emulatorConfig.EmulatorId}");
                Settings.EmulatorConfigs.Remove(emulatorConfig);
            });
            EmulatorDescriptors = _plugin.PlayniteApi.Emulation.Emulators.Select(e => new EmulatorDescriptor
            {
                Id = e.Id,
                Name = e.Name
            }).OrderBy(e => e.Name).ToList();
        }

        private void FillSettingsWithDefaults()
        {
            if (!WineDetector.IsRunningUnderWine())
            {
                Logger.Warn("Not running under Wine, skipping default settings filling.");
                return;
            }

            if (Settings.EmulatorConfigs == null)
            {
                Settings.EmulatorConfigs = new ObservableCollection<WineBridgeEmulatorConfig>();
            }

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

            if (Settings.LutrisDataPathLinux == null || Settings.LutrisExecutablePathLinux == null)
            {
                var foundConfiguration = DefaultSettingFinder.LutrisConfiguration;
                if (foundConfiguration != null)
                {
                    Settings.LutrisDataPathLinux = foundConfiguration.DataPath;
                    Settings.LutrisExecutablePathLinux = foundConfiguration.ExecutablePath;
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
                EpicPatchingState = TranslatePatchingState(EpicPatcher.State),
                EaPatchingState = TranslatePatchingState(EaPatcher.State),
                BattleNetPatchingState = TranslatePatchingState(BattleNetPatcher.State),
                ItchIoPatchingState = TranslatePatchingState(ItchIoPatcher.State)
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

        private void DoAutoDetectLutris()
        {
            var foundConfiguration = DefaultSettingFinder.LutrisConfiguration;
            Settings.LutrisDataPathLinux = foundConfiguration.DataPath;
            Settings.LutrisExecutablePathLinux = foundConfiguration.ExecutablePath;

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
            Settings.EmulatorConfigs =
                new ObservableCollection<WineBridgeEmulatorConfig>(Settings.EmulatorConfigs.OrderBy(e => e.EmulatorId));
            _plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            if (HasDuplicateConfigurations())
            {
                errors.Add(ResourceProvider.GetString(
                    "LOC_Yalgrin_WineBridge_Settings_Emulators_DuplicatedConfiguration"));
            }

            if (HasIncompleteConfigurations())
            {
                errors.Add(ResourceProvider.GetString(
                    "LOC_Yalgrin_WineBridge_Settings_Emulators_IncompleteConfiguration"));
            }

            return errors.Count == 0;
        }

        private bool HasDuplicateConfigurations()
        {
            return Settings.EmulatorConfigs?.Select(c => c.EmulatorId).GroupBy(id => id).Any(g => g.Count() > 1) ??
                   false;
        }

        private bool HasIncompleteConfigurations()
        {
            return Settings.EmulatorConfigs?.Any(c =>
                string.IsNullOrEmpty(c.EmulatorId) || string.IsNullOrEmpty(c.LinuxPath)) ?? false;
        }
    }
}