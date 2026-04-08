using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Playnite;

namespace WineBridgePlugin;

public partial class WineBridgePluginPluginSettings : ObservableObject
{
    [ObservableProperty] private string? property = "test value";
}

public partial class WineBridgePluginSettingsHandler : PluginSettingsHandler
{
    private readonly WineBridgePluginPlugin plugin;

    public WineBridgePluginPluginSettings Settings { get; } = new();

    public WineBridgePluginSettingsHandler(WineBridgePluginPlugin plugin)
    {
        this.plugin = plugin;

        // This is where you would also normally load your saved settings.
        // This is up to you how you save and load those.
        // Main recommendation is that you should be using PlayniteApi.UserDataDir to store that data.
        var settingsFile = Path.Combine(WineBridgePluginPlugin.PlayniteApi.UserDataDir, "settings.something");
    }

    public override FrameworkElement GetEditView(GetSettingsViewArgs args)
    {
        return new WineBridgePluginSettingsView { DataContext = this };
    }

    public override async Task BeginEditAsync(BeginEditArgs args)
    {
        // This gets called when settings class is loaded and editing is started.
    }

    public override async Task CancelEditAsync(CancelEditArgs args)
    {
        // This gets called when a user decides to close the view and cancel any unsaved changes.
    }

    public override async Task EndEditAsync(EndEditArgs args)
    {
        // This is called when a user decides to close the view and save changes made to the settings.
        // This is where you would save your settings to a file somewhere in PlayniteApi.UserDataDir
    }

    public override async Task<ICollection<string>> VerifySettingsAsync(VerifySettingsArgs args)
    {
        // This is executed when saving changes. You can do verification on current state
        // and if you detect some incorrect settings, you can report it here to the user.
        return [];
    }
}