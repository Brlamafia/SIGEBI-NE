using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace SIGEBI.Desktop;

internal static class DesktopTheme
{
    private static readonly PrivateFontCollection PrivateFonts = LoadPrivateFonts();
    private static readonly PrivateFontCollection HeroTitleFonts =
        LoadPrivateFonts("LibreFranklin-Bold.ttf");
    private static readonly FontFamily BodyFontFamily = FindFontFamily("DM Sans");
    private static readonly FontFamily HeadingFontFamily = FindFontFamily("Libre Franklin");
    private static readonly FontFamily HeroTitleFontFamily =
        FindFontFamily(HeroTitleFonts, "Libre Franklin");

    public static readonly Color Primary = Color.FromArgb(42, 104, 238);
    public static readonly Color PrimaryVivid = Color.FromArgb(0, 132, 218);
    public static readonly Color PrimaryDark = Color.FromArgb(8, 49, 108);
    public static readonly Color Cyan = Color.FromArgb(26, 177, 213);
    public static readonly Color Navy = Color.FromArgb(14, 27, 61);
    public static readonly Color Sidebar = Color.FromArgb(4, 88, 183);
    public static readonly Color SidebarSelected = Color.FromArgb(255, 255, 255);
    public static readonly Color Background = Color.FromArgb(243, 247, 255);
    public static readonly Color Surface = Color.White;
    public static readonly Color PrimarySoft = Color.FromArgb(232, 241, 255);
    public static readonly Color CyanSoft = Color.FromArgb(229, 248, 252);
    public static readonly Color Text = Color.FromArgb(30, 41, 59);
    public static readonly Color Muted = Color.FromArgb(100, 116, 139);
    public static readonly Color Border = Color.FromArgb(215, 226, 244);
    public static readonly Color Shadow = Color.FromArgb(28, 34, 73, 130);
    public static readonly Color Danger = Color.FromArgb(229, 34, 41);
    public static readonly Color Success = Color.FromArgb(24, 134, 75);

    public static Font Font(float size = 10, FontStyle style = FontStyle.Regular) =>
        CreateFont(BodyFontFamily, size, style);

    public static Font TitleFont(float size, FontStyle style = FontStyle.Bold) =>
        CreateFont(HeadingFontFamily, size, style);

    public static Font HeroTitleFont(float size) =>
        CreateFont(HeroTitleFontFamily, size, FontStyle.Bold);

    public static Font EyebrowFont(float size = 8.5f) =>
        CreateFont(BodyFontFamily, size, FontStyle.Bold);

    public static Font ButtonFont(float size = 9.5f) =>
        CreateFont(BodyFontFamily, size, FontStyle.Bold);

    public static Font LabelFont(float size = 9.5f) =>
        CreateFont(BodyFontFamily, size, FontStyle.Bold);

    public static Font CaptionFont(float size = 8.5f) =>
        CreateFont(BodyFontFamily, size, FontStyle.Regular);

    private static PrivateFontCollection LoadPrivateFonts(params string[] requestedFiles)
    {
        var collection = new PrivateFontCollection();
        var fontDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");
        var files = requestedFiles.Length > 0
            ? requestedFiles
            : new[]
            {
                "DMSans-Regular.ttf",
                "DMSans-Bold.ttf",
                "LibreFranklin-Regular.ttf",
                "LibreFranklin-Bold.ttf"
            };
        foreach (var fileName in files)
        {
            var path = Path.Combine(fontDirectory, fileName);
            if (File.Exists(path))
                collection.AddFontFile(path);
        }

        return collection;
    }

    private static FontFamily FindFontFamily(string familyName) =>
        FindFontFamily(PrivateFonts, familyName);

