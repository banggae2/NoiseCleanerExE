using System.Text.Json;

namespace NoiseCleaner;

public sealed class AppSettings
{
    public bool PortableMode { get; set; } = true;
    public string? FfmpegPath { get; set; }
    public string? DeepFilterPath { get; set; }
    public string? ModelPath { get; set; }

    public static string SettingsFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NoiseCleaner", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFile)) return new AppSettings();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsFile)) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(SettingsFile)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(SettingsFile, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    public string BaseDataDirectory => PortableMode
        ? AppContext.BaseDirectory
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NoiseCleaner");

    public string ToolsDirectory => Path.Combine(BaseDataDirectory, "tools");
    public string ModelsDirectory => Path.Combine(BaseDataDirectory, "models");

    public string ResolveFfmpegPath()
    {
        if (!string.IsNullOrWhiteSpace(FfmpegPath) && File.Exists(FfmpegPath)) return FfmpegPath;
        var local = Path.Combine(ToolsDirectory, "ffmpeg.exe");
        return local;
    }

    public string ResolveDeepFilterPath()
    {
        if (!string.IsNullOrWhiteSpace(DeepFilterPath) && File.Exists(DeepFilterPath)) return DeepFilterPath;
        var local = Path.Combine(ToolsDirectory, "deep-filter.exe");
        return local;
    }

    public string? ResolveModelPath()
    {
        if (!string.IsNullOrWhiteSpace(ModelPath) && File.Exists(ModelPath)) return ModelPath;
        var local = Path.Combine(ModelsDirectory, "DeepFilterNet3_onnx.tar.gz");
        return File.Exists(local) ? local : null;
    }
}
