namespace ScreenMonitor;

public sealed class MainForm : Form
{
    private readonly Config _cfg;
    private MonitorEngine? _engine;

    // Controls
    private readonly Button _btnSelectRegion;
    private readonly Button _btnSelectClick;
    private readonly Button _btnStartStop;
    private readonly Label _lblRegion;
    private readonly Label _lblClick;
    private readonly Label _lblStatus;
    private readonly TrackBar _trackInterval;
    private readonly TrackBar _trackThreshold;
    private readonly TrackBar _trackCooldown;
    private readonly Label _lblIntervalVal;
    private readonly Label _lblThresholdVal;
    private readonly Label _lblCooldownVal;
    private readonly PreviewPanel _preview;
    private readonly ListBox _logBox;

    public MainForm()
    {
        _cfg = Config.Load();

        Text = "Screen Monitor";
        Size = new Size(560, 660);
        MinimumSize = new Size(480, 560);
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(32, 32, 32);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 9f);

        // ── Section: Selection ────────────────────────────────────────
        var grpSelect = MakeGroup("Selection", 8, 8, ClientSize.Width - 18, 130);

        _btnSelectRegion = MakeButton("Pick Monitor Region", 8, 22, 200, 36);
        _btnSelectRegion.Click += OnSelectRegion;
        grpSelect.Controls.Add(_btnSelectRegion);

        _lblRegion = MakeLabel("Not set", 216, 30, grpSelect.Width - 224, 20);
        grpSelect.Controls.Add(_lblRegion);

        _btnSelectClick = MakeButton("Pick Click Spot", 8, 68, 200, 36);
        _btnSelectClick.Click += OnSelectClick;
        grpSelect.Controls.Add(_btnSelectClick);

        _lblClick = MakeLabel("Not set", 216, 76, grpSelect.Width - 224, 20);
        grpSelect.Controls.Add(_lblClick);

        Controls.Add(grpSelect);

        // ── Section: Settings ─────────────────────────────────────────
        var grpSettings = MakeGroup("Settings", 8, 148, ClientSize.Width - 18, 120);

        AddSlider(grpSettings, "Poll interval (ms):", 8, 18, 100, 2000, _cfg.PollIntervalMs,
            out _trackInterval, out _lblIntervalVal,
            v => { _cfg.PollIntervalMs = v; if (_engine is not null) { } });

        AddSlider(grpSettings, "Change threshold (%):", 8, 56, 1, 50, (int)_cfg.ChangeThresholdPercent,
            out _trackThreshold, out _lblThresholdVal,
            v => _cfg.ChangeThresholdPercent = v);

        AddSlider(grpSettings, "Cooldown (s):", 8, 94, 1, 30, (int)_cfg.CooldownSeconds,
            out _trackCooldown, out _lblCooldownVal,
            v => _cfg.CooldownSeconds = v);

        Controls.Add(grpSettings);

        // ── Section: Preview ──────────────────────────────────────────
        var grpPreview = MakeGroup("Live Preview", 8, 278, ClientSize.Width - 18, 130);
        _preview = new PreviewPanel { Dock = DockStyle.Fill };
        grpPreview.Controls.Add(_preview);
        Controls.Add(grpPreview);

        // ── Start / Stop ──────────────────────────────────────────────
        _btnStartStop = MakeButton("▶  Start Monitoring", 8, 418, ClientSize.Width - 18, 40);
        _btnStartStop.BackColor = Color.FromArgb(0, 140, 60);
        _btnStartStop.Click += OnStartStop;
        Controls.Add(_btnStartStop);

        // ── Status ────────────────────────────────────────────────────
        _lblStatus = MakeLabel("Ready.", 8, 466, ClientSize.Width - 18, 20);
        _lblStatus.ForeColor = Color.Silver;
        Controls.Add(_lblStatus);

        // ── Log ───────────────────────────────────────────────────────
        _logBox = new ListBox
        {
            Left = 8, Top = 490,
            Width = ClientSize.Width - 18,
            Height = ClientSize.Height - 500,
            BackColor = Color.FromArgb(20, 20, 20),
            ForeColor = Color.LightGray,
            BorderStyle = BorderStyle.FixedSingle,
            ScrollAlwaysVisible = true,
            Font = new Font("Consolas", 8.5f),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Top
        };
        Controls.Add(_logBox);

