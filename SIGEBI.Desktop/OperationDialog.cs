using System.Globalization;

namespace SIGEBI.Desktop;

public enum InputKind
{
    Text,
    Integer,
    Decimal,
    DateTime
}

public sealed record InputField(
    string Name,
    string Label,
    InputKind Kind = InputKind.Text,
    string DefaultValue = "");

public sealed class OperationDialog : Form
{
    private readonly IReadOnlyCollection<InputField> _fields;
    private readonly Dictionary<string, TextBox> _inputs = new();

    public OperationDialog(string title, params InputField[] fields)
    {
        _fields = fields;
        Text = title;
        Width = 480;
        Height = Math.Max(230, 125 + fields.Length * 58);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = fields.Length + 1,
            AutoSize = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

        for (var index = 0; index < fields.Length; index++)
        {
            var field = fields[index];
            var input = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = field.DefaultValue
            };
            _inputs[field.Name] = input;
            layout.Controls.Add(new Label
            {
                Text = field.Label,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, 0, index);
            layout.Controls.Add(input, 1, index);
        }

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true
        };
        var accept = new Button { Text = "Aceptar", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, AutoSize = true };
        accept.Click += (_, _) =>
        {
            try
            {
                _ = ObtenerValores();
            }
            catch (Exception exception)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(exception.Message, "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        buttons.Controls.Add(accept);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 0, fields.Length);
        layout.SetColumnSpan(buttons, 2);
        Controls.Add(layout);
        AcceptButton = accept;
        CancelButton = cancel;
    }

    public Dictionary<string, object?> ObtenerValores()
    {
        var values = new Dictionary<string, object?>();
        foreach (var field in _fields)
        {
            var text = _inputs[field.Name].Text.Trim();
            values[field.Name] = field.Kind switch
            {
                InputKind.Integer when int.TryParse(text, out var value) => value,
                InputKind.Decimal when decimal.TryParse(
                    text,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var value) => value,
                InputKind.DateTime when DateTime.TryParse(text, out var value) => value,
                InputKind.Text => text,
                _ => throw new ArgumentException($"El valor de «{field.Label}» no es válido.")
            };
        }
        return values;
    }
}
