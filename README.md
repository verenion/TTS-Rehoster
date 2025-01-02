# TTSRehoster

A work-in-progress tool for processing Tabletop Simulator assets - enabling re-hosting of saves/mods along with required 
assets. This tool allows you to take a working mod/save and create a local "copy" of the save, enabling the ability to 
"re-host" the assets under your own "Steam Cloud Manager". Once all assets are local, images can also be upscaled to
improve quality.

### Current Features

- Converts a TTS Save file / Mod to utilise local assets, instead of cloud assets
- Automatically upscales images

### Upscaling Demo

![Comparison of AI upscaling](app/imgs/Screenshot%202025-01-02%20132207.png)
![Comparison of AI upscaling](app/imgs/Screenshot%202025-01-02%20132351.png)

Before: 40.7MB\
After: 32.33MB
  

### TODO

- Configuration options for asset naming/structure
- Configuration options for AI Upscaling
- New model for NCNN and Real-ESRGAN

### Why?

There are two main problems this tool aims to solve:

1. Mods, and Saves, are stored as `.json` files. However, the assets are simply stored as 
external URLs, usually pointing to Steam's servers - whereby users can manage their own assets. However, if those assets
are removed by the uploader (intentionally or accidentally), the mod will no longer work - this is a problem that many 
mods have. This tool will not fix that, but it will allow you to preempt that, by taking a *working* save/mod, downloading
all the assets locally and creating a new "local" save, that when loaded, the assets can be uploaded to your own Steam
workshop using the built-in TTS tools.
2. Some mods that people upload are fairly poor quality, this tool can use AI to optionally "upscale" the image.
