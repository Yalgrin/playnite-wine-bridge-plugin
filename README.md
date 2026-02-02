# Playnite Wine Bridge Plugin

This plugin allows you to integrate [Playnite](https://playnite.link/) running under Wine on Linux with native
applications. It is designed to fix certain issues caused by running under Wine, mainly related to library
integrations and game launching. It aims to restore the original Windows experience as much as possible.

How does it work? It uses [Harmony](https://harmony.pardeike.net/) to patch specific methods in Playnite and library
plugins to redirect them to a custom script that allows you to launch and track any process on Linux.

## Motivation

After I moved my primary gaming PC to Linux, I've experimented with running Playnite under Wine and finally managed
to get it working with my existing library of games. As expected, many things such as game installation and launching
were not working at all. After some research, I've managed to create scripts that allow me to run native terminal
commands from Playnite, which in turn allows me to launch any game regardless of how it was installed. However, it
quickly became cumbersome to write custom play actions for every game I wanted to launch. And thus, this plugin
was created.

## Features

- [integration for Playnite libraries such as Steam, GOG, Epic etc.](https://github.com/Yalgrin/playnite-wine-bridge-plugin/wiki/Libraries)
- [emulator support](https://github.com/Yalgrin/playnite-wine-bridge-plugin/wiki/Emulators)
- [play actions for any supported launcher and custom actions](https://github.com/Yalgrin/playnite-wine-bridge-plugin/wiki/Play-Actions)
- [custom tools](https://github.com/Yalgrin/playnite-wine-bridge-plugin/wiki/Tools)
- open folders and links inside default apps in Linux

## Integrations

| Linux launcher |                     Integrated libraries                      | Detecting installed games |                                                                 Installation & uninstallation                                                                  | Launching |
|:--------------:|:-------------------------------------------------------------:|:-------------------------:|:--------------------------------------------------------------------------------------------------------------------------------------------------------------:|:---------:|
|     Steam      |                             Steam                             |             ✅             |                                                                               ✅                                                                                |     ✅     |
|     Heroic     |                    GOG<br/>Amazon<br/>Epic                    |             ✅             | ⚠️ <br/>Cannot install/uninstall from Playnite; it will launch Heroic client for you to do that manually. After it completes Playnite will properly detect it. |     ✅     |
|     Lutris     | GOG<br/>Amazon<br/>Epic<br/>EA App<br/>Battle.net<br/>Itch.io |             ✅             |     ️⚠️ <br/>Cannot uninstall games from Playnite; it will launch Lutris for you to do that manually. After it completes Playnite will properly detect it.     |     ✅     |

## Planned features

- support for more libraries such as GOG OSS, Legendary and Nile

## Instructions

Visit the [wiki](https://github.com/Yalgrin/playnite-wine-bridge-plugin/wiki) for detailed instructions.

## Known issues

Visit the [wiki page](https://github.com/Yalgrin/playnite-wine-bridge-plugin/wiki/Issues) for a list of known issues and potential solutions.

## Donate

If you like what I do, feel free to buy me a coffee.

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/yalgrin)

## Credits

- [Playnite](https://playnite.link/) by [JosefNemec](https://github.com/JosefNemec)
- [Harmony](https://harmony.pardeike.net/) by [Pardeike](https://github.com/pardeike)
- [Wine project](https://www.winehq.org/)
- [VDFParser](https://github.com/BrianLima/VDFParser) by [Brian Lima](https://github.com/BrianLima)
- [Wine icons created by Freepik - Flaticon](https://www.flaticon.com/free-icons/wine)
