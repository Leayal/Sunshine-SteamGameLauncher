# Sunshine-SteamGameLauncher
 Steam game launcher helper for Sunshine RemotePlay on Windows OSs.

# Note
 - The purpose of this tool was to make Steam Client launch the game like usual behavior instead of launching the executable file directly.
 - If launching the steam game with `steam://launch/<AppId>`, `steam://run/<AppId>` and `steam://rungameid/<AppId>`, [Moonlight](https://github.com/moonlight-stream/moonlight-qt) may complain `Something went wrong on your host PC when starting the stream` with something about `Make sure you don't have any DRM-protected content open on your host PC`. To workaround this, set the main command of the application in [Sunshine](https://github.com/LizardByte/Sunshine)'s setting to this tool's executable file (see Usage below for the command format).
 - Currently the tool doesn't support cross-privilege, if the game requires Administration to launch, the tool has to be run as Admin, too. Otherwise, you don't need to (or rather, shouldn't) run the tool as Admin.

# Usage
- `Sunshine-SteamGameLauncher.exe <SteamAppId> <Optional: Executable File Name>`
  + `Sunshine-SteamGameLauncher.exe` is the executable name of this tool, if you rename it to something else, please adjust the command accordingly.
  + `<SteamAppId>` is the numeric ID of the game, you can visit the game's store page and get the AppId from the store's URL (E.g: `https://store.steampowered.com/app/2719150/`=>AppId is `2719150`).
  + `<Optional: Executable File Name>` is obviously **Optional** and can be omitted. If you specify this argument, the tool will search for the specified executable file name and wait until the said executable file exit, instead of auto-finding executable name (which sometimes finds wrong one).


- This is just an example, if you put the tool at another location or having different game's ID, please adjust those accordingly:
  + Example use `D:\Tools\Sunshine-SteamGameLauncher.exe` as the path of the tool.
  + Example use `Holo X Break` game, the game's Store URL is `https://store.steampowered.com/app/2719150/Holo_X_Break/`.
  + Configure [Sunshine](https://github.com/LizardByte/Sunshine) application as follow: Set main command to be `"D:\Tools\Sunshine-SteamGameLauncher.exe" 2719150`, or `"D:\Tools\Sunshine-SteamGameLauncher.exe" 2719150 "Holo X Break.exe"` if the tool can't find the correct executable file.
  + Save and done. Please pay attention to the double quotes `"` in the command, they're *kind of* neccessary to be there.