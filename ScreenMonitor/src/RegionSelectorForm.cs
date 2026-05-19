namespace ScreenMonitor;

/// <summary>
/// Fullscreen semi-transparent overlay; user drags a rectangle to select the monitor region.
/// Returns the selected Rectangle in screen coordinates, or Rectangle.Empty on cancel.
/// </summary>
public sealed class RegionSelectorForm : Form
{
    public Rectangle SelectedRegion { get; private set; } = Rectangle.Empty;

    private Point _start;
    private Rectangle _current;
    private bool _dragging;

    private static readonly Brush DimBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
    private static readonly Pen SelectionPen = new(Color.FromArgb(255, 0, 180, 255), 2);
    private static readonly Brush SelectionBrush = new SolidBrush(Color.FromArgb(50, 0, 180, 255));
    private static readonly Font HintFont = new("Segoe UI", 14, FontStyle.Bold);
    private static readonly Brush HintBrush = Brushes.White;

    public RegionSelectorForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        TopMost = true;
        Cursor = Cursors.Cross;
        DoubleBuffered = true;
        BackColor = Color.Black;
        Opacity = 0.01; // nearly invisible until mouse down — avoids flash
        KeyPreview = true;

        // Span all monitors
        var bounds = SystemInformation.VirtualScreen;
        Bounds = bounds;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Opacity = 0.5;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            SelectedRegion = Rectangle.Empty;
            DialogResult = DialogResult.Cancel;
            Close();
        }
        base.OnKeyDown(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _start = e.Location;
            _current = Rectangle.Empty;
            _dragging = true;
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging)
        {
            _current = MakeRect(_start, e.Location);
            Invalidate();
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_dragging && e.Button == MouseButtons.Left)
        {
            _dragging = false;
            _current = MakeRect(_start, e.Location);

            if (_current.Width > 4 && _current.Height > 4)
            {
                // Convert form-local coords to screen coords
                SelectedRegion = new Rectangle(
                    PointToScreen(_current.Location),
                    _current.Size);
                DialogResult = DialogResult.OK;
            }
            else
            {
                SelectedRegion = Rectangle.Empty;
                DialogResult = DialogResult.Cancel;
            }
            Close();
        }
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.FillRectangle(DimBrush, ClientRectangle);

        if (_current.Width > 0 && _current.Height > 0)
        {
            // Cut out (lighten) the selected area
            g.FillRectangle(SelectionBrush, _current);
            g.DrawRectangle(SelectionPen, _current);

            string size = $"{_current.Width} × {_current.Height}";
            var textSize = g.MeasureString(size, HintFont);
            g.DrawString(size, HintFont, HintBrush,
                _current.X + (_current.Width - textSize.Width) / 2,
                _current.Y + (_current.Height - textSize.Height) / 2);
        }
        else
        {
            string hint = "Drag to select monitor region   •   Esc to cancel";
            var sz = g.MeasureString(hint, HintFont);
            g.DrawString(hint, HintFont, HintBrush,
                (Width - sz.Width) / 2, (Height - sz.Height) / 2);
        }
    }

    private static Rectangle MakeRect(Point a, Point b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y),
            Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
}
