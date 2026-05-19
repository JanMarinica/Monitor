namespace ScreenMonitor;

public static class ScreenCapture
{
    /// <summary>Captures the given screen rectangle into a new Bitmap.</summary>
    public static Bitmap Capture(Rectangle region)
    {
        var bmp = new Bitmap(region.Width, region.Height);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(region.Location, Point.Empty, region.Size);
        return bmp;
    }
}
