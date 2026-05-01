using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Playnite;
using WineBridgePlugin.Models;
using WineBridgePlugin.Patchers;
using WineBridgePlugin.Settings;
using WineBridgePlugin.Utils;
using MessageBoxResult = Playnite.MessageBoxResult;

namespace WineBridgePlugin;

public partial class WineBridgePluginPluginSettings : ObservableObject
{
    [ObservableProperty] private string? property = "test value";
}

public partial class WineBridgePluginSettingsModel : ObservableObject
{
    [ObservableProperty] private string? trackingDirectoryLinux;
    [ObservableProperty] private bool setScriptExecutePermissions = true;
    [ObservableProperty] private bool redirectExplorerCallsToLinux = true;
    [ObservableProperty] private bool redirectProtocolCallsToLinux = true;
    [ObservableProperty] private bool redirectFileDirectorySelectionCallsToLinux = true;
    [ObservableProperty] private string? fileDirectorySelectionProgram = "auto";
    [ObservableProperty] private bool forceHighQualityIcons;
    [ObservableProperty] private bool advancedProcessIntegration;

    [ObservableProperty] private bool steamIntegrationEnabled;
    [ObservableProperty] private string? steamDataPathLinux;
    [ObservableProperty] private string? steamExecutablePathLinux;

    [ObservableProperty] private bool heroicGogIntegrationEnabled;
    [ObservableProperty] private bool heroicAmazonIntegrationEnabled;
    [ObservableProperty] private bool heroicEpicIntegrationEnabled;
    [ObservableProperty] private string? heroicDataPathLinux;
    [ObservableProperty] private string? heroicExecutablePathLinux;

    [ObservableProperty] private bool lutrisGogIntegrationEnabled;
    [ObservableProperty] private bool lutrisAmazonIntegrationEnabled;
    [ObservableProperty] private bool lutrisEpicIntegrationEnabled;
    [ObservableProperty] private bool lutrisEaIntegrationEnabled;
    [ObservableProperty] private bool lutrisBattleNetIntegrationEnabled;
    [ObservableProperty] private bool lutrisItchIoIntegrationEnabled;
    [ObservableProperty] private string? lutrisDataPathLinux;
    [ObservableProperty] private string? lutrisExecutablePathLinux;

    [ObservableProperty] private bool itchIoIntegrationEnabled;
    [ObservableProperty] private string? itchIoDataPathLinux;
    [ObservableProperty] private string? itchIoExecutablePathLinux;

    [ObservableProperty] private ObservableCollection<WineBridgeEmulatorConfig>? emulatorConfigs;

    [ObservableProperty] private bool debugLoggingEnabled;

