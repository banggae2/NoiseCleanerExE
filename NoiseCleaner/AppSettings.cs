namespace NoiseCleaner;

public sealed class AppSettings
{
    // Compatibility members retained for older code paths. NoiseCleaner is portable-only.
    public bool PortableMode { get => true; set { } }
    public string? FfmpegPath { get => ResolveFfmpegPath(); set { } }
    public string? DeepFilterPath { get => ResolveDeepFilterPath(); set { } }
    public string? ModelPath { get => ResolveModelPath(); set { } }
    public static string SettingsFile => Path.Combine(AppContext.BaseDirectory, "portable-mode");

    public static AppSettings Load() => new();
    public void Save() { }

    public string BaseDataDirectory => AppContext.BaseDirectory;
    public string ToolsDirectory => Path.Combine(BaseDataDirectory, "tools");
    public string ModelsDirectory => Path.Combine(BaseDataDirectory, "models");

    public string ResolveFfmpegPath() => Path.Combine(ToolsDirectory, "ffmpeg.exe");
    public string ResolveDeepFilterPath() => Path.Combine(ToolsDirectory, "deep-filter.exe");

    public string? ResolveModelPath()
    {
        var local = Path.Combine(ModelsDirectory, "DeepFilterNet3_onnx.tar.gz");
        return File.Exists(local) ? local : null;
    }
}