    private static FontFamily FindFontFamily(
        PrivateFontCollection collection,
        string familyName) =>
        collection.Families.FirstOrDefault(family =>
            family.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase))
        ?? FontFamily.GenericSansSerif;

    private static Font CreateFont(FontFamily family, float size, FontStyle style)
    {
        var availableStyle = family.IsStyleAvailable(style)
            ? style
            : FontStyle.Regular;
        return new Font(family, size, availableStyle, GraphicsUnit.Point);
    }

    public static void StyleForm(Form form)
    {
        form.Font = Font();
        form.BackColor = Background;
        form.ForeColor = Text;
        form.Icon = SystemIcons.Application;
    }

    public static void StyleInput(TextBox input)
    {
        input.Font = Font(11);
        input.BorderStyle = BorderStyle.FixedSingle;
        input.BackColor = Surface;
        input.ForeColor = Text;
        input.Margin = new Padding(0, 5, 0, 14);
    }

    public static void StylePrimaryButton(Button button)
    {
        StyleButton(button, Primary, Color.White);
        ConfigureButtonAnimation(
            button,
            Primary,
            Color.FromArgb(24, 124, 242),
            PrimaryDark);
    }

    public static void StyleSecondaryButton(Button button)
    {
        StyleButton(button, Color.FromArgb(226, 232, 240), Text);
        ConfigureButtonAnimation(
            button,
            Color.FromArgb(226, 232, 240),
            Color.FromArgb(211, 222, 237),
            Color.FromArgb(191, 205, 224));
    }

    public static void StyleDangerButton(Button button)
    {
        StyleButton(button, Color.FromArgb(254, 226, 226), Danger);
        ConfigureButtonAnimation(
            button,
            Color.FromArgb(254, 226, 226),
            Color.FromArgb(254, 207, 207),
            Color.FromArgb(252, 180, 180));
    }

    public static void StyleTextButton(Button button)
    {
        StyleButton(button, Surface, Primary);
        button.Padding = new Padding(6, 0, 6, 0);
        ConfigureButtonAnimation(button, Surface, PrimarySoft, CyanSoft);
    }

    private static void StyleButton(Button button, Color backColor, Color foreColor)
    {
        button.Font = ButtonFont();
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Cursor = Cursors.Hand;
        button.Padding = new Padding(14, 0, 14, 0);
        button.MinimumSize = new Size(0, 42);
        button.UseVisualStyleBackColor = false;
        ConfigureRoundedButton(button, 10);
    }

    public static void StyleNavigationButton(Button button, bool selected)
    {
        if (button is AnimatedButton animated)
        {
            animated.AnimationEnabled = true;
            animated.CustomPaintEnabled = false;
        }
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        var normal = selected
            ? Color.FromArgb(46, 255, 255, 255)
            : Color.Transparent;
        var hover = selected
            ? Color.FromArgb(64, 255, 255, 255)
            : Color.FromArgb(35, 255, 255, 255);
        button.BackColor = normal;
        button.ForeColor = Color.White;
        button.Font = Font(10.5f, selected ? FontStyle.Bold : FontStyle.Regular);
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.Padding = new Padding(18, 0, 10, 0);
        button.Height = 50;
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
        ConfigureRoundedButton(button, 12);
        ConfigureButtonAnimation(
            button,
            normal,
            hover,
            Color.FromArgb(70, 255, 255, 255));
    }

    private static void ConfigureButtonAnimation(
        Button button,
        Color normal,
        Color hover,
        Color pressed)
    {
        if (button is AnimatedButton animated)
        {
            animated.ConfigureAnimation(normal, hover, pressed);
            return;
        }

        button.BackColor = normal;
        button.FlatAppearance.MouseOverBackColor = hover;
        button.FlatAppearance.MouseDownBackColor = pressed;
    }

    private static void ConfigureRoundedButton(Button button, int radius)
    {
        if (button is AnimatedButton animated)
        {
            animated.SetCornerRadius(radius);
            return;
        }
        SetRoundedRegion(button, radius);
    }

    public static void StyleGrid(DataGridView grid)
    {
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = Surface;
        grid.GridColor = Color.FromArgb(229, 236, 248);
        grid.RowHeadersVisible = false;
        grid.EnableHeadersVisualStyles = false;
        grid.AllowUserToResizeRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersHeight = 50;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = PrimaryDark,
            ForeColor = Color.White,
            Font = ButtonFont(),
            SelectionBackColor = PrimaryDark,
            Padding = new Padding(12, 0, 12, 0)
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Surface,
            ForeColor = Text,
            SelectionBackColor = Color.FromArgb(221, 235, 255),
            SelectionForeColor = Navy,
            Font = Font(9.5f),
            Padding = new Padding(12, 5, 12, 5)
        };
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        grid.RowTemplate.Height = 47;
        grid.RowTemplate.Resizable = DataGridViewTriState.False;
        grid.DefaultCellStyle.NullValue = "—";
    }

    public static void StyleTabs(
        TabControl tabs,
        bool compact = false,
        bool navigation = false)
    {
        tabs.Font = Font(10, FontStyle.Bold);
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.SizeMode = TabSizeMode.Fixed;
        tabs.Alignment = navigation ? TabAlignment.Left : TabAlignment.Top;
        tabs.Multiline = navigation;
        tabs.ItemSize = navigation
            ? new Size(56, 220)
            : compact
                ? new Size(145, 38)
                : new Size(150, 44);
        tabs.Padding = new Point(16, 5);
        tabs.DrawItem += (_, eventArgs) =>
        {
            var selected = eventArgs.Index == tabs.SelectedIndex;
            var bounds = eventArgs.Bounds;
            using var background = new SolidBrush(
                navigation
                    ? selected
                        ? SidebarSelected
                        : Sidebar
                    : selected
                        ? Surface
                        : Background);
            using var textBrush = new SolidBrush(
                navigation
                    ? selected
                        ? Color.White
                        : Color.FromArgb(210, 235, 255)
                    : selected
                        ? Primary
                        : Muted);
            eventArgs.Graphics.FillRectangle(background, bounds);
            if (selected)
            {
                using var accent = new SolidBrush(
                    navigation ? Color.FromArgb(168, 243, 255) : Primary);
                if (navigation)
                    eventArgs.Graphics.FillRectangle(
                        accent,
                        bounds.Left,
                        bounds.Top + 7,
                        4,
                        bounds.Height - 14);
                else
                    eventArgs.Graphics.FillRectangle(
                        accent,
                        bounds.Left,
                        bounds.Bottom - 3,
                        bounds.Width,
                        3);
            }

            TextRenderer.DrawText(
                eventArgs.Graphics,
                tabs.TabPages[eventArgs.Index].Text,
                tabs.Font,
                bounds,
                textBrush.Color,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);
        };
        tabs.SelectedIndexChanged += (_, _) => tabs.Invalidate();
    }

    public static void StyleEditor(Control editor)
    {
        editor.Font = Font(10.5f);
        editor.BackColor = Surface;
        editor.ForeColor = Text;
        editor.Margin = new Padding(0, 5, 0, 12);
    }

    public static void SetRoundedRegion(Control control, int radius)
    {
        if (control.Width <= 0 || control.Height <= 0)
            return;

        using var path = CreateRoundedPath(
            new RectangleF(0, 0, control.Width - 1, control.Height - 1),
            radius);
        var previous = control.Region;
        control.Region = new Region(path);
        previous?.Dispose();
    }

    internal static GraphicsPath CreateRoundedPath(RectangleF bounds, int radius)
    {
        var path = new GraphicsPath();
        bounds.Width = Math.Max(1, bounds.Width);
        bounds.Height = Math.Max(1, bounds.Height);
        var diameter = Math.Min(
            Math.Max(1, radius * 2f),
            Math.Min(bounds.Width, bounds.Height));
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(
            bounds.Right - diameter,
            bounds.Bottom - diameter,
            diameter,
            diameter,
            0,
            90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static Image? LoadLoginLogo() => LoadImageAsset("ne-library-logo.png");

    public static Image? LoadSidebarLogo() => LoadImageAsset("ne-library-mark.png");

    public static Icon? LoadApplicationIcon()
    {
        var iconPath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "ne-library-app.ico");
        if (File.Exists(iconPath))
            return new Icon(iconPath);

        using var image = LoadSidebarLogo();
        if (image is null)
            return null;

        using var source = new Bitmap(image);
        var visibleBounds = FindVisibleBounds(source);
        var scale = Math.Min(
            52f / visibleBounds.Width,
            52f / visibleBounds.Height);
        var iconWidth = Math.Max(1, (int)Math.Round(visibleBounds.Width * scale));
        var iconHeight = Math.Max(1, (int)Math.Round(visibleBounds.Height * scale));
        var iconBounds = new Rectangle(
            (64 - iconWidth) / 2,
            (64 - iconHeight) / 2,
            iconWidth,
            iconHeight);
        using var bitmap = new Bitmap(64, 64);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(PrimaryDark);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(
                source,
                iconBounds,
                visibleBounds,
                GraphicsUnit.Pixel);
        }

        return Icon.FromHandle(bitmap.GetHicon()).Clone() as Icon;
    }

    private static Rectangle FindVisibleBounds(Bitmap bitmap)
    {
        var left = bitmap.Width;
        var top = bitmap.Height;
        var right = -1;
        var bottom = -1;

        for (var y = 0; y < bitmap.Height; y += 2)
        for (var x = 0; x < bitmap.Width; x += 2)
        {
            if (bitmap.GetPixel(x, y).A < 16)
                continue;

            left = Math.Min(left, x);
            top = Math.Min(top, y);
            right = Math.Max(right, x);
            bottom = Math.Max(bottom, y);
        }

        return right >= left && bottom >= top
            ? Rectangle.FromLTRB(left, top, right + 1, bottom + 1)
            : new Rectangle(0, 0, bitmap.Width, bitmap.Height);
    }

    private static Image? LoadImageAsset(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        if (!File.Exists(path))
            return null;
        using var stream = File.OpenRead(path);
        using var original = Image.FromStream(stream);
        return new Bitmap(original);
    }

    public static Panel CreateBrandMark(string initials, int size)
    {
        var panel = new Panel
        {
            Width = size,
            Height = size,
            BackColor = Color.Transparent
        };
        panel.Paint += (_, eventArgs) =>
        {
            eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Primary);
            eventArgs.Graphics.FillEllipse(brush, 0, 0, size - 1, size - 1);
            TextRenderer.DrawText(
                eventArgs.Graphics,
                initials,
                Font(size * 0.26f, FontStyle.Bold),
                new Rectangle(0, 0, size, size),
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
        return panel;
    }
}