    public WineBridgePluginSettingsModel GetClone()
    {
        return new WineBridgePluginSettingsModel
        {
            TrackingDirectoryLinux = TrackingDirectoryLinux,
            SetScriptExecutePermissions = SetScriptExecutePermissions,
            RedirectExplorerCallsToLinux = RedirectExplorerCallsToLinux,
            RedirectProtocolCallsToLinux = RedirectProtocolCallsToLinux,
            RedirectFileDirectorySelectionCallsToLinux = RedirectFileDirectorySelectionCallsToLinux,
            FileDirectorySelectionProgram = FileDirectorySelectionProgram,
            ForceHighQualityIcons = ForceHighQualityIcons,
            AdvancedProcessIntegration = AdvancedProcessIntegration,
            SteamIntegrationEnabled = SteamIntegrationEnabled,
            SteamDataPathLinux = SteamDataPathLinux,
            SteamExecutablePathLinux = SteamExecutablePathLinux,
            HeroicGogIntegrationEnabled = HeroicGogIntegrationEnabled,
            HeroicAmazonIntegrationEnabled = HeroicAmazonIntegrationEnabled,
            HeroicEpicIntegrationEnabled = HeroicEpicIntegrationEnabled,
            HeroicDataPathLinux = HeroicDataPathLinux,
            HeroicExecutablePathLinux = HeroicExecutablePathLinux,
            LutrisGogIntegrationEnabled = LutrisGogIntegrationEnabled,
            LutrisAmazonIntegrationEnabled = LutrisAmazonIntegrationEnabled,
            LutrisEpicIntegrationEnabled = LutrisEpicIntegrationEnabled,
            LutrisEaIntegrationEnabled = LutrisEaIntegrationEnabled,
            LutrisBattleNetIntegrationEnabled = LutrisBattleNetIntegrationEnabled,
            LutrisItchIoIntegrationEnabled = LutrisItchIoIntegrationEnabled,
            LutrisDataPathLinux = LutrisDataPathLinux,
            LutrisExecutablePathLinux = LutrisExecutablePathLinux,
            ItchIoIntegrationEnabled = ItchIoIntegrationEnabled,
            ItchIoDataPathLinux = ItchIoDataPathLinux,
            ItchIoExecutablePathLinux = ItchIoExecutablePathLinux,
            EmulatorConfigs = EmulatorConfigs != null
                ? new ObservableCollection<WineBridgeEmulatorConfig>(
                    EmulatorConfigs.Select(config => new WineBridgeEmulatorConfig
                    {
                        EmulatorId = config.EmulatorId,
                        LinuxPath = config.LinuxPath
                    }))
                : null,
            DebugLoggingEnabled = DebugLoggingEnabled
        };
    }
}

public partial class WineBridgeEmulatorConfig : ObservableObject
{
    [ObservableProperty] private string? emulatorId;
    [ObservableProperty] private string? linuxPath;
}

public class EmulatorDescriptor
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

public class PatchingStatuses
{
    public string? SystemPatchingState { get; set; }
    public string? PlaynitePatchingState { get; set; }
    public string? SteamPatchingState { get; set; }
    public string? GogPatchingState { get; set; }
    public string? AmazonPatchingState { get; set; }
    public string? EpicPatchingState { get; set; }
    public string? EaPatchingState { get; set; }
    public string? BattleNetPatchingState { get; set; }
    public string? ItchIoPatchingState { get; set; }
}

public partial class WineBridgePluginSettingsHandler : PluginSettingsHandler
{
    private static readonly ILogger Logger = LogManager.GetLogger();

    private readonly WineBridgePlugin _plugin;
    private WineBridgePluginSettingsModel EditingClone { get; set; }

    public WineBridgePluginSettingsModel Settings { get; set; }

    public PatchingStatuses PatchingStatuses { get; set; }

    public List<EmulatorDescriptor> EmulatorDescriptors { get; private set; }

    public List<string> FileDirectorySelectorPrograms => WineUtils.FileDirectorySelectorPrograms;

    public ICommand AutoDetectSteam { get; private set; }
    public ICommand AutoDetectHeroic { get; private set; }
    public ICommand AutoDetectLutris { get; private set; }
    public ICommand AutoDetectItchIo { get; private set; }
    public ICommand AddEmulatorConfig { get; private set; }
    public ICommand RemoveEmulatorConfig { get; private set; }

