# Playnite Wine Bridge Plugin

This plugin allows you to integrate [Playnite](https://playnite.link/) running under Wine on Linux with native
applications. It allows you to detect currently installed games, install, uninstall and launch games the same way
it works on Windows. Also, it gives you the ability to configure any Linux game to be launched from Playnite.

How does it work? It uses [Harmony](https://harmony.pardeike.net/) to patch specific methods in Playnite and library
plugins to redirect them to a custom script that allows you to launch and track any process on Linux.

## Motivation

After I moved my primary gaming PC to Linux, I've experimented with running Playnite under Wine and finally managed
to get it working with my existing library of games. As expected, many things such as game installation and launching
were not working at all. After some research, I've managed to create scripts that allow me to run native terminal
commands from Playnite, which in turn allows me to launch any game regardless of how it was installed. However, it
quickly became cumbersome to write custom play actions for every game I wanted to launch. And thus, this plugin
was created.

## Information

The plugin currently supports:

- integrations for libraries such as: Steam, GOG, Amazon & more using Steam Linux client, Heroic Games Launcher and
  Lutris
- option to add custom Steam play actions including non-Steam games
- option to add Heroic play actions for any installed game
- option to add Lutris play actions for any installed game
- option to add custom play actions for any game

## Integrations

| Linux launcher |              Integrated libraries              | Detecting installed games |                                                                 Installation & uninstallation                                                                  | Launching |
|:--------------:|:----------------------------------------------:|:-------------------------:|:--------------------------------------------------------------------------------------------------------------------------------------------------------------:|:---------:|
|     Steam      |                     Steam                      |             ✅             |                                                                               ✅                                                                                |     ✅     |
|     Heroic     |               GOG, Amazon, Epic                |             ✅             | ⚠️ <br/>Cannot install/uninstall from Playnite; it will launch Heroic client for you to do that manually. After it completes Playnite will properly detect it. |     ✅     |
|     Lutris     | GOG, Amazon, Epic, EA App, Battle.net, Itch.io |             ✅             |     ️⚠️ <br/>Cannot uninstall games from Playnite; it will launch Lutris for you to do that manually. After it completes Playnite will properly detect it.     |     ✅     |

## Planned features

- search options when adding game actions for Steam
- support for more libraries such as GOG OSS, Legendary and Nile
- support for other Playnite features such as tools and emulators

## Instructions

### Basic configuration

1. Install [Playnite](https://playnite.link/) inside a [Wine Prefix](https://www.winehq.org/) on your Linux machine.
   This isn't a tutorial for that, but here are some general tips: use Wine 9 or above, install `corefonts dotnet48`
   inside the prefix using `winetricks` and once the app is installed enable the following option: Settings > Advanced >
   Disable hardware acceleration (or try using a virtual desktop). Avoid using `gamemoderun` or any other wrappers.

> **Note: Since you're using Playnite under Wine, which is not officially supported, you might encounter unexpected
issues. If you do, please DON'T just blindly report them to the official Playnite (or related plugin) issue tracker.
Check if the problem occurs on the Windows version first.**

2. Install any library extensions you want to use with Playnite. Install this plugin from the official Playnite
   extension repository.

3. Configure the plugin in Playnite settings as follows:

![](Screenshots/configuration.png)

_All of these paths & game install locations need to be accessible in Wine. By default, Wine maps the root directory `/`
to drive `Z:`, so it shouldn't be a problem unless you manually changed them._

Notable parameters:

- **Tracking directory (Linux)** - directory used to track running processes from Wine. By default, it's set to `/tmp`
  folder. You can leave it as is.
- **Steam data path (Linux)** and **Steam executable path (Linux)** - paths to Steam data folder and executable. The
  data folder is the one containing `steamapps` and `userdata` directories. Typical configurations:
    - For native installation - `/home/<user>/.local/share/Steam` and `steam`
    - For flatpak installation - `/home/<user>/.var/app/com.valvesoftware.Steam/.local/share/Steam` and
      `flatpak run com.valvesoftware.Steam`
- **Heroic data path (Linux)** and **Heroic executable path (Linux)** - paths to Heroic data folder and executable. The
  data folder is the one containing folders such as `gog_store`, `nile_store` and `legendaryConfig`. Typical
  configurations:
    - For native installation - `/home/<user>/.config/heroic` and `heroic`
    - For AppImage installation - `/home/<user>/.config/heroic` and `<path_to_Heroic_AppImage_file>`
    - For flatpak installation - `/home/<user>/.var/app/com.heroicgameslauncher.hgl/config/heroic` and
      `flatpak run com.heroicgameslauncher.hgl`
- **Lutris data path (Linux)** and **Lutris executable path (Linux)** - paths to Lutris data folder and executable. The
  data folder is the one containing database file `pga.db` and folders such as `games` and `runners`. Typical
  configurations:
    - For native installation - `/home/<user>/.local/share/lutris` and `lutris`
    - For flatpak installation - `/home/<user>/.var/app/net.lutris.Lutris/data/lutris` and
      `flatpak run net.lutris.Lutris`
- **Enable debug logging** - enables more detailed logging. If you're facing a problem, then enable this option,
  reproduce the problem and report the issue with the logs attached.

Most paths should be auto-detected on first start. You can use the corresponding buttons to detect them again.

**All paths need to be absolute paths, not relative!**

4. Restart Playnite. Go back to the settings and make sure that System, Playnite & installed libraries patching states
   are displayed as "
   Patched".

### Custom Steam play actions

If you want a specific play action to trigger a Steam game launch, you can add it as a custom Steam play action by
right-clicking on the desired game and going to the **Wine Bridge** menu.

First, find the App ID for the desired game. For bought Steam games, you can find it for example: in the store page URL
or by going to game page in [SteamDB](https://steamdb.info). For non-Steam games, your best bet would likely be to
create a desktop shortcut and check its properties for the ID.

1. Right-click on the game and go to the **Wine Bridge** menu.
2. Select **Add Steam game action** or **Add Non-Steam game added to Steam action**.
3. Enter the App ID and confirm it with **OK**.
4. The game should be marked as installed and you should be able to launch it from Playnite.

### Custom Heroic play actions

If you want a specific play action to trigger a Heroic game launch, you can add it as a custom Heroic play action by
right-clicking on the desired game and going to the **Wine Bridge** menu.

The first and easier option is to use **Add Heroic installed game action**. It allows you to select an installed game
from Heroic, which will automatically generate a play action.

If you want to go the manual route you can still do that by using **Add Heroic custom game action** option. This
requires you to manually enter app id and a runner for the specific game, which you can get for example by creating a
desktop shortcut.

### Custom Lutris play actions

If you want a specific play action to trigger a Lutris game launch, you can add it as a custom Lutris play action by
right-clicking on the desired game and going to the **Wine Bridge** menu.

The first and easier option is to use **Add Lutris installed game action**. It allows you to select an installed game
from Lutris, which will automatically generate a play action.

If you want to go the manual route you can still do that by using **Add Lutris custom game action** option. This
requires you to manually enter app id for the specific game, which you can get for example by creating a desktop
shortcut.

### Custom play actions

If you want to run other games from Playnite, you can add them as custom play actions by right-clicking on the desired
game and going to the **Wine Bridge** menu.

Before you do anything, make sure you know the terminal command that will launch the game. Run the command in the
terminal and determine whether it waits for the game to close or not.

If the command doesn't immediately close and waits for the game to shutdown, follow the **Synchronous commands**
section. If not, use the **Asynchronous commands** section.

#### Synchronous commands

1. Right-click on the game and go to the **Wine Bridge** menu.
2. Select **Add custom Linux action**.
3. Enter the terminal command you've previously used to launch the game and confirm it with **OK**.
4. Enter the custom name for the play action (or leave it at default) and confirm it with **OK**.
5. The game should be marked as installed and you should be able to launch it from Playnite.

For example:

- if I want to launch **Space Cadet Pinball** installed as a Flatpak, I would use the following command:
  `flatpak run com.github.k4zmu2a.spacecadetpinball`

#### Asynchronous commands

1. Determine the **tracking expression** for the game you want to launch. The script uses `pgrep` to find the exact
   process(es) it needs to track, so you want to use the same format. One way to do it is to run the following commands:

```shell
   ps -aux
```

and manually search for the process name. Use `grep` to narrow down the results.

```shell
   ps -aux | grep "<search term>"
```

Make sure the search term finds only the game and not any other processes. It's worth mentioning that `pgrep` is case
sensitive, so pay attention when choosing the proper search term. Once you're done, write down the final expression.

2. Right-click on the game and go to the **Wine Bridge** menu.
3. Select **Add custom async Linux action**.
4. Enter the terminal command you've previously used to launch the game and confirm it with **OK**.
5. Enter the found tracking expression and confirm it with **OK**.
6. Enter the custom name for the play action (or leave it at default) and confirm it with **OK**.
7. The game should be marked as installed and you should be able to launch it from Playnite.

For example:

- if I want to launch **Guild Wars 2** installed using Heroic Games Launcher as a custom game, I would use the following
  command and tracking expression:
    - command - `xdg-open 'heroic://launch?appName=pPo838krjLRRDrJWvtPm3M&runner=sideload'` which I obtained by creating
      a shortcut through Heroic and getting the command from the properties of the shortcut.
    - tracking expression - `Gw2-64.exe` since it spawns a process with the name of this file in the name. You can
      probably use something more precise just to be sure.

## Potential issues

#### Random Wine windows pop-up

It's possible that the Linux script has not been assigned proper permissions. Use the following commands to assign them
manually.

```shell
# Checks the execution permissions for the script
ls -al <wine_prefix_path>/drive_c/users/<user>/AppData/Roaming/Playnite/Extensions/Yalgrin_WineBridgePlugin/Resources/run-in-linux.sh

# Sets the execution permissions for the script
chmod a+x <wine_prefix_path>/drive_c/users/<user>/AppData/Roaming/Playnite/Extensions/Yalgrin_WineBridgePlugin/Resources/run-in-linux.sh
```

#### The game never gets past the "Launching" state or displays an error regarding something in Wine

Likely caused by invalid configuration or missing permissions. Make sure you've followed the instructions carefully and
that you've set the correct paths. You can also enable debug logging and check the logs.

#### The game launches but Playnite treats it as if it was closed immediately

This is most likely caused by invalid tracking expression. Launch the game and manually find it using
`ps -aux | grep "<search term>"` command. If it returns nothing, then you will need to adjust the tracking expression.

#### The game launched but it is stuck on the "Playing" state

This is most likely caused by invalid tracking expression. Launch the game and manually find it using
`ps -aux | grep "<search term>"` command. If it returns anything other than the game process, then you will need to
adjust the tracking expression.

## Donate

If you like what I do, feel free to buy me a coffee.

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/yalgrin)

## Credits

- [Playnite](https://playnite.link/) by [JosefNemec](https://github.com/JosefNemec)
- [Harmony](https://harmony.pardeike.net/) by [Pardeike](https://github.com/pardeike)
- [Wine project](https://www.winehq.org/)
- [Wine icons created by Freepik - Flaticon](https://www.flaticon.com/free-icons/wine)
