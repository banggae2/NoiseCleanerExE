$ErrorActionPreference = 'Stop'
dotnet publish .\NoiseCleaner\NoiseCleaner.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
Write-Host "Published to NoiseCleaner\\bin\\Release\\net8.0-windows\\win-x64\\publish"
