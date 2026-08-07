namespace SIGEBI.Desktop;

public sealed record DetailItem(string Label, string Value);

/// <summary>
/// Ventana de solo lectura para consultar una fila sin exponer acciones de edición.
/// </summary>
public sealed class DetailsDialog : Form
{
    public DetailsDialog(string title, IReadOnlyCollection<DetailItem> details)
    {
        Text = title;
        var rows = (int)Math.Ceiling(details.Count / 2d);
        ClientSize = new Size(820, Math.Min(760, Math.Max(340, 190 + rows * 64)));
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        DesktopTheme.StyleForm(this);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = DesktopTheme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));

        root.Controls.Add(CrearEncabezado(title), 0, 0);
        root.Controls.Add(CrearContenido(details), 0, 1);
        root.Controls.Add(CrearPie(), 0, 2);
        Controls.Add(root);
    }

    private static Control CrearEncabezado(string title)
    {
        var header = new WebGradientPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(30, 20, 24, 14)
        };
        header.Controls.Add(new Label
        {
            Text = "Consulta la información completa del registro seleccionado.",
            Dock = DockStyle.Bottom,
            Height = 24,
            ForeColor = Color.FromArgb(220, 245, 255),
            Font = DesktopTheme.Font(9.5f),
            BackColor = Color.Transparent
        });
        header.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 38,
            ForeColor = Color.White,
            Font = DesktopTheme.TitleFont(18),
            BackColor = Color.Transparent
        });
        return header;
    }

    private static Control CrearContenido(IEnumerable<DetailItem> details)
    {
        var values = details.ToArray();
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 2,
            BackColor = DesktopTheme.Background,
            Padding = new Padding(30, 18, 30, 12),
            Margin = Padding.Empty
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        for (var index = 0; index < values.Length; index++)
        {
            var row = index / 2;
            if (index % 2 == 0)
                content.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            content.Controls.Add(CrearCampo(values[index]), index % 2, row);
        }

        return content;
    }

    private static Control CrearCampo(DetailItem detail)
    {
        var field = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0, 0, 12, 0),
            Margin = Padding.Empty,
            BackColor = Color.Transparent
        };
        field.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        field.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        field.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        field.Controls.Add(new Label
        {
            Text = detail.Label,
            Dock = DockStyle.Fill,
            ForeColor = DesktopTheme.Text,
            Font = DesktopTheme.Font(9F, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 0);
        field.Controls.Add(new Label
        {
            Text = detail.Value,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Padding = new Padding(12, 7, 12, 0),
            BackColor = DesktopTheme.Surface,
            ForeColor = DesktopTheme.Text,
            Font = DesktopTheme.Font(9.5F),
            BorderStyle = BorderStyle.FixedSingle
        }, 0, 1);
        return field;
    }

    private Control CrearPie()
    {
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(24, 16, 30, 14),
            BackColor = DesktopTheme.Background
        };
        var close = new AnimatedButton
        {
            Text = "Cerrar",
            DialogResult = DialogResult.OK,
            AutoSize = true
        };
        DesktopTheme.StylePrimaryButton(close);
        footer.Controls.Add(close);
        AcceptButton = close;
        CancelButton = close;
        return footer;
    }
}