        UpdateLabels();
    }

    // ── Event handlers ────────────────────────────────────────────────

    private void OnSelectRegion(object? sender, EventArgs e)
    {
        bool wasRunning = _engine?.IsRunning ?? false;
        if (wasRunning) StopEngine();

        Hide();
        using var picker = new RegionSelectorForm();
        if (picker.ShowDialog() == DialogResult.OK)
        {
            var r = picker.SelectedRegion;
            _cfg.MonitorRegion.X = r.X;
            _cfg.MonitorRegion.Y = r.Y;
            _cfg.MonitorRegion.Width = r.Width;
            _cfg.MonitorRegion.Height = r.Height;
            _cfg.Save();
            Log($"Region set: ({r.X},{r.Y}) {r.Width}×{r.Height}");
        }
        Show();
        Activate();
        UpdateLabels();
        if (wasRunning) StartEngine();
    }

    private void OnSelectClick(object? sender, EventArgs e)
    {
        bool wasRunning = _engine?.IsRunning ?? false;
        if (wasRunning) StopEngine();

        Hide();
        using var picker = new ClickSpotPickerForm();
        if (picker.ShowDialog() == DialogResult.OK)
        {
            _cfg.ClickTarget.X = picker.SelectedPoint.X;
            _cfg.ClickTarget.Y = picker.SelectedPoint.Y;
            _cfg.Save();
            Log($"Click target set: ({picker.SelectedPoint.X}, {picker.SelectedPoint.Y})");
        }
        Show();
        Activate();
        UpdateLabels();
        if (wasRunning) StartEngine();
    }

    private void OnStartStop(object? sender, EventArgs e)
    {
        if (_engine?.IsRunning ?? false)
            StopEngine();
        else
            StartEngine();
    }

    private void StartEngine()
    {
        if (!_cfg.MonitorRegion.IsSet)
        {
            MessageBox.Show("Please select a monitor region first.", "Screen Monitor",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!_cfg.ClickTarget.IsSet)
        {
            MessageBox.Show("Please select a click target first.", "Screen Monitor",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _engine?.Dispose();
        _engine = new MonitorEngine(_cfg);
        _engine.StatusChanged += OnEngineStatus;
        _engine.DiffReported += OnDiffReported;
        _engine.Start();

        _btnStartStop.Text = "⏹  Stop Monitoring";
        _btnStartStop.BackColor = Color.FromArgb(160, 40, 40);
        _btnSelectRegion.Enabled = false;
        _btnSelectClick.Enabled = false;
    }

    private void StopEngine()
    {
        _engine?.Stop();
        _engine?.Dispose();
        _engine = null;

        _btnStartStop.Text = "▶  Start Monitoring";
        _btnStartStop.BackColor = Color.FromArgb(0, 140, 60);
        _btnSelectRegion.Enabled = true;
        _btnSelectClick.Enabled = true;
    }

    private void OnEngineStatus(string msg)
    {
        if (InvokeRequired) { Invoke(() => OnEngineStatus(msg)); return; }
        _lblStatus.Text = msg;
        Log(msg);
    }

    private void OnDiffReported(double diff)
    {
        if (InvokeRequired) { Invoke(() => OnDiffReported(diff)); return; }
        _preview.UpdateDiff(diff);

        // Grab a fresh thumbnail for the preview
        if (_cfg.MonitorRegion.IsSet)
        {
            try
            {
                var bmp = ScreenCapture.Capture(_cfg.MonitorRegion.ToRectangle());
                _preview.UpdateSnapshot(bmp);
                bmp.Dispose();
            }
            catch { /* ignore transient errors */ }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopEngine();
        _cfg.Save();
        base.OnFormClosing(e);
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private void UpdateLabels()
    {
        var r = _cfg.MonitorRegion;
        _lblRegion.Text = r.IsSet
            ? $"({r.X}, {r.Y})  {r.Width} × {r.Height} px"
            : "Not set";

        var c = _cfg.ClickTarget;
        _lblClick.Text = c.IsSet ? $"({c.X}, {c.Y})" : "Not set";
    }

    private void Log(string msg)
    {
        string entry = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        _logBox.Items.Add(entry);
        _logBox.TopIndex = _logBox.Items.Count - 1;
    }

    // ── Builder helpers ───────────────────────────────────────────────

    private static GroupBox MakeGroup(string text, int x, int y, int w, int h)
    {
        return new GroupBox
        {
            Text = text, Left = x, Top = y, Width = w, Height = h,
            ForeColor = Color.Silver,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
    }

    private static Button MakeButton(string text, int x, int y, int w, int h)
    {
        return new Button
        {
            Text = text, Left = x, Top = y, Width = w, Height = h,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.WhiteSmoke,
            BackColor = Color.FromArgb(60, 60, 80),
            Cursor = Cursors.Hand
        };
    }

    private static Label MakeLabel(string text, int x, int y, int w, int h)
    {
        return new Label
        {
            Text = text, Left = x, Top = y, Width = w, Height = h,
            ForeColor = Color.LightGray,
            AutoEllipsis = true
        };
    }

    private void AddSlider(GroupBox parent, string label, int x, int y,
        int min, int max, int initial,
        out TrackBar track, out Label valLabel,
        Action<int> onChange)
    {
        var lbl = MakeLabel(label, x, y + 2, 160, 18);
        parent.Controls.Add(lbl);

        var t = new TrackBar
        {
            Left = x + 166, Top = y - 4,
            Width = parent.Width - x - 166 - 60,
            Height = 30,
            Minimum = min, Maximum = max, Value = initial,
            TickFrequency = (max - min) / 10,
            AutoSize = false,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        track = t;

        var vl = MakeLabel(initial.ToString(), t.Right + 4, y + 2, 50, 18);
        vl.ForeColor = Color.CornflowerBlue;
        valLabel = vl;

        t.ValueChanged += (_, _) =>
        {
            vl.Text = t.Value.ToString();
            onChange(t.Value);
            _cfg.Save();
        };

        parent.Controls.Add(track);
        parent.Controls.Add(valLabel);
    }
}