    public WineBridgePluginSettingsHandler(WineBridgePlugin plugin)
    {
        this._plugin = plugin;

        var settingsFile = Path.Combine(WineBridgePlugin.PlayniteApi.UserDataDir, "settings.json");
        var savedSettings = File.Exists(settingsFile)
            ? JsonConvert.DeserializeObject<WineBridgePluginSettingsModel>(File.ReadAllText(settingsFile))
            : null;

        Settings = savedSettings ?? new WineBridgePluginSettingsModel();
        try
        {
            FillSettingsWithDefaults();
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to fill settings with default values!");
        }

        AutoDetectSteam = new AsyncRelayCommand(DoAutoDetectSteam);
        AutoDetectHeroic = new AsyncRelayCommand(DoAutoDetectHeroic);
        AutoDetectLutris = new AsyncRelayCommand(DoAutoDetectLutris);
        AutoDetectItchIo = new AsyncRelayCommand(DoAutoDetectItchIo);
        AddEmulatorConfig = new RelayCommand(() =>
        {
            Logger.Debug("Adding a new emulator config...");
            Settings.EmulatorConfigs ??= [];
            Settings.EmulatorConfigs.Add(new WineBridgeEmulatorConfig());
        });

        RemoveEmulatorConfig = new RelayCommand<WineBridgeEmulatorConfig>(emulatorConfig =>
        {
            if (emulatorConfig == null)
            {
                return;
            }

            Logger.Debug($"Removing emulator config: {emulatorConfig.EmulatorId}");
            Settings.EmulatorConfigs?.Remove(emulatorConfig);
        });
        EmulatorDescriptors = [];
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

        if (Settings.ItchIoDataPathLinux == null || Settings.ItchIoExecutablePathLinux == null)
        {
            var foundConfiguration = DefaultSettingFinder.ItchIoConfiguration;
            if (foundConfiguration != null)
            {
                Settings.ItchIoDataPathLinux = foundConfiguration.DataPath;
                Settings.ItchIoExecutablePathLinux = foundConfiguration.ExecutablePath;
            }
        }
    }

    private static string TranslatePatchingState(PatchingState state)
    {
        return WineBridgePlugin.PlayniteApi.GetLocalizedString($"LOC_Yalgrin_WineBridge_PatchingState_{state}");
    }

    private async Task DoAutoDetectSteam()
    {
        var foundConfiguration = DefaultSettingFinder.SteamConfiguration;

        if (Settings.SteamDataPathLinux == foundConfiguration.DataPath &&
            Settings.SteamExecutablePathLinux == foundConfiguration.ExecutablePath)
        {
            await WineBridgePlugin.PlayniteApi.Dialogs.ShowMessageAsync(
                WineBridgePlugin.PlayniteApi.GetLocalizedString(
                    "LOC_Yalgrin_WineBridge_Messages_DetectedTheSameConfiguration"),
                "",
                MessageBoxButtons.OK, MessageBoxSeverity.Information);
            return;
        }

        var messageBoxResult = await WineBridgePlugin.PlayniteApi.Dialogs.ShowMessageAsync(string.Format(
                WineBridgePlugin.PlayniteApi.GetLocalizedString(
                    "LOC_Yalgrin_WineBridge_Messages_DoYouWantToReplaceConfig"),
                GetFoundConfigurationTypeMsg(foundConfiguration.Type),
                foundConfiguration.DataPath,
                foundConfiguration.ExecutablePath,
                Settings.SteamDataPathLinux,
                Settings.SteamExecutablePathLinux),
            WineBridgePlugin.PlayniteApi.GetLocalizedString(
                "LOC_Yalgrin_WineBridge_Messages_DoYouWantToReplaceConfig_Caption"),
            MessageBoxButtons.YesNo,
            MessageBoxSeverity.Question);
        if (messageBoxResult != MessageBoxResult.Yes)
        {
            return;
        }

        Settings.SteamDataPathLinux = foundConfiguration.DataPath;
        Settings.SteamExecutablePathLinux = foundConfiguration.ExecutablePath;
    }

