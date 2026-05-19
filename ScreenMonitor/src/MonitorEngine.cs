namespace ScreenMonitor;

/// <summary>
/// Polls the configured screen region and fires a click when pixel change exceeds threshold.
/// Runs on a background thread; thread-safe start/stop.
/// </summary>
public sealed class MonitorEngine : IDisposable
{
    public event Action<string>? StatusChanged;
    public event Action<double>? DiffReported;

    private readonly Config _cfg;
    private CancellationTokenSource? _cts;
    private Task? _task;
    private bool _disposed;

    public bool IsRunning => _task is { IsCompleted: false };

    public MonitorEngine(Config cfg) => _cfg = cfg;

    public void Start()
    {
        if (IsRunning) return;
        _cts = new CancellationTokenSource();
        _task = Task.Run(() => Loop(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        try { _task?.Wait(2000); } catch { /* ignore */ }
    }

    private void Loop(CancellationToken ct)
    {
        Report("Monitoring started.");
        Bitmap? previous = null;
        DateTime lastClick = DateTime.MinValue;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var region = _cfg.MonitorRegion.ToRectangle();
                if (region.Width <= 0 || region.Height <= 0)
                {
                    Report("No monitor region set — pausing.");
                    Thread.Sleep(1000);
                    continue;
                }

                Bitmap current = ScreenCapture.Capture(region);

                if (previous is not null)
                {
                    double diff = ChangeDetector.DiffPercent(previous, current);
                    DiffReported?.Invoke(diff);

                    bool cooldownOk = (DateTime.Now - lastClick).TotalSeconds >= _cfg.CooldownSeconds;

                    if (diff >= _cfg.ChangeThresholdPercent && cooldownOk)
                    {
                        Report($"Change detected ({diff:F1}%) — clicking ({_cfg.ClickTarget.X}, {_cfg.ClickTarget.Y}).");
                        MouseSimulator.Click(_cfg.ClickTarget.X, _cfg.ClickTarget.Y);
                        lastClick = DateTime.Now;
                    }
                }

                previous?.Dispose();
                previous = current;

                Thread.Sleep(_cfg.PollIntervalMs);
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            Report($"Error: {ex.Message}");
        }
        finally
        {
            previous?.Dispose();
            Report("Monitoring stopped.");
        }
    }

    private void Report(string msg) => StatusChanged?.Invoke(msg);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _cts?.Dispose();
    }
}