internal sealed class SurfaceCard : Panel
{
    public SurfaceCard()
    {
        DoubleBuffered = true;
        BackColor = DesktopTheme.Surface;
        Padding = new Padding(2);
        ResizeRedraw = true;
        Resize += (_, _) => DesktopTheme.SetRoundedRegion(this, 14);
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(DesktopTheme.Border, 1);
        using var path = CreateRoundedPath(
            new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3)),
            14);
        eventArgs.Graphics.DrawPath(pen, path);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class WebGradientPanel : Panel
{
    private static readonly Color StartColor = Color.FromArgb(8, 88, 188);
    private static readonly Color EndColor = Color.FromArgb(14, 145, 210);

    public WebGradientPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnPaintBackground(PaintEventArgs eventArgs)
    {
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(
            0,
            0,
            Math.Max(1, Width),
            Math.Max(1, Height));
        using var background = new LinearGradientBrush(
            bounds,
            StartColor,
            EndColor,
            LinearGradientMode.Horizontal);
        eventArgs.Graphics.FillRectangle(background, bounds);

        using var glow = new SolidBrush(Color.FromArgb(24, 190, 244, 255));
        eventArgs.Graphics.FillEllipse(
            glow,
            Width - 250,
            -95,
            330,
            330);
        eventArgs.Graphics.FillEllipse(
            glow,
            -150,
            Height - 210,
            300,
            300);
        using var ring = new Pen(Color.FromArgb(32, 255, 255, 255), 2);
        eventArgs.Graphics.DrawEllipse(
            ring,
            Width - 155,
            Height - 145,
            230,
            230);
        eventArgs.Graphics.DrawEllipse(
            ring,
            Width - 205,
            Height - 195,
            330,
            330);

        using var dot = new SolidBrush(Color.FromArgb(34, 255, 255, 255));
        for (var x = 16; x < Width; x += 22)
        for (var y = 18; y < Height; y += 22)
            eventArgs.Graphics.FillEllipse(dot, x, y, 2, 2);
    }
}

