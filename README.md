# NoiseCleaner

Windows GUI wrapper for local video/audio noise reduction using **DeepFilterNet** and **FFmpeg**. No Python is required on the user's machine.

## Features

- Drag & drop MP4/MKV/MOV/AVI/WebM/WAV and other common media files
- Extracts audio to 48 kHz WAV with FFmpeg
- Runs the native `deep-filter.exe` CLI
- Optionally enables DeepFilterNet post-filter (`--pf`)
- Muxes the cleaned audio back into the original video without re-encoding the video stream
- Outputs `<name>_clean.mp4` or `<name>_clean.wav`

## Requirements

Place these files next to the published application:

```text
NoiseCleaner.exe
tools/
  ffmpeg.exe
  deep-filter.exe
```

DeepFilterNet's native CLI accepts 48 kHz WAV input; NoiseCleaner converts input automatically before running it.

### deep-filter.exe

Download the precompiled native binary from the official DeepFilterNet releases and rename the Windows executable to `deep-filter.exe` if necessary.

Official project: https://github.com/Rikorose/DeepFilterNet

### ffmpeg.exe

Use a trusted Windows FFmpeg build and place `ffmpeg.exe` in `tools/`.

## Build

Requires .NET 8 SDK on the development machine only.

```powershell
dotnet restore
dotnet build -c Release
dotnet publish NoiseCleaner/NoiseCleaner.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Published EXE will be under:

```text
NoiseCleaner/bin/Release/net8.0-windows/win-x64/publish/
```

Copy the `tools` directory beside the published EXE.

## Notes

- The end user does **not** need Python.
- The published app is self-contained, so the end user does not need to install the .NET runtime either.
- The current mux step outputs MP4 with AAC audio while copying the original video stream.
- DeepFilterNet's native CLI supports `-D` delay compensation and `--pf` post-filter; this GUI enables delay compensation by default.

## License

This repository contains only the wrapper source code. FFmpeg and DeepFilterNet are separate projects with their own licenses. Review their licenses before redistributing their binaries together with this app.
