namespace NoiseCleaner;

public sealed class AppSettings
{
    public static AppSettings Load() => new();

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
