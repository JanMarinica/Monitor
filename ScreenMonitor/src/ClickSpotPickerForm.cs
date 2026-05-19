namespace ScreenMonitor;

/// <summary>
/// Fullscreen overlay; user clicks once to set the auto-click target.
/// Returns the chosen Point in screen coordinates, or Point.Empty on cancel.
/// </summary>
public sealed class ClickSpotPickerForm : Form
{
    public Point SelectedPoint { get; private set; } = Point.Empty;

    private Point _hover;

    private static readonly Brush DimBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
    private static readonly Pen CrosshairPen = new(Color.FromArgb(255, 255, 80, 0), 2);
    private static readonly Font HintFont = new("Segoe UI", 14, FontStyle.Bold);
    private static readonly Brush HintBrush = Brushes.White;
    private const int CrossSize = 20;

    public ClickSpotPickerForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        TopMost = true;
        Cursor = Cursors.Cross;
        DoubleBuffered = true;
        BackColor = Color.Black;
        Opacity = 0.5;
        KeyPreview = true;

        Bounds = SystemInformation.VirtualScreen;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            SelectedPoint = Point.Empty;
            DialogResult = DialogResult.Cancel;
            Close();
        }
        base.OnKeyDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        _hover = e.Location;
        Invalidate();
        base.OnMouseMove(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            SelectedPoint = PointToScreen(e.Location);
            DialogResult = DialogResult.OK;
            Close();
        }
        base.OnMouseDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.FillRectangle(DimBrush, ClientRectangle);

        // Crosshair
        g.DrawLine(CrosshairPen,
            _hover.X - CrossSize, _hover.Y,
            _hover.X + CrossSize, _hover.Y);
        g.DrawLine(CrosshairPen,
            _hover.X, _hover.Y - CrossSize,
            _hover.X, _hover.Y + CrossSize);
        g.DrawEllipse(CrosshairPen,
            _hover.X - CrossSize / 2, _hover.Y - CrossSize / 2,
            CrossSize, CrossSize);

        string coords = $"({PointToScreen(_hover).X}, {PointToScreen(_hover).Y})";
        g.DrawString(coords, HintFont, HintBrush, _hover.X + CrossSize, _hover.Y - CrossSize);

        string hint = "Click to set click target   •   Esc to cancel";
        var sz = g.MeasureString(hint, HintFont);
        g.DrawString(hint, HintFont, HintBrush,
            (Width - sz.Width) / 2, Height - sz.Height - 30);
    }
}
