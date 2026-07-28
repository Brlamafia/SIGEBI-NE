using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace SIGEBI.Desktop;

internal sealed class DashboardDonut : Control
{
    private int _percentage;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Percentage
    {
        get => _percentage;
        set
        {
            _percentage = Math.Clamp(value, 0, 100);
            Invalidate();
        }
    }

    public DashboardDonut()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        MinimumSize = new Size(118, 118);
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var size = Math.Max(1, Math.Min(Width, Height) - 20);
        var bounds = new Rectangle((Width - size) / 2, (Height - size) / 2, size, size);
        using var track = new Pen(Color.FromArgb(58, 255, 255, 255), 13);
        using var progress = new Pen(Color.FromArgb(160, 231, 255), 13)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        eventArgs.Graphics.DrawArc(track, bounds, -90, 360);
        if (Percentage > 0)
            eventArgs.Graphics.DrawArc(progress, bounds, -90, Percentage * 3.6f);

        TextRenderer.DrawText(
            eventArgs.Graphics,
            $"{Percentage}%",
            DesktopTheme.Font(20, FontStyle.Bold),
            new Rectangle(0, Height / 2 - 25, Width, 34),
            Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(
            eventArgs.Graphics,
            "atendidas",
            DesktopTheme.Font(8),
            new Rectangle(0, Height / 2 + 8, Width, 22),
            Color.FromArgb(220, 246, 255),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

internal sealed class DashboardProgressBar : Control
{
    private int _value;
    private int _maximum = 1;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0, Maximum);
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(1, value);
            _value = Math.Min(_value, _maximum);
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BarColor { get; set; } = DesktopTheme.Primary;

    public DashboardProgressBar()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        Height = 9;
        MinimumSize = new Size(40, 9);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 1, Math.Max(1, Width - 1), Math.Max(5, Height - 2));
        using var trackPath = RoundedPath(bounds);
        using var trackBrush = new SolidBrush(Color.FromArgb(231, 237, 247));
        eventArgs.Graphics.FillPath(trackBrush, trackPath);

        var fillWidth = (int)Math.Round(bounds.Width * (Value / (double)Maximum));
        if (fillWidth <= 0)
            return;
        var fillBounds = new Rectangle(bounds.X, bounds.Y, Math.Max(bounds.Height, fillWidth), bounds.Height);
        using var fillPath = RoundedPath(fillBounds);
        using var fillBrush = new SolidBrush(BarColor);
        eventArgs.Graphics.FillPath(fillBrush, fillPath);
    }

    private static GraphicsPath RoundedPath(Rectangle bounds)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(bounds.Width, bounds.Height);
        if (diameter < 2)
        {
            path.AddRectangle(bounds);
            return path;
        }
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 90, 180);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 180);
        path.CloseFigure();
        return path;
    }
}