internal class AnimatedButton : Button
{
    private readonly System.Windows.Forms.Timer _animationTimer;
    private Color _normalColor;
    private Color _hoverColor;
    private Color _pressedColor;
    private Color _currentColor;
    private Color _targetColor;
    private bool _configured;
    private int _cornerRadius;
    private bool _customPaintEnabled = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool AnimationEnabled { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool CustomPaintEnabled
    {
        get => _customPaintEnabled;
        set
        {
            if (_customPaintEnabled == value)
                return;
            _customPaintEnabled = value;
            SetCornerRadius(_cornerRadius);
        }
    }

    public AnimatedButton()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor,
            true);
        DoubleBuffered = true;
        _animationTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _animationTimer.Tick += (_, _) => AnimateFrame();
    }

    public override void NotifyDefault(bool value)
    {
        // El control se dibuja completamente en OnPaint; no se usa el
        // contorno negro que WinForms agrega al botón predeterminado.
    }

    protected override bool ShowFocusCues => false;

    internal void ConfigureAnimation(
        Color normal,
        Color hover,
        Color pressed)
    {
        _normalColor = normal;
        _hoverColor = hover;
        _pressedColor = pressed;
        if (!_configured)
        {
            _configured = true;
            _currentColor = normal;
            _targetColor = normal;
            ApplyColor(normal);
            return;
        }

        if (!AnimationEnabled)
        {
            _animationTimer.Stop();
            _currentColor = normal;
            _targetColor = normal;
            ApplyColor(normal);
            return;
        }

        _targetColor = normal;
        _animationTimer.Start();
    }

    internal void SetCornerRadius(int radius)
    {
        _cornerRadius = radius;
        var previous = Region;
        Region = null;
        previous?.Dispose();
        if (!_customPaintEnabled && Width > 1 && Height > 1)
            DesktopTheme.SetRoundedRegion(this, radius);
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs eventArgs)
    {
        base.OnMouseEnter(eventArgs);
        AnimateTo(_hoverColor);
    }

    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        if (!_customPaintEnabled && _cornerRadius > 0)
            DesktopTheme.SetRoundedRegion(this, _cornerRadius);
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs eventArgs)
    {
        base.OnMouseLeave(eventArgs);
        AnimateTo(_normalColor);
    }

    protected override void OnMouseDown(MouseEventArgs eventArgs)
    {
        base.OnMouseDown(eventArgs);
        AnimateTo(_pressedColor);
    }

    protected override void OnMouseUp(MouseEventArgs eventArgs)
    {
        base.OnMouseUp(eventArgs);
        AnimateTo(ClientRectangle.Contains(eventArgs.Location)
            ? _hoverColor
            : _normalColor);
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        if (!_customPaintEnabled)
        {
            base.OnPaint(eventArgs);
            return;
        }

        eventArgs.Graphics.Clear(DesktopTheme.Surface);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        eventArgs.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        using var path = CreateButtonPath();
        var color = _configured ? _currentColor : BackColor;
        if (color.A > 0)
        {
            using var background = new SolidBrush(color);
            eventArgs.Graphics.FillPath(background, path);
        }

        var content = new Rectangle(
            Padding.Left,
            Padding.Top,
            Math.Max(0, Width - Padding.Horizontal),
            Math.Max(0, Height - Padding.Vertical));
        TextRenderer.DrawText(
            eventArgs.Graphics,
            Text,
            Font,
            content,
            Enabled ? ForeColor : Color.FromArgb(120, 132, 151),
            GetTextFlags());
    }

    protected override void OnPaintBackground(PaintEventArgs eventArgs)
    {
        if (!_customPaintEnabled)
        {
            base.OnPaintBackground(eventArgs);
            return;
        }

        var ancestor = Parent;
        while (ancestor is not null && ancestor.BackColor.A < 255)
            ancestor = ancestor.Parent;
        eventArgs.Graphics.Clear(ancestor?.BackColor ?? DesktopTheme.Surface);
    }

    protected GraphicsPath CreateButtonPath() =>
        DesktopTheme.CreateRoundedPath(
            new RectangleF(0.5f, 0.5f, Math.Max(1, Width - 1f), Math.Max(1, Height - 1f)),
            Math.Max(1, _cornerRadius));

    private TextFormatFlags GetTextFlags()
    {
        var flags = TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix;
        flags |= TextAlign switch
        {
            ContentAlignment.TopLeft or
            ContentAlignment.MiddleLeft or
            ContentAlignment.BottomLeft => TextFormatFlags.Left,
            ContentAlignment.TopRight or
            ContentAlignment.MiddleRight or
            ContentAlignment.BottomRight => TextFormatFlags.Right,
            _ => TextFormatFlags.HorizontalCenter
        };
        return flags;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _animationTimer.Dispose();
        base.Dispose(disposing);
    }

    private void AnimateTo(Color target)
    {
        if (!_configured)
            return;
        if (!AnimationEnabled)
        {
            _currentColor = target;
            ApplyColor(target);
            return;
        }
        _targetColor = target;
        _animationTimer.Start();
    }

    private void AnimateFrame()
    {
        _currentColor = Blend(_currentColor, _targetColor, 0.24f);
        ApplyColor(_currentColor);
        if (ColorDistance(_currentColor, _targetColor) > 5)
            return;
        _currentColor = _targetColor;
        ApplyColor(_currentColor);
        _animationTimer.Stop();
    }

    private void ApplyColor(Color color)
    {
        if (_customPaintEnabled)
        {
            BackColor = DesktopTheme.Surface;
            FlatAppearance.MouseOverBackColor = DesktopTheme.Surface;
            FlatAppearance.MouseDownBackColor = DesktopTheme.Surface;
        }
        else
        {
            BackColor = color;
            FlatAppearance.MouseOverBackColor = color;
            FlatAppearance.MouseDownBackColor = color;
        }
        Invalidate();
    }

    private static Color Blend(Color from, Color to, float progress) =>
        Color.FromArgb(
            Lerp(from.A, to.A, progress),
            Lerp(from.R, to.R, progress),
            Lerp(from.G, to.G, progress),
            Lerp(from.B, to.B, progress));

    private static int Lerp(int from, int to, float progress) =>
        Math.Clamp((int)Math.Round(from + (to - from) * progress), 0, 255);

    private static int ColorDistance(Color first, Color second) =>
        Math.Abs(first.A - second.A) +
        Math.Abs(first.R - second.R) +
        Math.Abs(first.G - second.G) +
        Math.Abs(first.B - second.B);
}

