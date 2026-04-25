using Playnite;
using SteamKit2;
using WineBridgePlugin.Processes;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin.Integrations.Steam;

public class SteamPlayController(Game game)
    : PlayController("steam_play_primary", $"Start {game.Name} via Steam")
{
    private CancellationTokenSource? _watcherToken;

    public override async ValueTask DisposeAsync()
    {
        if (_watcherToken != null)
        {
            await _watcherToken.CancelAsync();
            _watcherToken.Dispose();
        }
    }

    public override async Task PlayAsync(PlayActionArgs args)
    {
        var executablePath = WineBridgeSettings.LutrisExecutablePathLinux;
        if (executablePath == null)
        {
            throw new Exception("Lutris installation path not set.");
        }

        _watcherToken = new CancellationTokenSource();

        var gameId = game.LibraryGameId!.ToSteamGameID();
        //TODO get whether to show dialog from settings
        var process = SteamProcessStarter.Start(game.LibraryGameId!, !gameId.IsMod && !gameId.IsShortcut);

        _ = LinuxProcessMonitor.TrackLinuxProcess(this, process, _watcherToken);

        await Task.CompletedTask;
    }
}

public static class SteamLauncher
{
    public static GameID ToSteamGameID(this string gameId)
    {
        return new GameID(ulong.Parse(gameId));
    }
}