    private async Task DoAutoDetectHeroic()
    {
        var foundConfiguration = DefaultSettingFinder.HeroicConfiguration;

        if (Settings.HeroicDataPathLinux == foundConfiguration.DataPath &&
            Settings.HeroicExecutablePathLinux == foundConfiguration.ExecutablePath)
        {
            await WineBridgePlugin.PlayniteApi.Dialogs.ShowMessageAsync(
                WineBridgePlugin.PlayniteApi.GetLocalizedString(
                    "LOC_Yalgrin_WineBridge_Messages_DetectedTheSameConfiguration"), "",
                MessageBoxButtons.OK, MessageBoxSeverity.Information);
            return;
        }

        var messageBoxResult = await WineBridgePlugin.PlayniteApi.Dialogs.ShowMessageAsync(string.Format(
                WineBridgePlugin.PlayniteApi.GetLocalizedString(
                    "LOC_Yalgrin_WineBridge_Messages_DoYouWantToReplaceConfig"),
                GetFoundConfigurationTypeMsg(foundConfiguration.Type),
                foundConfiguration.DataPath,
                foundConfiguration.ExecutablePath,
                Settings.HeroicDataPathLinux,
                Settings.HeroicExecutablePathLinux),
            WineBridgePlugin.PlayniteApi.GetLocalizedString(
                "LOC_Yalgrin_WineBridge_Messages_DoYouWantToReplaceConfig_Caption"),
            MessageBoxButtons.YesNo,
            MessageBoxSeverity.Question);
        if (messageBoxResult != MessageBoxResult.Yes)
        {
            return;
        }

        Settings.HeroicDataPathLinux = foundConfiguration.DataPath;
        Settings.HeroicExecutablePathLinux = foundConfiguration.ExecutablePath;
    }

    private async Task DoAutoDetectLutris()
    {
        var foundConfiguration = DefaultSettingFinder.LutrisConfiguration;

        if (Settings.LutrisDataPathLinux == foundConfiguration.DataPath &&
            Settings.LutrisExecutablePathLinux == foundConfiguration.ExecutablePath)
        {
            await WineBridgePlugin.PlayniteApi.Dialogs.ShowMessageAsync(
                WineBridgePlugin.PlayniteApi.GetLocalizedString(
                    "LOC_Yalgrin_WineBridge_Messages_DetectedTheSameConfiguration"), "",
                MessageBoxButtons.OK, MessageBoxSeverity.Information);
            return;
        }

        var messageBoxResult = await WineBridgePlugin.PlayniteApi.Dialogs.ShowMessageAsync(string.Format(
                WineBridgePlugin.PlayniteApi.GetLocalizedString(
                    "LOC_Yalgrin_WineBridge_Messages_DoYouWantToReplaceConfig"),
                GetFoundConfigurationTypeMsg(foundConfiguration.Type),
                foundConfiguration.DataPath,
                foundConfiguration.ExecutablePath,
                Settings.LutrisDataPathLinux,
                Settings.LutrisExecutablePathLinux),
            WineBridgePlugin.PlayniteApi.GetLocalizedString(
                "LOC_Yalgrin_WineBridge_Messages_DoYouWantToReplaceConfig_Caption"),
            MessageBoxButtons.YesNo,
            MessageBoxSeverity.Question);
        if (messageBoxResult != MessageBoxResult.Yes)
        {
            return;
        }

        Settings.LutrisDataPathLinux = foundConfiguration.DataPath;
        Settings.LutrisExecutablePathLinux = foundConfiguration.ExecutablePath;
    }

    private async Task DoAutoDetectItchIo()
    {
        var foundConfiguration = DefaultSettingFinder.ItchIoConfiguration;

        if (Settings.ItchIoDataPathLinux == foundConfiguration.DataPath &&
            Settings.ItchIoExecutablePathLinux == foundConfiguration.ExecutablePath)
        {
            await WineBridgePlugin.PlayniteApi.Dialogs.ShowMessageAsync(
                WineBridgePlugin.PlayniteApi.GetLocalizedString(
                    "LOC_Yalgrin_WineBridge_Messages_DetectedTheSameConfiguration"), "",
                MessageBoxButtons.OK, MessageBoxSeverity.Information);
            return;
        }

        var messageBoxResult = await WineBridgePlugin.PlayniteApi.Dialogs.ShowMessageAsync(string.Format(
                WineBridgePlugin.PlayniteApi.GetLocalizedString(
                    "LOC_Yalgrin_WineBridge_Messages_DoYouWantToReplaceConfig"),
                GetFoundConfigurationTypeMsg(foundConfiguration.Type),
                foundConfiguration.DataPath,
                foundConfiguration.ExecutablePath,
                Settings.ItchIoDataPathLinux,
                Settings.ItchIoExecutablePathLinux),
            WineBridgePlugin.PlayniteApi.GetLocalizedString(
                "LOC_Yalgrin_WineBridge_Messages_DoYouWantToReplaceConfig_Caption"),
            MessageBoxButtons.YesNo,
            MessageBoxSeverity.Question);
        if (messageBoxResult != MessageBoxResult.Yes)
        {
            return;
        }

        Settings.ItchIoDataPathLinux = foundConfiguration.DataPath;
        Settings.ItchIoExecutablePathLinux = foundConfiguration.ExecutablePath;
    }

