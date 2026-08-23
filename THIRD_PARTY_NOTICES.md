# Third-Party Notices

NoiseCleanerExE itself is licensed under the MIT License. The following components are separate third-party projects and are not part of the NoiseCleanerExE source license.

## FFmpeg

Project: https://ffmpeg.org/

Automatic installation uses the BtbN Windows x64 LGPL build named `ffmpeg-master-latest-win64-lgpl.zip` from:

https://github.com/BtbN/FFmpeg-Builds

FFmpeg licensing depends on how it is configured and built. NoiseCleanerExE intentionally selects the LGPL-labelled BtbN build. Anyone redistributing FFmpeg with NoiseCleanerExE should independently verify the exact downloaded build and comply with the applicable FFmpeg/LGPL notices and source-offer requirements.

## DeepFilterNet

Project: https://github.com/Rikorose/DeepFilterNet

Automatic installation downloads the upstream Windows x64 `deep-filter` executable from the project's latest GitHub release.

DeepFilterNet source code is offered upstream under its stated license terms. Check the upstream repository at the time of redistribution for the authoritative license text and notices.

## DeepFilterNet model files

NoiseCleanerExE can download `DeepFilterNet3_onnx.tar.gz` directly from the upstream DeepFilterNet repository.

Model weights may have licensing or redistribution terms distinct from source code. This repository therefore does not bundle or redistribute model weights. If you create a distributable package containing the downloaded model, verify the model's applicable upstream license/permission first.

## Distribution policy of this repository

This repository contains only the NoiseCleanerExE wrapper source code and build scripts. FFmpeg binaries, DeepFilterNet binaries, and model weights are fetched from their upstream sources only when the user requests installation at runtime.
