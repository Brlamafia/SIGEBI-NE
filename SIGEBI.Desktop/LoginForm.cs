namespace SIGEBI.Desktop;

public sealed class LoginForm : Form
{
    private readonly ApiClient _apiClient;
    private readonly TextBox _url = new()
    {
        Text = Environment.GetEnvironmentVariable("SIGEBI_API_URL")
            ?? "http://localhost:5297",
        Dock = DockStyle.Fill
    };
    private readonly TextBox _email = new() { Dock = DockStyle.Fill };
    private readonly TextBox _password = new() { UseSystemPasswordChar = true, Dock = DockStyle.Fill };
    private readonly Button _login = new() { Text = "Entrar a SIGEBI", Dock = DockStyle.Fill, Height = 38 };

    public LoginForm(ApiClient apiClient)
    {
        _apiClient = apiClient;
        Text = "SIGEBI - Inicio de sesión";
        Width = 470;
        Height = 330;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var title = new Label
        {
            Text = "NE Library",
            Font = new Font(Font.FontFamily, 22, FontStyle.Bold),
            AutoSize = true,
            Anchor = AnchorStyles.None
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28),
            ColumnCount = 2,
            RowCount = 5
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
        layout.Controls.Add(title, 0, 0);
        layout.SetColumnSpan(title, 2);
        AddRow(layout, 1, "URL de la API", _url);
        AddRow(layout, 2, "Correo", _email);
        AddRow(layout, 3, "Contraseña", _password);
        layout.Controls.Add(_login, 0, 4);
        layout.SetColumnSpan(_login, 2);
        Controls.Add(layout);

        _login.Click += LoginAsync;
        AcceptButton = _login;
    }

    public bool Autenticado { get; private set; }
    public DesktopSession? Session { get; private set; }

    private async void LoginAsync(object? sender, EventArgs e)
    {
        try
        {
            _login.Enabled = false;
            _login.Text = "Conectando...";
            _apiClient.ConfigurarBaseUrl(_url.Text);
            var session = await _apiClient.IniciarSesionAsync(
                _email.Text,
                _password.Text);
            if (!session.PuedeUsarDesktop)
            {
                _apiClient.CerrarSesion();
                throw new UnauthorizedAccessException(
                    "Esta aplicación es exclusiva para administradores, bibliotecarios y auditores.");
            }
            Session = session;
            Autenticado = true;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "No se pudo iniciar sesión", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _login.Enabled = true;
            _login.Text = "Entrar a SIGEBI";
        }
    }

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control input)
    {
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        layout.Controls.Add(input, 1, row);
    }
}
