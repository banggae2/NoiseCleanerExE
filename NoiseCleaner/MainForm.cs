using System.Diagnostics;

namespace NoiseCleaner;

public sealed class MainForm : Form
{
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly TextBox _input = new() { Dock = DockStyle.Fill, ReadOnly = true, PlaceholderText = "MP4 / MKV / MOV / WAV 파일을 선택하세요" };
    private readonly Button _browse = new() { Text = "파일 선택", AutoSize = true };
    private readonly Button _start = new() { Text = "소음 제거 시작", AutoSize = true, Enabled = false };
    private readonly Button _settingsButton = new() { Text = "설정", AutoSize = true };
    private readonly ComboBox _qualityPreset = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly CheckBox _postFilter = new() { Text = "Post-filter", Checked = false, AutoSize = true };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Fill, Style = ProgressBarStyle.Continuous };
    private readonly TextBox _log = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
    private string? _selectedFile;

    public MainForm()
    {
        Text = "NoiseCleaner - DeepFilterNet";
        Width = 820;
        Height = 560;
        MinimumSize = new Size(670, 460);
        AllowDrop = true;
        StartPosition = FormStartPosition.CenterScreen;

        _qualityPreset.Items.AddRange(new object[]
        {
            new QualityPreset("자연스러움", 12),
            new QualityPreset("균형 (추천)", 18),
            new QualityPreset("강한 제거", 30),
            new QualityPreset("최대 제거", 100)
        });
        _qualityPreset.SelectedIndex = 1;

        var fileRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
        fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fileRow.Controls.Add(_input, 0, 0);
        fileRow.Controls.Add(_browse, 1, 0);

        var actionRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        actionRow.Controls.Add(_start);
        actionRow.Controls.Add(_settingsButton);
        actionRow.Controls.Add(new Label { Text = "품질:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
        actionRow.Controls.Add(_qualityPreset);
        actionRow.Controls.Add(_postFilter);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), RowCount = 6, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "동영상/오디오 파일", AutoSize = true, Font = new Font(Font, FontStyle.Bold) }, 0, 0);
        layout.Controls.Add(fileRow, 0, 1);
        layout.Controls.Add(actionRow, 0, 2);
        layout.Controls.Add(new Label { Text = "추천 기본값: 균형 18 dB / Post-filter OFF / AAC 256 kbps", AutoSize = true }, 0, 3);
        layout.Controls.Add(_progress, 0, 4);
        layout.Controls.Add(_log, 0, 5);
        Controls.Add(layout);

        _browse.Click += (_, _) => Browse();
        _settingsButton.Click += (_, _) => OpenSettings();
        _start.Click += async (_, _) => await ProcessAsync();
        DragEnter += (_, e) => { if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy; };
        DragDrop += (_, e) =>
        {
            var files = e.Data?.GetData(DataFormats.FileDrop) as string[];
            if (files?.Length > 0) SelectFile(files[0]);
        };

        Log("모드: Portable only");
        Log("기본 품질: 균형 18 dB / Post-filter OFF / AAC 256 kbps");
    }

    private void OpenSettings()
    {
        using var form = new SettingsForm(_settings);
        form.ShowDialog(this);
    }

    private void Browse()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Media files|*.mp4;*.mkv;*.mov;*.avi;*.webm;*.wav;*.mp3;*.m4a;*.aac|All files|*.*",
            Multiselect = false
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) SelectFile(dialog.FileName);
    }

    private void SelectFile(string path)
    {
        _selectedFile = path;
        _input.Text = path;
        _start.Enabled = File.Exists(path);
        Log($"선택됨: {path}");
    }

    private async Task ProcessAsync()
    {
        if (_selectedFile is null) return;

        var ffmpeg = _settings.ResolveFfmpegPath();
        var deepFilter = _settings.ResolveDeepFilterPath();
        var model = _settings.ResolveModelPath();

        if (!File.Exists(ffmpeg) || !File.Exists(deepFilter))
        {
            var result = MessageBox.Show(this,
                "FFmpeg 또는 DeepFilterNet 실행파일을 찾을 수 없습니다.\n설정에서 자동 설치하시겠습니까?",
                "필수 파일 없음", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes) OpenSettings();
            return;
        }

        var preset = _qualityPreset.SelectedItem as QualityPreset ?? new QualityPreset("균형", 18);

        _start.Enabled = false;
        _settingsButton.Enabled = false;
        _progress.Style = ProgressBarStyle.Marquee;
        var tempDir = Path.Combine(Path.GetTempPath(), "NoiseCleaner", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var input = _selectedFile;
            var baseName = Path.GetFileNameWithoutExtension(input);
            var parent = Path.GetDirectoryName(input)!;
            var wav = Path.Combine(tempDir, "input_48k.wav");
            var outDir = Path.Combine(tempDir, "dfout");
            Directory.CreateDirectory(outDir);

            Log("1/3 오디오 추출 및 48 kHz 변환...");
            await RunAsync(ffmpeg, $"-y -i \"{input}\" -vn -ar 48000 -c:a pcm_s24le \"{wav}\"");

            Log($"2/3 DeepFilterNet 소음 제거... ({preset.Name}, attenuation {preset.AttenuationDb} dB)");
            var pf = _postFilter.Checked ? " --pf" : string.Empty;
            var modelArg = model is not null ? $" -m \"{model}\"" : string.Empty;
            var attenuationArg = $" --atten-lim {preset.AttenuationDb}";
            if (model is not null) Log($"모델: {model}");
            else Log("모델: deep-filter 기본 내장 모델 사용");
            await RunAsync(deepFilter, $"-D{pf}{attenuationArg}{modelArg} -o \"{outDir}\" \"{wav}\"");

            var cleanedWav = Directory.GetFiles(outDir, "*.wav").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
            if (cleanedWav is null) throw new InvalidOperationException("DeepFilterNet 결과 WAV를 찾을 수 없습니다.");

            var ext = Path.GetExtension(input).ToLowerInvariant();
            if (ext == ".wav")
            {
                var outWav = Path.Combine(parent, baseName + "_clean.wav");
                File.Copy(cleanedWav, outWav, true);
                Log($"완료: {outWav}");
                MessageBox.Show(this, $"완료!\n{outWav}", "NoiseCleaner", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                Log("3/3 원본 영상에 정제 오디오 결합... (AAC 256 kbps)");
                var output = Path.Combine(parent, baseName + "_clean.mp4");
                await RunAsync(ffmpeg, $"-y -i \"{input}\" -i \"{cleanedWav}\" -map 0:v:0 -map 1:a:0 -c:v copy -c:a aac -b:a 256k -shortest \"{output}\"");
                Log($"완료: {output}");
                MessageBox.Show(this, $"완료!\n{output}", "NoiseCleaner", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            Log("오류: " + ex.Message);
            MessageBox.Show(this, ex.Message, "처리 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
            _progress.Style = ProgressBarStyle.Continuous;
            _progress.Value = 0;
            _start.Enabled = _selectedFile is not null && File.Exists(_selectedFile);
            _settingsButton.Enabled = true;
        }
    }

    private async Task RunAsync(string exe, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(exe)!
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log(e.Data); };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException($"{Path.GetFileName(exe)} 종료 코드: {process.ExitCode}");
    }

    private void Log(string text)
    {
        if (InvokeRequired) { BeginInvoke(() => Log(text)); return; }
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
    }

    private sealed record QualityPreset(string Name, int AttenuationDb)
    {
        public override string ToString() => $"{Name} ({AttenuationDb} dB)";
    }
}
