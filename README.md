# NoiseCleanerExE

Windows GUI wrapper for local video/audio noise reduction using **DeepFilterNet** and **FFmpeg**. No Python is required on the user's machine.

## Features

- Drag & drop MP4/MKV/MOV/AVI/WebM/WAV and other common media files
- Extracts audio to 48 kHz WAV with FFmpeg
- Runs the native `deep-filter.exe` CLI
- Optional DeepFilterNet post-filter (`--pf`)
- Supports an explicit DeepFilterNet model with `-m`
- Muxes cleaned audio back into the original video without re-encoding the video stream
- Outputs `<name>_clean.mp4` or `<name>_clean.wav`
- Settings are saved to `%LOCALAPPDATA%\NoiseCleaner\settings.json`

## Dependency setup

Open **Settings** in the app. Each dependency can either be installed automatically or pointed at an existing file.

### Portable mode

When Portable mode is enabled, automatic installs go beside the executable:

```text
NoiseCleaner.exe
tools/
  ffmpeg.exe
  deep-filter.exe
models/
  DeepFilterNet3_onnx.tar.gz
```

### LocalAppData mode

When Portable mode is disabled, dependencies are installed under:

```text
%LOCALAPPDATA%\NoiseCleaner\tools\
%LOCALAPPDATA%\NoiseCleaner\models\
```

You can also choose arbitrary paths for FFmpeg, DeepFilterNet and the model.

## Automatic downloads

The app downloads third-party components directly from their upstream public sources at runtime. They are not committed to or bundled in this repository.

- FFmpeg: BtbN Windows x64 **LGPL** static build (`ffmpeg-master-latest-win64-lgpl.zip`)
- DeepFilterNet: latest Windows x64 `deep-filter` executable from the official DeepFilterNet GitHub release
- Model: `DeepFilterNet3_onnx.tar.gz` from the official DeepFilterNet repository

DeepFilterNet's native CLI accepts 48 kHz WAV input; NoiseCleaner converts input automatically before running it.

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

## License and third-party software

NoiseCleanerExE wrapper source code is MIT licensed.

FFmpeg, DeepFilterNet and DeepFilterNet model files are separate third-party works with their own license terms. The project does not include their binaries or model files in the repository; the application can download them from upstream at the user's request.

See [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md) before redistributing a package that contains downloaded third-party files.
