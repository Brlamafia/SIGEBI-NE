namespace SIGEBI.Desktop;

public sealed class LoginForm : Form
{
    private readonly ApiClient _apiClient;
    private readonly TableLayoutPanel _root = new();
    private readonly SurfaceCard _loginCard = new();
    private Control? _brandPanel;
    private Control? _loginHost;
    private readonly TextBox _email = new()
    {
        Dock = DockStyle.Fill,
        PlaceholderText = "nombre@correo.com"
    };
    private readonly TextBox _password = new()
    {
        UseSystemPasswordChar = true,
        Dock = DockStyle.Fill,
        PlaceholderText = "Escribe tu contraseña"
    };
    private readonly Button _login = new GradientButton
    {
        Text = "Iniciar sesión",
        Dock = DockStyle.Fill,
        Height = 44
    };
    private readonly Label _message = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        ForeColor = DesktopTheme.Danger,
        TextAlign = ContentAlignment.MiddleLeft
    };

    public LoginForm(ApiClient apiClient)
    {
        _apiClient = apiClient;
        _apiClient.ConfigurarBaseUrl(
            Environment.GetEnvironmentVariable("SIGEBI_API_URL")
            ?? "http://localhost:5297");

        Text = "SIGEBI - Inicio de sesión";
        ClientSize = new Size(1180, 760);
        MinimumSize = new Size(640, 560);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        WindowState = FormWindowState.Maximized;
        DesktopTheme.StyleForm(this);

        _root.Dock = DockStyle.Fill;
        _root.ColumnCount = 2;
        _root.RowCount = 1;
        _root.Margin = Padding.Empty;
        _root.Padding = Padding.Empty;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
        _brandPanel = CreateBrandPanel();
        _loginHost = CreateLoginPanel();
        _root.Controls.Add(_brandPanel, 0, 0);
        _root.Controls.Add(_loginHost, 1, 0);
        Controls.Add(_root);

        _login.Click += LoginAsync;
        AcceptButton = _login;
        Resize += (_, _) => AplicarDisenoResponsivo();
        Shown += (_, _) =>
        {
            AplicarDisenoResponsivo();
            _email.Focus();
        };
    }

    public bool Autenticado { get; private set; }
    public DesktopSession? Session { get; private set; }

    private void AplicarDisenoResponsivo()
    {
        if (_brandPanel is null || _loginHost is null)
            return;

        var compact = ClientSize.Width < 1250;
        _root.SuspendLayout();
        if (compact)
        {
            _root.ColumnStyles[0].SizeType = SizeType.Absolute;
            _root.ColumnStyles[0].Width = 0;
            _root.ColumnStyles[1].SizeType = SizeType.Percent;
            _root.ColumnStyles[1].Width = 100;
            _brandPanel.Visible = false;
        }
        else
        {
            _root.ColumnStyles[0].SizeType = SizeType.Percent;
            _root.ColumnStyles[0].Width = 54;
            _root.ColumnStyles[1].SizeType = SizeType.Percent;
            _root.ColumnStyles[1].Width = 46;
            _brandPanel.Visible = true;
        }

        var availableWidth = compact
            ? ClientSize.Width - 64
            : _loginHost.ClientSize.Width - 96;
        var availableHeight = _loginHost.ClientSize.Height - 96;
        _loginCard.Width = Math.Clamp(availableWidth, 430, compact ? 560 : 520);
        _loginCard.Height = Math.Clamp(availableHeight, 560, 650);
        _loginCard.Padding = compact
            ? new Padding(24, 18, 24, 12)
            : Padding.Empty;
        _root.ResumeLayout(true);
    }

    private static Control CreateBrandPanel()
    {
        var panel = new WebGradientPanel { Dock = DockStyle.Fill };
        var content = new TableLayoutPanel
        {
            Width = 700,
            Height = 820,
            Anchor = AnchorStyles.None,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(26, 24, 26, 18),
            BackColor = Color.Transparent
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 178));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));

        var logoImage = DesktopTheme.LoadBrandImage();
        if (logoImage is not null)
        {
            var logo = new PictureBox
            {
                Image = logoImage,
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 120, 18)
            };
            logo.Disposed += (_, _) => logoImage.Dispose();
            content.Controls.Add(logo, 0, 0);
        }
        content.Controls.Add(new Label
        {
            Text = "LIBRERÍA NUEVA ERA",
            ForeColor = Color.FromArgb(168, 243, 255),
            Font = DesktopTheme.Font(9, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 2);
        content.Controls.Add(new Label
        {
            Text = "Tu próxima historia\ncomienza aquí.",
            ForeColor = Color.White,
            Font = DesktopTheme.Font(43, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 3);
        content.Controls.Add(new Label
        {
            Text = "Gestiona solicitudes, préstamos, devoluciones e inventario\ndesde el espacio de trabajo del personal de SIGEBI.",
            ForeColor = Color.FromArgb(225, 245, 255),
            Font = DesktopTheme.Font(11.5f),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft
        }, 0, 4);
        var quoteMark = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        quoteMark.Paint += (_, eventArgs) =>
        {
            using var line = new Pen(Color.FromArgb(90, 211, 243));
            eventArgs.Graphics.DrawLine(line, 0, 1, quoteMark.Width - 120, 1);
        };
        quoteMark.Controls.Add(new Label
        {
            Text = "“",
            ForeColor = Color.FromArgb(190, 239, 255),
            Font = DesktopTheme.Font(24, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        });
        content.Controls.Add(quoteMark, 0, 6);
        content.Controls.Add(new Label
        {
            Text = "Leer es encontrar una puerta donde antes había una pared.\nSIGEBI · Nueva Era",
            ForeColor = Color.White,
            Font = DesktopTheme.Font(9.5f, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(0, 8, 0, 0)
        }, 0, 7);
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        host.Controls.Add(content, 0, 0);
        panel.Controls.Add(host);
        return panel;
    }

    private Control CreateLoginPanel()
    {
        _email.Font = DesktopTheme.Font(11);
        _email.BorderStyle = BorderStyle.None;
        _email.BackColor = DesktopTheme.Surface;
        _email.ForeColor = DesktopTheme.Text;
        _password.Font = DesktopTheme.Font(11);
        _password.BorderStyle = BorderStyle.None;
        _password.BackColor = DesktopTheme.Surface;
        _password.ForeColor = DesktopTheme.Text;
        DesktopTheme.StylePrimaryButton(_login);
        _login.FlatAppearance.BorderSize = 0;
        _login.TabStop = false;

        var showPassword = new AnimatedButton
        {
            Text = "Mostrar",
            FlatStyle = FlatStyle.Flat,
            ForeColor = DesktopTheme.Primary,
            BackColor = DesktopTheme.Surface,
            Font = DesktopTheme.Font(9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        DesktopTheme.StyleTextButton(showPassword);
        showPassword.Click += (_, _) =>
        {
            _password.UseSystemPasswordChar = !_password.UseSystemPasswordChar;
            showPassword.Text = _password.UseSystemPasswordChar
                ? "Mostrar"
                : "Ocultar";
        };
        var emailHost = new OutlinedInputPanel(_email) { Dock = DockStyle.Fill };
        var passwordHost = new OutlinedInputPanel(_password, showPassword)
        {
            Dock = DockStyle.Fill
        };

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 12,
            BackColor = Color.Transparent,
            Padding = new Padding(18, 18, 18, 12),
            Margin = Padding.Empty
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.Controls.Add(new Label
        {
            Text = "BIENVENIDO",
            Font = DesktopTheme.Font(8.5f, FontStyle.Bold),
            ForeColor = DesktopTheme.Cyan,
            AutoSize = true
        }, 0, 0);
        content.Controls.Add(new Label
        {
            Text = "Inicia sesión",
            Font = DesktopTheme.Font(26, FontStyle.Bold),
            ForeColor = DesktopTheme.Navy,
            AutoSize = true
        }, 0, 1);
        content.Controls.Add(new Label
        {
            Text = "Accede con tus credenciales institucionales de empleado.",
            ForeColor = DesktopTheme.Muted,
            Font = DesktopTheme.Font(10),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft
        }, 0, 2);
        content.Controls.Add(FieldLabel("Correo electrónico"), 0, 4);
        content.Controls.Add(emailHost, 0, 5);
        content.Controls.Add(FieldLabel("Contraseña"), 0, 6);
        content.Controls.Add(passwordHost, 0, 7);
        content.Controls.Add(new Label
        {
            Text = "Acceso interno · Personal autorizado",
            Dock = DockStyle.Fill,
            ForeColor = DesktopTheme.Muted,
            Font = DesktopTheme.Font(8.5f),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 8);
        _login.Text = "Iniciar sesión";
        content.Controls.Add(_login, 0, 9);
        content.Controls.Add(_message, 0, 10);
        content.Controls.Add(new Label
        {
            Text = "Sistema Integral de Gestión Bibliotecaria · Nueva Era",
            Dock = DockStyle.Fill,
            ForeColor = DesktopTheme.Muted,
            Font = DesktopTheme.Font(8.5f),
            TextAlign = ContentAlignment.BottomCenter
        }, 0, 11);

        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = DesktopTheme.Background,
            ColumnCount = 1,
            RowCount = 1,
            Padding = new Padding(36)
        };
        _loginCard.Width = 500;
        _loginCard.Height = 620;
        _loginCard.Anchor = AnchorStyles.None;
        _loginCard.BackColor = DesktopTheme.Surface;
        _loginCard.Padding = Padding.Empty;
        _loginCard.Controls.Add(content);
        host.Controls.Add(_loginCard, 0, 0);
        return host;
    }

    private static Label FieldLabel(string text) => new()
    {
        Text = text,
        Font = DesktopTheme.Font(9.5f, FontStyle.Bold),
        ForeColor = DesktopTheme.Text,
        AutoSize = true,
        Anchor = AnchorStyles.Left
    };

    private async void LoginAsync(object? sender, EventArgs e)
    {
        _message.Text = string.Empty;
        if (string.IsNullOrWhiteSpace(_email.Text) ||
            string.IsNullOrWhiteSpace(_password.Text))
        {
            _message.Text = "Escribe tu correo y contraseña.";
            return;
        }

        try
        {
            _login.Enabled = false;
            _login.Text = "Conectando…";
            UseWaitCursor = true;
            var session = await _apiClient.IniciarSesionAsync(
                _email.Text.Trim(),
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
            _message.Text = ObtenerMensajeInicioSesion(exception);
            _password.SelectAll();
            _password.Focus();
        }
        finally
        {
            UseWaitCursor = false;
            _login.Enabled = true;
            _login.Text = "Iniciar sesión";
        }
    }

    private static string ObtenerMensajeInicioSesion(Exception exception)
    {
        if (exception is TaskCanceledException)
            return "La conexión tardó demasiado. Verifica que la API esté encendida.";

        if (exception is HttpRequestException &&
            (exception.Message.Contains("actively refused", StringComparison.OrdinalIgnoreCase) ||
             exception.Message.Contains("No connection could be made", StringComparison.OrdinalIgnoreCase) ||
             exception.InnerException is System.Net.Sockets.SocketException))
            return "No pudimos conectar con SIGEBI. Confirma que la API esté encendida.";

        return string.IsNullOrWhiteSpace(exception.Message)
            ? "No pudimos iniciar sesión. Revisa tus datos e inténtalo nuevamente."
            : exception.Message.Trim();
    }
}
