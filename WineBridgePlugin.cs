using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using WineBridgePlugin.Integrations.Heroic;
using WineBridgePlugin.Integrations.Lutris;
using WineBridgePlugin.Patchers;
using WineBridgePlugin.Utils;

namespace WineBridgePlugin
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class WineBridgePlugin : GenericPlugin
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        static WineBridgePlugin()
        {
            try
            {
                HarmonyPatcher.Initialize();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to initialize harmony patcher!");
            }
        }

        private WineBridgePluginSettingsViewModel SettingsViewModel { get; set; }
        public static WineBridgePluginSettingsModel Settings { get; private set; }

        public override Guid Id { get; } = Guid.Parse("bf485eaa-9e08-4697-baec-eabfb4c1d36e");

        public WineBridgePlugin(IPlayniteAPI api) : base(api)
        {
            SettingsViewModel = new WineBridgePluginSettingsViewModel(this);
            Settings = SettingsViewModel.Settings;
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };

            HarmonyPatcher.MakeScriptExecutable();
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return SettingsViewModel;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new WineBridgePluginSettingsView();
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var menuSection = ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_SectionName");

            yield return new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_AddSteamLinuxAction"),
                MenuSection = menuSection,
                Action = AddSteamLinuxAction
            };

            yield return new GameMenuItem
            {
                Description =
                    ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_AddSteamNonSteamLinuxAction"),
                MenuSection = menuSection,
                Action = AddSteamNonSteamLinuxAction
            };

            yield return AddSeparator(menuSection);

            yield return new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_AddHeroicAction"),
                MenuSection = menuSection,
                Action = AddHeroicAction
            };

            yield return new GameMenuItem
            {
                Description =
                    ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_AddHeroicCustomAction"),
                MenuSection = menuSection,
                Action = AddHeroicCustomAction
            };

            yield return AddSeparator(menuSection);

            yield return new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_AddLutrisAction"),
                MenuSection = menuSection,
                Action = AddLutrisAction
            };

            yield return new GameMenuItem
            {
                Description =
                    ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_AddLutrisCustomAction"),
                MenuSection = menuSection,
                Action = AddLutrisCustomAction
            };

            yield return AddSeparator(menuSection);

            yield return new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_AddCustomLinuxAction"),
                MenuSection = menuSection,
                Action = AddCustomLinuxAction
            };

            yield return new GameMenuItem
            {
                Description =
                    ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_AddCustomAsyncLinuxAction"),
                MenuSection = menuSection,
                Action = AddCustomAsyncLinuxAction
            };
            yield return AddSeparator(menuSection);

            yield return new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_RemoveAllActions"),
                MenuSection = menuSection,
                Action = RemoveAllActions
            };
        }

        private static GameMenuItem AddSeparator(string menuSection)
        {
            return new GameMenuItem
            {
                MenuSection = menuSection,
                Description = "-"
            };
        }

        private void AddCustomLinuxAction(GameMenuItemActionArgs args)
        {
            foreach (var game in args.Games)
            {
                var mainCaption =
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_AddCustomLinuxAction")}";
                var result = PlayniteApi.Dialogs.SelectString(
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_EnterLinuxCommand")}",
                    mainCaption, "");
                if (!result.Result)
                {
                    return;
                }

                var nameResult = PlayniteApi.Dialogs.SelectString(
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_EnterActionName")}",
                    mainCaption, "Play on Linux");
                if (!nameResult.Result)
                {
                    return;
                }

                AddWineBridgeAction(game, $"[WB] {nameResult.SelectedString}",
                    result.SelectedString, false, "-");
            }
        }

        private void AddCustomAsyncLinuxAction(GameMenuItemActionArgs args)
        {
            foreach (var game in args.Games)
            {
                var mainCaption =
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_AddCustomAsyncLinuxAction")}";
                var commandResult = PlayniteApi.Dialogs.SelectString(
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_EnterLinuxCommand")}",
                    mainCaption, "");
                if (!commandResult.Result)
                {
                    return;
                }

                var processCheckResult = PlayniteApi.Dialogs.SelectString(
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_EnterTrackingExpression")}",
                    mainCaption, "");
                if (!processCheckResult.Result)
                {
                    return;
                }

                var nameResult = PlayniteApi.Dialogs.SelectString(
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_EnterActionName")}",
                    mainCaption, "Play on Linux");
                if (!nameResult.Result)
                {
                    return;
                }

                AddWineBridgeAction(game, $"[WB] {nameResult.SelectedString}",
                    commandResult.SelectedString, true, processCheckResult.SelectedString);
            }
        }

        private void AddSteamLinuxAction(GameMenuItemActionArgs args)
        {
            foreach (var game in args.Games)
            {
                var mainCaption =
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_AddSteamLinuxAction")}";
                var result = PlayniteApi.Dialogs.SelectString(
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_EnterSteamAppId")}",
                    mainCaption, "");
                if (!result.Result)
                {
                    return;
                }

                if (!ulong.TryParse(result.SelectedString, out var steamAppId))
                {
                    PlayniteApi.Dialogs.ShowErrorMessage(
                        ResourceProvider.GetString("LOC_Yalgrin_WineBridge_Error_InvalidNumber"));
                    return;
                }

                var nameResult = PlayniteApi.Dialogs.SelectString(
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_EnterActionName")}",
                    mainCaption, "Play on Linux");
                if (!nameResult.Result)
                {
                    return;
                }

                AddSteamWineBridgeAction(game, $"[WB] {nameResult.SelectedString}", steamAppId);
            }
        }

        private void AddSteamNonSteamLinuxAction(GameMenuItemActionArgs args)
        {
            foreach (var game in args.Games)
            {
                var mainCaption =
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_AddSteamNonSteamLinuxAction")}";
                var result = PlayniteApi.Dialogs.SelectString(
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_EnterSteamNonSteamAppId")}",
                    mainCaption, "");
                if (!result.Result)
                {
                    return;
                }

                if (!ulong.TryParse(result.SelectedString, out var givenId))
                {
                    PlayniteApi.Dialogs.ShowErrorMessage(
                        ResourceProvider.GetString("LOC_Yalgrin_WineBridge_Error_InvalidNumber"));
                    return;
                }

                //https://github.com/ValveSoftware/steam-for-linux/issues/9463#issuecomment-2558366504
                ulong appId;
                ulong trackingId;
                if (givenId >= (ulong)1 << 32)
                {
                    appId = givenId;
                    trackingId = givenId >> 32;
                }
                else
                {
                    trackingId = givenId;
                    appId = (givenId << 32) | (1L << 25);
                }

                var nameResult = PlayniteApi.Dialogs.SelectString(
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_EnterActionName")}",
                    mainCaption, "Play on Linux");
                if (!nameResult.Result)
                {
                    return;
                }

                AddSteamWineBridgeAction(game, $"[WB] {nameResult.SelectedString}", appId, trackingId);
            }
        }

        private void AddHeroicAction(GameMenuItemActionArgs args)
        {
            var caption =
                ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_SelectInstalledHeroicGame");
            var installedGames = HeroicClient.GetInstalledGames();
            var options = installedGames.ConvertAll(g => new GenericItemOption
            {
                Name = g.Name,
                Description = $"{g.AppId} | {g.Platform.ToHeroicRunner()} | {g.Platform}"
            }).OrderBy(g => g.Name).ToList();

            foreach (var game in args.Games)
            {
                var mainCaption =
                    $"{game.Name} - {caption}";
                var selectedItem = PlayniteApi.Dialogs.ChooseItemWithSearch(options,
                    str => options.Where(o =>
                        string.IsNullOrEmpty(str) || o.Name.ToLowerInvariant().Contains(str.ToLowerInvariant()) ||
                        o.Description.ToLowerInvariant().Contains(str.ToLowerInvariant())).ToList(),
                    caption: mainCaption);
                if (selectedItem == null)
                {
                    return;
                }

                var split = Regex.Split(selectedItem.Description, " \\| ");
                if (split.Length != 3)
                {
                    return;
                }

                var nameResult = PlayniteApi.Dialogs.SelectString(
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_EnterActionName")}",
                    mainCaption, "Play on Linux");
                if (!nameResult.Result)
                {
                    return;
                }

                var foundGame =
                    installedGames.Find(g => g.AppId == split[0] && g.Platform.ToHeroicRunner() == split[1]);
                if (foundGame?.InstallPath != null)
                {
                    game.InstallDirectory = WineUtils.LinuxPathToWindows(foundGame.InstallPath);
                }

                AddHeroicWineBridgeAction(game, $"[WB] {nameResult.SelectedString}", split[0], split[1]);
            }
        }

        private void AddHeroicCustomAction(GameMenuItemActionArgs args)
        {
            foreach (var game in args.Games)
            {
                var mainCaption =
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_AddHeroicCustomAction")}";
                var appIdResult = PlayniteApi.Dialogs.SelectString(
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_EnterHeroicAppId")}",
                    mainCaption, "");
                if (!appIdResult.Result)
                {
                    return;
                }

                var runnerResult = PlayniteApi.Dialogs.SelectString(
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_EnterHeroicRunner")}",
                    mainCaption, "sideload");
                if (!runnerResult.Result)
                {
                    return;
                }

                var nameResult = PlayniteApi.Dialogs.SelectString(
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_EnterActionName")}",
                    mainCaption, "Play on Linux");
                if (!nameResult.Result)
                {
                    return;
                }

                var foundGame = HeroicClient.GetInstalledGames().Find(g =>
                    g.AppId == appIdResult.SelectedString &&
                    g.Platform.ToHeroicRunner() == runnerResult.SelectedString);
                if (foundGame?.InstallPath != null)
                {
                    game.InstallDirectory = WineUtils.LinuxPathToWindows(foundGame.InstallPath);
                }

                AddHeroicWineBridgeAction(game, $"[WB] {nameResult.SelectedString}", appIdResult.SelectedString,
                    runnerResult.SelectedString);
            }
        }

        private void AddLutrisAction(GameMenuItemActionArgs args)
        {
            var caption =
                ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_SelectInstalledLutrisGame");
            var installedGames = LutrisClient.GetInstalledGames();
            var options = installedGames.ConvertAll(g => new GenericItemOption
            {
                Name = g.Name,
                Description = $"{g.LutrisId} | {GetTranslatedServiceName(g.Service)}"
            }).OrderBy(g => g.Name).ToList();
            foreach (var game in args.Games)
            {
                var mainCaption =
                    $"{game.Name} - {caption}";
                var selectedItem = PlayniteApi.Dialogs.ChooseItemWithSearch(options,
                    str => options.Where(o =>
                        string.IsNullOrEmpty(str) || o.Name.ToLowerInvariant().Contains(str.ToLowerInvariant()) ||
                        o.Description.ToLowerInvariant().Contains(str.ToLowerInvariant())).ToList(),
                    caption: mainCaption);
                if (selectedItem == null)
                {
                    return;
                }

                var split = Regex.Split(selectedItem.Description, " \\| ");
                if (split.Length != 2)
                {
                    return;
                }

                var nameResult = PlayniteApi.Dialogs.SelectString(
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_EnterActionName")}",
                    mainCaption, "Play on Linux");
                if (!nameResult.Result)
                {
                    return;
                }

                var searchId = Convert.ToInt64(split[0]);
                var foundGame =
                    installedGames.Find(g => g.LutrisId == searchId);
                if (foundGame?.InstallPath != null)
                {
                    game.InstallDirectory = WineUtils.LinuxPathToWindows(foundGame.InstallPath);
                }

                AddLutrisWineBridgeAction(game, $"[WB] {nameResult.SelectedString}", split[0]);
            }
        }

        private static string GetTranslatedServiceName(string service)
        {
            if (string.IsNullOrEmpty(service))
            {
                return ResourceProvider.GetString("LOC_Yalgrin_WineBridge_LutrisService_Custom");
            }

            var resource =
                ResourceProvider.GetResource("LOC_Yalgrin_WineBridge_LutrisService_" + service.ToLowerInvariant());
            if (resource == null)
            {
                return ResourceProvider.GetString("LOC_Yalgrin_WineBridge_LutrisService_Unknown") + "('" + service +
                       "')";
            }

            return resource as string;
        }

        private void AddLutrisCustomAction(GameMenuItemActionArgs args)
        {
            foreach (var game in args.Games)
            {
                var mainCaption =
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_AddLutrisCustomAction")}";
                var appIdResult = PlayniteApi.Dialogs.SelectString(
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_EnterLutrisAppId")}",
                    mainCaption, "");
                if (!appIdResult.Result)
                {
                    return;
                }

                var nameResult = PlayniteApi.Dialogs.SelectString(
                    $"{game.Name} - {ResourceProvider.GetString("LOC_Yalgrin_WineBridge_GameMenuItem_Dialog_EnterActionName")}",
                    mainCaption, "Play on Linux");
                if (!nameResult.Result)
                {
                    return;
                }

                var foundGame = HeroicClient.GetInstalledGames().Find(g =>
                    g.AppId == appIdResult.SelectedString);
                if (foundGame?.InstallPath != null)
                {
                    game.InstallDirectory = WineUtils.LinuxPathToWindows(foundGame.InstallPath);
                }

                AddLutrisWineBridgeAction(game, $"[WB] {nameResult.SelectedString}", appIdResult.SelectedString);
            }
        }

        private void RemoveAllActions(GameMenuItemActionArgs args)
        {
            foreach (var game in args.Games)
            {
                var actions = game.GameActions;
                if (actions == null)
                {
                    continue;
                }

                var modified = false;
                for (var i = actions.Count - 1; i >= 0; i--)
                {
                    var gameAction = actions[i];
                    if (gameAction.Name.StartsWith("[WB]"))
                    {
                        actions.Remove(gameAction);
                        modified = true;
                    }
                }

                if (!modified)
                {
                    continue;
                }

                game.IsInstalled = false;
                game.OverrideInstallState = false;
                game.IncludeLibraryPluginAction = true;
                PlayniteApi.Database.Games.Update(game);
            }
        }

        private void AddSteamWineBridgeAction(Game game, string name, ulong steamAppId)
        {
            AddSteamWineBridgeAction(game, name, steamAppId, steamAppId);
        }

        private void AddSteamWineBridgeAction(Game game, string name, ulong steamAppId, ulong trackingAppId)
        {
            AddWineBridgeAction(game, name, $"{Constants.WineBridgeSteamPrefix}{steamAppId}", $"{trackingAppId}");
        }

        private void AddHeroicWineBridgeAction(Game game, string name, string appId, string runner)
        {
            AddWineBridgeAction(game, name, $"{Constants.WineBridgeHeroicPrefix}{runner}/{appId}", null);
        }

        private void AddLutrisWineBridgeAction(Game game, string name, string appId)
        {
            AddWineBridgeAction(game, name, $"{Constants.WineBridgeLutrisPrefix}{appId}", null);
        }

        private void AddWineBridgeAction(Game game, string name, string launchScript, bool asyncAction,
            string trackingExpression)
        {
            var launchScriptWithPrefix = (asyncAction ? Constants.WineBridgeAsyncPrefix : Constants.WineBridgePrefix) +
                                         launchScript;
            string arguments = null;
            if (asyncAction)
            {
                arguments = $"{trackingExpression}";
            }

            AddWineBridgeAction(game, name, launchScriptWithPrefix, arguments);
        }

        private void AddWineBridgeAction(Game game, string name, string launchScript,
            string trackingExpression)
        {
            var actions = game.GameActions;
            if (actions == null)
            {
                actions = new ObservableCollection<GameAction>();
                game.GameActions = actions;
            }

            GameAction action = null;
            var add = false;
            for (var i = 0; i < actions.Count; i++)
            {
                var gameAction = actions[i];
                if (gameAction.Name != name)
                {
                    continue;
                }

                if (action == null)
                {
                    action = gameAction;
                }
                else
                {
                    actions.Remove(gameAction);
                }
            }

            if (action == null)
            {
                action = new GameAction();
                add = true;
            }

            action.Name = name;
            action.IsPlayAction = true;
            action.Type = GameActionType.File;
            action.TrackingMode = TrackingMode.Default;
            action.Path = launchScript;
            if (trackingExpression != null)
            {
                action.Arguments = $"{trackingExpression}";
            }

            action.WorkingDir = "";

            if (add)
            {
                actions.Add(action);
            }

            game.IsInstalled = true;
            game.OverrideInstallState = true;
            game.IncludeLibraryPluginAction = false;
            PlayniteApi.Database.Games.Update(game);
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            //not used
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            //not used
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            //not used
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            //not used
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            //not used
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            //not used
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            //not used
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            //not used
        }
    }
}