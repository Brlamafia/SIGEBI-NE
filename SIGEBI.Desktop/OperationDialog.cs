using System.Globalization;

namespace SIGEBI.Desktop;

public enum InputKind
{
    Text,
    Password,
    Integer,
    Decimal,
    DateTime,
    Select
}

public sealed record InputField(
    string Name,
    string Label,
    InputKind Kind = InputKind.Text,
    string DefaultValue = "",
    IReadOnlyList<string>? Options = null);

public sealed class OperationDialog : Form
{
    private readonly IReadOnlyCollection<InputField> _fields;
    private readonly Dictionary<string, Control> _inputs = new();

    public OperationDialog(string title, params InputField[] fields)
    {
        _fields = fields;
        Text = title;
        ClientSize = new Size(
            620,
            Math.Min(820, Math.Max(390, 270 + fields.Length * 72)));
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        DesktopTheme.StyleForm(this);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = DesktopTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));

        root.Controls.Add(CreateHeader(title), 0, 0);
        root.Controls.Add(CreateFields(), 0, 1);

        var (footer, accept, cancel) = CreateFooter();
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);

        accept.Click += (_, _) =>
        {
            try
            {
                _ = ObtenerValores();
            }
            catch (Exception exception)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(
                    this,
                    $"Hay un dato que necesita corrección:\n\n{exception.Message}\n\nRevísalo y vuelve a intentarlo.",
                    "No se puede guardar todavía",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        };
        AcceptButton = accept;
        CancelButton = cancel;
    }

    private static Control CreateHeader(string title)
    {
        var panel = new WebGradientPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(32, 22, 24, 16)
        };
        panel.Controls.Add(new Label
        {
            Text = "Completa los datos y revisa la información antes de guardar.",
            Dock = DockStyle.Bottom,
            Height = 30,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(220, 245, 255),
            Font = DesktopTheme.Font(9.5f)
        });
        panel.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 38,
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            Font = DesktopTheme.TitleFont(18)
        });
        return panel;
    }

    private Control CreateFields()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(34, 24, 34, 16),
            ColumnCount = 1,
            RowCount = Math.Max(1, _fields.Count * 2),
            AutoScroll = true,
            BackColor = DesktopTheme.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var row = 0;
        foreach (var field in _fields)
        {
            var input = CrearEditor(field);
            _inputs[field.Name] = input;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            layout.Controls.Add(new Label
            {
                Text = field.Label,
                AutoSize = true,
                ForeColor = DesktopTheme.Text,
                Font = DesktopTheme.Font(9.5f, FontStyle.Bold),
                Anchor = AnchorStyles.Left
            }, 0, row++);
            layout.Controls.Add(input, 0, row++);
        }
        return layout;
    }

    private static Control CrearEditor(InputField field)
    {
        Control editor;
        if (field.Kind == InputKind.DateTime)
        {
            var picker = new DateTimePicker
            {
                Dock = DockStyle.Top,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy  HH:mm",
                Height = 38
            };
            if (DateTime.TryParse(field.DefaultValue, out var date))
                picker.Value = date;
            editor = picker;
        }
        else if (field.Kind is InputKind.Integer or InputKind.Decimal)
        {
            var number = new NumericUpDown
            {
                Dock = DockStyle.Top,
                Height = 38,
                Minimum = 0,
                Maximum = 1_000_000_000,
                DecimalPlaces = field.Kind == InputKind.Decimal ? 2 : 0,
                ThousandsSeparator = true
            };
            if (decimal.TryParse(
                    field.DefaultValue,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var value) &&
                value >= number.Minimum &&
                value <= number.Maximum)
                number.Value = value;
            editor = number;
        }
        else if (field.Kind == InputKind.Select)
        {
            var select = new ComboBox
            {
                Dock = DockStyle.Top,
                Height = 38,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            if (field.Options is not null)
                select.Items.AddRange(field.Options.Cast<object>().ToArray());
            var selectedIndex = select.FindStringExact(field.DefaultValue);
            select.SelectedIndex = selectedIndex >= 0
                ? selectedIndex
                : select.Items.Count > 0 ? 0 : -1;
            editor = select;
        }
        else
        {
            var text = field.Kind == InputKind.Password
                ? new PasswordTextBox()
                : new TextBox();
            text.Dock = DockStyle.Top;
            text.Text = field.DefaultValue;
            text.PlaceholderText = field.Label;
            DesktopTheme.StyleInput(text);
            editor = text;
        }

        DesktopTheme.StyleEditor(editor);
        return editor;
    }

    private static (Control Footer, Button Accept, Button Cancel) CreateFooter()
    {
        var footer = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 20, 30, 18),
            WrapContents = false,
            BackColor = DesktopTheme.Background
        };
        var accept = new AnimatedButton
        {
            Text = "Guardar cambios",
            DialogResult = DialogResult.OK,
            AutoSize = true
        };
        var cancel = new AnimatedButton
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            AutoSize = true
        };
        DesktopTheme.StylePrimaryButton(accept);
        DesktopTheme.StyleSecondaryButton(cancel);
        accept.Margin = new Padding(8, 0, 0, 0);
        footer.Controls.Add(accept);
        footer.Controls.Add(cancel);
        return (footer, accept, cancel);
    }

    public Dictionary<string, object?> ObtenerValores()
    {
        var values = new Dictionary<string, object?>();
        foreach (var field in _fields)
        {
            var input = _inputs[field.Name];
            var text = input.Text.Trim();
            values[field.Name] = field.Kind switch
            {
                InputKind.Integer when input is NumericUpDown number =>
                    decimal.ToInt32(number.Value),
                InputKind.Decimal when input is NumericUpDown number => number.Value,
                InputKind.DateTime when input is DateTimePicker picker =>
                    NormalizarFechaUtc(picker.Value),
                InputKind.Select when input is ComboBox select &&
                    select.SelectedItem is not null => select.SelectedItem.ToString(),
                InputKind.Password when !string.IsNullOrWhiteSpace(text) => text,
                InputKind.Text => text,
                _ => throw new ArgumentException(
                    $"El valor de «{field.Label}» no es válido.")
            };
        }
        return values;
    }

    private static DateTime NormalizarFechaUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
        };
}
