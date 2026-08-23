using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;

namespace NoiseCleaner;

public static class DependencyInstaller
{
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NoiseCleanerExE", "1.0"));
        return client;
    }

    public static async Task<string> InstallFfmpegAsync(AppSettings settings, IProgress<string>? progress = null)
    {
        Directory.CreateDirectory(settings.ToolsDirectory);
        var target = Path.Combine(settings.ToolsDirectory, "ffmpeg.exe");
        var tempZip = Path.Combine(Path.GetTempPath(), $"noisecleaner_ffmpeg_{Guid.NewGuid():N}.zip");
        var tempDir = Path.Combine(Path.GetTempPath(), $"noisecleaner_ffmpeg_{Guid.NewGuid():N}");

        try
        {
            const string url = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-lgpl.zip";
            progress?.Report("FFmpeg LGPL 빌드 다운로드 중...");
            await DownloadAsync(url, tempZip);
            ZipFile.ExtractToDirectory(tempZip, tempDir);
            var found = Directory.GetFiles(tempDir, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault()
                        ?? throw new InvalidOperationException("다운로드한 압축 파일에서 ffmpeg.exe를 찾지 못했습니다.");
            File.Copy(found, target, true);
            settings.FfmpegPath = target;
            settings.Save();
            progress?.Report("FFmpeg 설치 완료");
            return target;
        }
        finally
        {
            TryDelete(tempZip);
            TryDeleteDirectory(tempDir);
        }
    }

    public static async Task<string> InstallDeepFilterAsync(AppSettings settings, IProgress<string>? progress = null)
    {
        Directory.CreateDirectory(settings.ToolsDirectory);
        var target = Path.Combine(settings.ToolsDirectory, "deep-filter.exe");

        progress?.Report("DeepFilterNet 최신 Windows 릴리스 확인 중...");
        using var response = await Http.GetAsync("https://api.github.com/repos/Rikorose/DeepFilterNet/releases/latest");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var asset = doc.RootElement.GetProperty("assets").EnumerateArray()
            .Select(x => new
            {
                Name = x.GetProperty("name").GetString() ?? string.Empty,
                Url = x.GetProperty("browser_download_url").GetString() ?? string.Empty
            })
            .FirstOrDefault(x => x.Name.StartsWith("deep-filter-", StringComparison.OrdinalIgnoreCase)
                              && x.Name.Contains("x86_64-pc-windows-msvc", StringComparison.OrdinalIgnoreCase)
                              && x.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("DeepFilterNet Windows x64 릴리스 파일을 찾지 못했습니다.");

        progress?.Report($"{asset.Name} 다운로드 중...");
        await DownloadAsync(asset.Url, target);
        settings.DeepFilterPath = target;
        settings.Save();
        progress?.Report("DeepFilterNet 설치 완료");
        return target;
    }

    public static async Task<string> InstallModelAsync(AppSettings settings, IProgress<string>? progress = null)
    {
        Directory.CreateDirectory(settings.ModelsDirectory);
        var target = Path.Combine(settings.ModelsDirectory, "DeepFilterNet3_onnx.tar.gz");
        const string url = "https://raw.githubusercontent.com/Rikorose/DeepFilterNet/main/models/DeepFilterNet3_onnx.tar.gz";
        progress?.Report("DeepFilterNet3 ONNX 모델 다운로드 중...");
        await DownloadAsync(url, target);
        settings.ModelPath = target;
        settings.Save();
        progress?.Report("모델 설치 완료");
        return target;
    }

    private static async Task DownloadAsync(string url, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync();
        await using var output = File.Create(destination);
        await input.CopyToAsync(output);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
    }
}