internal sealed class GradientButton : AnimatedButton
{
    private readonly System.Windows.Forms.Timer _gradientTimer;
    private float _hoverProgress;
    private float _targetProgress;

    public GradientButton()
    {
        DoubleBuffered = true;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        ForeColor = Color.White;
        Cursor = Cursors.Hand;
        SetCornerRadius(11);
        _gradientTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _gradientTimer.Tick += (_, _) =>
        {
            _hoverProgress += (_targetProgress - _hoverProgress) * 0.22f;
            if (Math.Abs(_targetProgress - _hoverProgress) < 0.02f)
            {
                _hoverProgress = _targetProgress;
                _gradientTimer.Stop();
            }
            Invalidate();
        };
    }

    protected override void OnMouseEnter(EventArgs eventArgs)
    {
        base.OnMouseEnter(eventArgs);
        if (!AnimationEnabled)
            return;
        _targetProgress = 1;
        _gradientTimer.Start();
    }

    protected override void OnMouseLeave(EventArgs eventArgs)
    {
        base.OnMouseLeave(eventArgs);
        if (!AnimationEnabled)
            return;
        _targetProgress = 0;
        _gradientTimer.Start();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        eventArgs.Graphics.Clear(DesktopTheme.Surface);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var start = Enabled
            ? Blend(
                DesktopTheme.Primary,
                Color.FromArgb(56, 126, 255),
                _hoverProgress)
            : Color.FromArgb(148, 163, 184);
        var end = Enabled
            ? Blend(
                DesktopTheme.PrimaryVivid,
                DesktopTheme.Cyan,
                _hoverProgress)
            : Color.FromArgb(148, 163, 184);
        using var background = new LinearGradientBrush(
            ClientRectangle,
            start,
            end,
            LinearGradientMode.Horizontal);
        using var path = CreateButtonPath();
        eventArgs.Graphics.FillPath(background, path);
        TextRenderer.DrawText(
            eventArgs.Graphics,
            Text,
            Font,
            ClientRectangle,
            ForeColor,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _gradientTimer.Dispose();
        base.Dispose(disposing);
    }

    private static Color Blend(Color from, Color to, float progress) =>
        Color.FromArgb(
            (int)(from.A + (to.A - from.A) * progress),
            (int)(from.R + (to.R - from.R) * progress),
            (int)(from.G + (to.G - from.G) * progress),
            (int)(from.B + (to.B - from.B) * progress));
}

internal sealed class OutlinedInputPanel : Panel
{
    public OutlinedInputPanel(Control input, Control? suffix = null)
    {
        DoubleBuffered = true;
        BackColor = DesktopTheme.Surface;
        Padding = suffix is null
            ? new Padding(14, 10, 14, 8)
            : new Padding(14, 10, 96, 8);
        Height = 52;
        ResizeRedraw = true;
        input.Dock = DockStyle.Fill;
        input.Margin = Padding.Empty;
        input.BackColor = DesktopTheme.Surface;
        if (input is TextBox textBox)
            textBox.BorderStyle = BorderStyle.None;
        Controls.Add(input);
        if (suffix is not null)
        {
            suffix.Dock = DockStyle.None;
            suffix.MinimumSize = Size.Empty;
            suffix.Size = new Size(72, 32);
            suffix.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            suffix.Margin = Padding.Empty;
            Controls.Add(suffix);
            suffix.BringToFront();
            void AlignSuffix() => suffix.Location = new Point(
                Math.Max(0, ClientSize.Width - suffix.Width - 12),
                Math.Max(0, (ClientSize.Height - suffix.Height) / 2));
            Resize += (_, _) => AlignSuffix();
            HandleCreated += (_, _) => AlignSuffix();
        }
        Resize += (_, _) => DesktopTheme.SetRoundedRegion(this, 10);
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var border = new Pen(DesktopTheme.Border, 1);
        using var path = CreateRoundedPath(
            new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3)),
            10);
        eventArgs.Graphics.DrawPath(border, path);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
