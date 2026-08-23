namespace NoiseCleaner;

public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly TextBox _ffmpeg = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly TextBox _deepFilter = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly TextBox _model = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly TextBox _status = new() { Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, Height = 110 };

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;
        Text = "NoiseCleaner 포터블 구성";
        Width = 760;
        Height = 400;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(700, 360);

        RefreshPaths();

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 3, RowCount = 6 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        AddRow(grid, 0, "FFmpeg", _ffmpeg, InstallFfmpeg);
        AddRow(grid, 1, "DeepFilterNet", _deepFilter, InstallDeepFilter);
        AddRow(grid, 2, "모델", _model, InstallModel);

        var info = new Label
        {
            AutoSize = true,
            Text = "모든 구성요소는 NoiseCleaner.exe가 있는 폴더 아래 tools / models에만 설치됩니다."
        };
        grid.Controls.Add(info, 0, 3);
        grid.SetColumnSpan(info, 3);

        grid.Controls.Add(_status, 0, 4);
        grid.SetColumnSpan(_status, 3);

        var close = new Button { Text = "닫기", AutoSize = true, Anchor = AnchorStyles.Right };
        close.Click += (_, _) => Close();
        grid.Controls.Add(close, 2, 5);

        Controls.Add(grid);
    }

    private void AddRow(TableLayoutPanel grid, int row, string label, TextBox box, Func<Task> install)
    {
        var installButton = new Button { Text = "자동 설치", AutoSize = true };
        installButton.Click += async (_, _) => await install();
        grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        grid.Controls.Add(box, 1, row);
        grid.Controls.Add(installButton, 2, row);
    }

    private void RefreshPaths()
    {
        _ffmpeg.Text = _settings.ResolveFfmpegPath();
        _deepFilter.Text = _settings.ResolveDeepFilterPath();
        _model.Text = Path.Combine(_settings.ModelsDirectory, "DeepFilterNet3_onnx.tar.gz");
    }

    private async Task InstallFfmpeg() => await RunInstall(async p =>
    {
        await DependencyInstaller.InstallFfmpegAsync(_settings, p);
        RefreshPaths();
    });

    private async Task InstallDeepFilter() => await RunInstall(async p =>
    {
        await DependencyInstaller.InstallDeepFilterAsync(_settings, p);
        RefreshPaths();
    });

    private async Task InstallModel() => await RunInstall(async p =>
    {
        await DependencyInstaller.InstallModelAsync(_settings, p);
        RefreshPaths();
    });

    private async Task RunInstall(Func<IProgress<string>, Task> action)
    {
        try
        {
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
