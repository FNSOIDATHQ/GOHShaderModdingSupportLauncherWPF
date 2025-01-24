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

* When startup, program will load cache from C:\Users\YOURUSERNAME\AppData\Local\Temp\GOHSMSLauncher\settings.conf
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
* Save settings in Temp folder is to avoid conf files being stored in locations that won't delete automatically. I do not like it when programs place config files everywhere
* Game Launch process always comes with the -showmodinfo parameter, which is an enhancement I found that let game shows detailed mod information

## Development Guide

I'm building this program using Visual Studio 2022, with environment below:
* .Net 9 SDK
* Windows Presentation Foundation(WPF)
* Microsoft.NET.ILLink.Tasks 9.0.1
* WPF-UI 3.0.5 through MIT Lience https://github.com/lepoco/wpfui 

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