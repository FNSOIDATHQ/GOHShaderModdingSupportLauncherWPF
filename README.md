# Gate of Hell Shader Modding Support Launcher

Language：English [中文](./READMEcn.md) 

## Catalog
* [User Guide](#user-guide)
* [Runtime Details](#runtime-details)
* * [Preprocess](#preprocess)
* * [Postprocess](#postprocess)
* [Development Guide](#development-guide)
* [Credits](#credits)
* [Support my Work](#support-my-work)
---

## User Guide

Please check the workshop page of this launcher for user guide.  
https://steamcommunity.com/sharedfiles/filedetails/?id=3410344592

## Runtime Details

* At startup, the launcher loads settings from %LOCALAPPDATA%\GOHSMSLauncher\settings.conf。
* No matter what launch option is selected, program will follow the process below:
0. Move Environment.CurrentDirectory to game directory
1. Preprocess
2. Launch game with startup parameters
3. Hide windows to make programs run in the background
4. Wait for game program to finish
5. When game finished, run postprocess
6. Decide to exit the program or redisplay the window according to settings

* When exit, save caches

### Preprocess

* If no cached game path, run search method
* * If program is in subfolder of steamapps, search game by hardcoded path
* * Else get steam path from Registry and search game path in all game libraries
* * Move Environment.CurrentDirectory to game directory
* [File Replace Method Only] Replace Files in game root directory
* * If no modified shader.pak found,extract pak from program
* * Else using cached pak to replace original shader.pak
* Force set bump quality to parallax in player profile

### Postprocess

* [File Replace Method Only][Need enable in Settings] Restore vanilla shader file, modified file will remain as cache
* [Need enable in Settings] Clear shader cache in C:\Users\YOURUSERNAME\Documents\my games\gates of hell\shader_cache

### Notice
* Force change bump quality is a special measure used to support my shader mods
* Move Environment.CurrentDirectory to game directory is always necessary to let steam not truncate our launch commands
* Settings are saved under LocalAppData, with compatibility for existing settings.conf beside the EXE.
* Game Launch process always comes with the -showmodinfo parameter, which is an enhancement I found that let game shows detailed mod information

## Development Guide

I'm building this program using Visual Studio 2026, with environment below:
* .NET 10 SDK (Visual Studio 2026)
* Windows Presentation Foundation(WPF)
* WPF-UI 4.3.0 through the MIT license https://github.com/lepoco/wpfui
* Publish with `dotnet publish -c Release -p:PublishProfile=FolderProfile`. Distribute the sole EXE from `bin/net10-single-file/`.
* The target PC must have the .NET 10 Windows Desktop Runtime (x64) installed. The EXE does not include the runtime.

## Credits
Special Thanks to  
* @𝙆𝙄𝙍𝙄𝙉 𝙎𝙏𝙍𝙊𝙉𝙂 Provides high-res material for launcher icon  
* Players who participated in the launcher test  

for their contribution during the development!  

## Support my Work
If you like my products, please give this repository a STAR, I'd appreciate it =)  
  
If possible, you can directly support my work in the following ways:

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/N4N2ZJR4A)  
[![mbd.pub](./img/mbd.png)](https://mbd.pub/o/fedStudio)  