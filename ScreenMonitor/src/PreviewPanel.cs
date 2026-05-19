namespace ScreenMonitor;

/// <summary>
/// A panel that shows a live thumbnail of the monitored region with a diff heat bar.
/// </summary>
public sealed class PreviewPanel : Panel
{
    private Bitmap? _snapshot;
    private double _diffPercent;
    private readonly object _lock = new();

    public PreviewPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(30, 30, 30);
    }

    public void UpdateSnapshot(Bitmap bmp)
    {
        Bitmap copy = (Bitmap)bmp.Clone();
        lock (_lock)
        {
            _snapshot?.Dispose();
            _snapshot = copy;
        }
        BeginInvoke(Invalidate);
    }

    public void UpdateDiff(double percent)
    {
        _diffPercent = percent;
        BeginInvoke(Invalidate);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);

        Bitmap? snap;
        lock (_lock) snap = _snapshot;

        if (snap is not null)
        {
            var dst = FitRect(snap.Width, snap.Height, ClientRectangle with { Height = ClientRectangle.Height - 14 });
            g.DrawImage(snap, dst);
        }
        else
        {
            var msg = "No preview yet";
            using var f = new Font("Segoe UI", 10);
            var sz = g.MeasureString(msg, f);
            g.DrawString(msg, f, Brushes.Gray,
                (Width - sz.Width) / 2, (Height - 14 - sz.Height) / 2);
        }

        // Diff bar at the bottom
        var barRect = new Rectangle(0, Height - 12, Width, 12);
        g.FillRectangle(Brushes.Black, barRect);
        double clamped = Math.Min(_diffPercent, 100);
        int fillW = (int)(clamped / 100.0 * Width);
        if (fillW > 0)
        {
            var color = _diffPercent < 5 ? Color.LimeGreen
                      : _diffPercent < 20 ? Color.Orange
                      : Color.OrangeRed;
            using var bar = new SolidBrush(color);
            g.FillRectangle(bar, new Rectangle(0, Height - 12, fillW, 12));
        }
        using var barFont = new Font("Segoe UI", 7);
        g.DrawString($"Δ {_diffPercent:F1}%", barFont, Brushes.White, 2, Height - 11);
    }

    private static Rectangle FitRect(int srcW, int srcH, Rectangle dest)
    {
        if (srcW == 0 || srcH == 0) return dest;
        float scale = Math.Min((float)dest.Width / srcW, (float)dest.Height / srcH);
        int w = (int)(srcW * scale);
        int h = (int)(srcH * scale);
        return new Rectangle(dest.X + (dest.Width - w) / 2, dest.Y + (dest.Height - h) / 2, w, h);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _snapshot?.Dispose();
        base.Dispose(disposing);
    }
}