    private static string GetFoundConfigurationTypeMsg(string type)
    {
        switch (type)
        {
            case "Native":
                return WineBridgePlugin.PlayniteApi.GetLocalizedString(
                    "LOC_Yalgrin_WineBridge_Messages_FoundNativeConfiguration");
            case "Flatpak":
                return WineBridgePlugin.PlayniteApi.GetLocalizedString(
                    "LOC_Yalgrin_WineBridge_Messages_FoundFlatpakConfiguration");
            default:
                return WineBridgePlugin.PlayniteApi.GetLocalizedString(
                    "LOC_Yalgrin_WineBridge_Messages_ConfigurationNotFoundPlaceholder");
        }
    }

    public override FrameworkElement GetEditView(GetSettingsViewArgs args)
    {
        return new WineBridgePluginSettingsView { DataContext = this };
    }

    public override async Task BeginEditAsync(BeginEditArgs args)
    {
        EditingClone = Settings.GetClone();
        PatchingStatuses = new PatchingStatuses
        {
            SystemPatchingState = TranslatePatchingState(SystemPatcher.State),
            PlaynitePatchingState = TranslatePatchingState(PlaynitePatcher.State),
            SteamPatchingState = TranslatePatchingState(SteamPatcher.State),
            // GogPatchingState = TranslatePatchingState(GogPatcher.State),
            // AmazonPatchingState = TranslatePatchingState(AmazonPatcher.State),
            // EpicPatchingState = TranslatePatchingState(EpicPatcher.State),
            // EaPatchingState = TranslatePatchingState(EaPatcher.State),
            // BattleNetPatchingState = TranslatePatchingState(BattleNetPatcher.State),
            // ItchIoPatchingState = TranslatePatchingState(ItchIoPatcher.State)
        };
    }

    public override async Task CancelEditAsync(CancelEditArgs args)
    {
        Settings = EditingClone;
    }

    public override async Task EndEditAsync(EndEditArgs args)
    {
        Settings.EmulatorConfigs =
            new ObservableCollection<WineBridgeEmulatorConfig>(Settings.EmulatorConfigs?.OrderBy(e => e.EmulatorId) ??
                                                               (IEnumerable<WineBridgeEmulatorConfig>)[]);

        var settingsFile = Path.Combine(WineBridgePlugin.PlayniteApi.UserDataDir, "settings.json");
        await File.WriteAllTextAsync(settingsFile, JsonConvert.SerializeObject(Settings, Formatting.Indented));
    }

    public override async Task<ICollection<string>> VerifySettingsAsync(VerifySettingsArgs args)
    {
        var errors = new List<string>();
        if (HasDuplicateConfigurations())
        {
            errors.Add(WineBridgePlugin.PlayniteApi.GetLocalizedString(
                "LOC_Yalgrin_WineBridge_Settings_Emulators_DuplicatedConfiguration"));
        }

        if (HasIncompleteConfigurations())
        {
            errors.Add(WineBridgePlugin.PlayniteApi.GetLocalizedString(
                "LOC_Yalgrin_WineBridge_Settings_Emulators_IncompleteConfiguration"));
        }

        return errors;
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