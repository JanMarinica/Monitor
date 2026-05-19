using System.Runtime.InteropServices;

namespace ScreenMonitor;

public static class MouseSimulator
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public MOUSEINPUT mi;
        // Pad to the size of the union (keyboard/hardware inputs are larger)
        private long _pad1;
        private long _pad2;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    private const uint INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;

    public static void Click(int screenX, int screenY, bool rightButton = false)
    {
        int screenW = GetSystemMetrics(SM_CXSCREEN);
        int screenH = GetSystemMetrics(SM_CYSCREEN);

        // Normalise to 0–65535 range expected by MOUSEEVENTF_ABSOLUTE
        int absX = (int)Math.Round(screenX * 65535.0 / (screenW - 1));
        int absY = (int)Math.Round(screenY * 65535.0 / (screenH - 1));

        uint downFlag = rightButton ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_LEFTDOWN;
        uint upFlag   = rightButton ? MOUSEEVENTF_RIGHTUP   : MOUSEEVENTF_LEFTUP;

        INPUT[] inputs =
        [
            new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT
                {
                    dx = absX,
                    dy = absY,
                    dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
                }
            },
            new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT { dx = absX, dy = absY, dwFlags = downFlag | MOUSEEVENTF_ABSOLUTE }
            },
            new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT { dx = absX, dy = absY, dwFlags = upFlag | MOUSEEVENTF_ABSOLUTE }
            }
        ];

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }
}
