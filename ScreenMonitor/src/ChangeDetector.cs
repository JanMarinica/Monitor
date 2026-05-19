using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ScreenMonitor;

public static class ChangeDetector
{
    /// <summary>
    /// Returns the percentage of pixels that differ between two same-sized bitmaps.
    /// Uses fast Marshal-based byte comparison on the raw pixel data.
    /// </summary>
    public static double DiffPercent(Bitmap a, Bitmap b)
    {
        if (a.Width != b.Width || a.Height != b.Height)
            return 100.0;

        var rect = new Rectangle(0, 0, a.Width, a.Height);

        BitmapData dataA = a.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        BitmapData dataB = b.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            int byteCount = Math.Abs(dataA.Stride) * a.Height;
            byte[] bytesA = new byte[byteCount];
            byte[] bytesB = new byte[byteCount];

            Marshal.Copy(dataA.Scan0, bytesA, 0, byteCount);
            Marshal.Copy(dataB.Scan0, bytesB, 0, byteCount);

            int diffPixels = 0;
            int totalPixels = a.Width * a.Height;

            // Each pixel is 4 bytes (B G R A); compare R,G,B only
            for (int i = 0; i < byteCount; i += 4)
            {
                if (bytesA[i] != bytesB[i] ||
                    bytesA[i + 1] != bytesB[i + 1] ||
                    bytesA[i + 2] != bytesB[i + 2])
                {
                    diffPixels++;
                }
            }

            return diffPixels * 100.0 / totalPixels;
        }
        finally
        {
            a.UnlockBits(dataA);
            b.UnlockBits(dataB);
        }
    }
}
