namespace NoiseCleaner;

public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly TextBox _ffmpeg = new() { Dock = DockStyle.Fill };
    private readonly TextBox _deepFilter = new() { Dock = DockStyle.Fill };
    private readonly TextBox _model = new() { Dock = DockStyle.Fill };
    private readonly CheckBox _portable = new() { Text = "Portable 모드 (실행파일 폴더에 tools/models 설치)", AutoSize = true };
    private readonly TextBox _status = new() { Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, Height = 80 };

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;
        Text = "NoiseCleaner 설정";
        Width = 760;
        Height = 430;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(700, 400);

        _portable.Checked = settings.PortableMode;
        _ffmpeg.Text = settings.FfmpegPath ?? settings.ResolveFfmpegPath();
        _deepFilter.Text = settings.DeepFilterPath ?? settings.ResolveDeepFilterPath();
        _model.Text = settings.ModelPath ?? settings.ResolveModelPath() ?? Path.Combine(settings.ModelsDirectory, "DeepFilterNet3_onnx.tar.gz");

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 4, RowCount = 6 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        AddPathRow(grid, 0, "FFmpeg", _ffmpeg, () => BrowseExe(_ffmpeg, "ffmpeg.exe"), InstallFfmpeg);
        AddPathRow(grid, 1, "DeepFilterNet", _deepFilter, () => BrowseExe(_deepFilter, "deep-filter.exe"), InstallDeepFilter);
        AddPathRow(grid, 2, "모델", _model, BrowseModel, InstallModel);

        grid.Controls.Add(_portable, 0, 3);
        grid.SetColumnSpan(_portable, 4);
        grid.Controls.Add(_status, 0, 4);
        grid.SetColumnSpan(_status, 4);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        var save = new Button { Text = "저장", AutoSize = true };
        var close = new Button { Text = "닫기", AutoSize = true };
        save.Click += (_, _) => SaveSettings();
        close.Click += (_, _) => Close();
        buttons.Controls.Add(save);
        buttons.Controls.Add(close);
        grid.Controls.Add(buttons, 0, 5);
        grid.SetColumnSpan(buttons, 4);

        Controls.Add(grid);
    }

    private void AddPathRow(TableLayoutPanel grid, int row, string label, TextBox box, Action browse, Func<Task> install)
    {
        var browseButton = new Button { Text = "찾아보기", AutoSize = true };
        var installButton = new Button { Text = "자동 설치", AutoSize = true };
        browseButton.Click += (_, _) => browse();
        installButton.Click += async (_, _) => await install();
        grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        grid.Controls.Add(box, 1, row);
        grid.Controls.Add(browseButton, 2, row);
        grid.Controls.Add(installButton, 3, row);
    }

    private void BrowseExe(TextBox target, string fileName)
    {
        using var dialog = new OpenFileDialog { Filter = $"{fileName}|{fileName}|Executable|*.exe|All files|*.*" };
        if (dialog.ShowDialog(this) == DialogResult.OK) target.Text = dialog.FileName;
    }

    private void BrowseModel()
    {
        using var dialog = new OpenFileDialog { Filter = "DeepFilterNet model|*.tar.gz;*.tgz|All files|*.*" };
        if (dialog.ShowDialog(this) == DialogResult.OK) _model.Text = dialog.FileName;
    }

    private void SaveSettings()
    {
        _settings.PortableMode = _portable.Checked;
        _settings.FfmpegPath = _ffmpeg.Text.Trim();
        _settings.DeepFilterPath = _deepFilter.Text.Trim();
        _settings.ModelPath = _model.Text.Trim();
        _settings.Save();
        SetStatus("설정을 저장했습니다.");
    }

    private async Task InstallFfmpeg()
    {
        await RunInstall(async p =>
        {
            _ffmpeg.Text = await DependencyInstaller.InstallFfmpegAsync(_settings, p);
        });
    }

    private async Task InstallDeepFilter()
    {
        await RunInstall(async p =>
        {
            _deepFilter.Text = await DependencyInstaller.InstallDeepFilterAsync(_settings, p);
        });
    }

    private async Task InstallModel()
    {
        await RunInstall(async p =>
        {
            _model.Text = await DependencyInstaller.InstallModelAsync(_settings, p);
        });
    }

    private async Task RunInstall(Func<IProgress<string>, Task> action)
    {
        try
        {
            _settings.PortableMode = _portable.Checked;
            var progress = new Progress<string>(SetStatus);
            UseWaitCursor = true;
            await action(progress);
        }
        catch (Exception ex)
        {
            SetStatus("설치 실패: " + ex.Message);
            MessageBox.Show(this, ex.Message, "설치 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void SetStatus(string text)
    {
        if (InvokeRequired) { BeginInvoke(() => SetStatus(text)); return; }
        _status.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
    }
}
