using System.Data;
using System.Text.Json;

namespace SIGEBI.Desktop;

public sealed class MainForm : Form
{
    private sealed record DeferredModule(Func<TabPage> Factory);

    // Mantiene los datos ya consultados al navegar entre módulos. Cualquier alta,
    // cambio o eliminación invalida este caché desde ApiClient.
    private static readonly TimeSpan ModuleRefreshInterval = TimeSpan.FromMinutes(5);
    private readonly ApiClient _api;
    private readonly DesktopSession _session;
    private readonly Task _inicioWarmupTask;
    private readonly Dictionary<TabPage, DateTime> _moduleLoadedAt = [];
    private readonly HashSet<TabPage> _modulesLoading = [];
    private readonly Font _gridStatusFont = DesktopTheme.Font(9.5f, FontStyle.Bold);
    private readonly Font _emptyGridFont = DesktopTheme.Font(11);
    private readonly Label _status = new()
    {
        Text = "Todo listo para trabajar",
        Dock = DockStyle.Fill,
        ForeColor = DesktopTheme.Muted,
        Font = DesktopTheme.Font(9),
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true
    };
    private readonly ToolTip _toolTips = new()
    {
        AutoPopDelay = 8000,
        InitialDelay = 350,
        ReshowDelay = 150,
        ShowAlways = true
    };

    public MainForm(ApiClient api, DesktopSession session)
    {
        _api = api;
        _session = session;
        Text = "SIGEBI · Gestión bibliotecaria";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1100, 700);
        DesktopTheme.StyleForm(this);
        Icon = DesktopTheme.LoadApplicationIcon() ?? Icon;
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        var tabs = new BufferedTabControl
        {
            Dock = DockStyle.Fill,
            Appearance = TabAppearance.FlatButtons,
            SizeMode = TabSizeMode.Fixed,
            ItemSize = new Size(0, 1),
            Multiline = true,
            Margin = Padding.Empty,
            Padding = Point.Empty
        };
        tabs.TabPages.Add(CrearModuloDiferido("Inicio", CrearInicio));
        if (session.TieneRol("Administrador") || session.TieneRol("Bibliotecario"))
        {
            tabs.TabPages.Add(CrearModuloDiferido("Solicitudes", CrearSolicitudes));
            tabs.TabPages.Add(CrearModuloDiferido("Pr\u00E9stamos", CrearPrestamos));
            tabs.TabPages.Add(CrearModuloDiferido("Devoluciones", CrearDevoluciones));
            tabs.TabPages.Add(CrearModuloDiferido("Cat\u00E1logo", CrearCatalogo));
            tabs.TabPages.Add(CrearModuloDiferido("Inventario", CrearInventario));
            tabs.TabPages.Add(CrearModuloDiferido("Multas", CrearMultas));
        }
        if (session.TieneRol("Administrador") || session.TieneRol("Auditor"))
        {
            tabs.TabPages.Add(CrearModuloDiferido("Auditor\u00EDa", CrearAuditoria));
            tabs.TabPages.Add(CrearModuloDiferido("Reportes", CrearReportes));
        }
        if (session.TieneRol("Administrador"))
            tabs.TabPages.Add(CrearModuloDiferido("Administraci\u00F3n", CrearAdministracion));

        var logout = new AnimatedButton
        {
            Text = "↪  Cerrar sesión",
            Dock = DockStyle.Fill,
            Margin = new Padding(18, 8, 18, 16)
        };
        DesktopTheme.StyleNavigationButton(logout, false);
        logout.Font = DesktopTheme.Font(9.5f);
        logout.Padding = new Padding(12, 0, 8, 0);
        logout.Click += (_, _) =>
        {
            _api.CerrarSesion();
            CerrarSesionSolicitado = true;
            Hide();
            Close();
        };

        var sidebar = CrearBarraLateral(tabs, logout);
        var moduleTitle = new Label
        {
            Text = tabs.TabPages.Count > 0 ? tabs.TabPages[0].Text : "SIGEBI",
            Dock = DockStyle.Fill,
            ForeColor = DesktopTheme.Navy,
            Font = DesktopTheme.TitleFont(18),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var header = CrearEncabezado(moduleTitle);

        var statusFooter = CrearPieEstado();

        var root = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = DesktopTheme.Background
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 282));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.Controls.Add(sidebar, 0, 0);
        root.SetRowSpan(sidebar, 3);
        root.Controls.Add(header, 1, 0);
        root.Controls.Add(tabs, 1, 1);
        root.Controls.Add(statusFooter, 1, 2);
        Controls.Add(root);
        // Arranca la consulta compacta antes de que el usuario seleccione Inicio.
        // GetAsync también reúne solicitudes idénticas que ya estén en curso.
        _inicioWarmupTask = CalentarInicioAsync();
        _toolTips.SetToolTip(logout, "Finaliza la sesión actual de forma segura.");
        tabs.SelectedIndexChanged += (_, _) =>
        {
            moduleTitle.Text = tabs.SelectedTab?.Text ?? "SIGEBI";
        };
        Shown += async (_, _) =>
        {
            tabs.ItemSize = new Size(0, 1);
            await CargarPrimeraPestanaAsync(tabs);
            _ = PrepararModulosEnSegundoPlanoAsync(tabs);
        };
    }

    public bool CerrarSesionSolicitado { get; private set; }

    private Control CrearBarraLateral(TabControl tabs, Button logout)
    {
        var sidebar = new WebGradientPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 24, 18, 0)
        };
        var layout = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));

        layout.Controls.Add(CrearMarcaLateral(), 0, 0);
        var navigation = new BufferedFlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0),
            Margin = Padding.Empty
        };

        var buttons = new List<Button>();
        Button? activeButton = null;
        for (var index = 0; index < tabs.TabPages.Count; index++)
        {
            var pageIndex = index;
            var button = new AnimatedButton
            {
                Text = $"{ObtenerIconoModulo(tabs.TabPages[index].Text)}   {tabs.TabPages[index].Text}",
                Width = 226,
                Margin = new Padding(0, 3, 0, 3),
                Tag = pageIndex
            };
            DesktopTheme.StyleNavigationButton(button, index == 0);
            button.Click += (_, _) =>
            {
                if (ReferenceEquals(activeButton, button))
                    return;
                if (activeButton is not null)
                    DesktopTheme.StyleNavigationButton(activeButton, false);
                tabs.SelectedIndex = pageIndex;
                DesktopTheme.StyleNavigationButton(button, true);
                activeButton = button;
            };
            buttons.Add(button);
            navigation.Controls.Add(button);
            if (index == 0)
                activeButton = button;
        }

        void AdjustNavigation()
        {
            if (buttons.Count == 0)
                return;
            var availableHeight = Math.Max(
                0,
                navigation.ClientSize.Height - navigation.Padding.Vertical);
            var itemHeight = Math.Clamp(
                availableHeight / buttons.Count - 6,
                38,
                50);
            var itemWidth = Math.Max(150, navigation.ClientSize.Width - 4);
            foreach (var button in buttons)
            {
                button.Width = itemWidth;
                button.Height = itemHeight;
            }
        }
        navigation.Resize += (_, _) => AdjustNavigation();
        navigation.HandleCreated += (_, _) => AdjustNavigation();

        layout.Controls.Add(navigation, 0, 1);
        layout.Controls.Add(logout, 0, 2);
        sidebar.Controls.Add(layout);
        return sidebar;
    }

    private static Control CrearMarcaLateral()
    {
        var content = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Padding = new Padding(6, 0, 6, 10)
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var logoImage = DesktopTheme.LoadSidebarLogo();
        if (logoImage is not null)
        {
            var logo = new PictureBox
            {
                Image = logoImage,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            logo.Disposed += (_, _) => logoImage.Dispose();
            content.Controls.Add(logo, 0, 0);
        }
        else
        {
            content.Controls.Add(new Label
            {
                Text = "SIGEBI · NEW ERA",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = DesktopTheme.TitleFont(17),
                TextAlign = ContentAlignment.MiddleCenter
            }, 0, 0);
        }
        content.Controls.Add(new Label
        {
            Text = "GESTIÓN DEL PERSONAL",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(205, 242, 255),
            Font = DesktopTheme.EyebrowFont(8),
            TextAlign = ContentAlignment.MiddleCenter
        }, 0, 1);
        return content;
    }

    private Control CrearEncabezado(Label moduleTitle)
    {
        var header = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = DesktopTheme.Surface,
            Padding = new Padding(28, 9, 26, 8),
            Margin = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 255));

        var heading = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        heading.Controls.Add(new Label
        {
            Text = "SIGEBI  ·  PANEL DEL PERSONAL",
            Dock = DockStyle.Fill,
            ForeColor = DesktopTheme.Primary,
            Font = DesktopTheme.EyebrowFont(8),
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 0);
        heading.Controls.Add(moduleTitle, 0, 1);
        header.Controls.Add(heading, 0, 0);

        var firstInitial = _session.Usuario.Nombre
            .Trim()
            .FirstOrDefault();
        var lastInitial = _session.Usuario.Apellido
            .Trim()
            .FirstOrDefault();
        var initials = string.Concat(
            firstInitial == default ? 'S' : firstInitial,
            lastInitial == default ? 'N' : lastInitial)
            .ToUpperInvariant();
        var avatar = DesktopTheme.CreateBrandMark(initials, 42);
        avatar.Anchor = AnchorStyles.None;
        header.Controls.Add(avatar, 1, 0);
        header.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = $"{_session.Usuario.Nombre} {_session.Usuario.Apellido}\n{string.Join(" · ", _session.Roles)}",
            ForeColor = DesktopTheme.Text,
            Font = DesktopTheme.LabelFont(),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            AutoEllipsis = true
        }, 2, 0);
        return header;
    }

    private Control CrearPieEstado()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = DesktopTheme.Surface,
            Padding = new Padding(24, 4, 24, 4),
            Margin = Padding.Empty
        };
        footer.Paint += (_, eventArgs) =>
        {
            using var border = new Pen(DesktopTheme.Border);
            eventArgs.Graphics.DrawLine(border, 0, 0, footer.Width, 0);
        };
        var layout = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 245));
        layout.Controls.Add(new Label
        {
            Text = "●",
            Dock = DockStyle.Fill,
            ForeColor = DesktopTheme.Success,
            Font = DesktopTheme.Font(8),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        layout.Controls.Add(_status, 1, 0);
        var connectionBadge = new Label
        {
            Text = "API CONECTADA  ·  SUPABASE",
            Dock = DockStyle.Fill,
            ForeColor = DesktopTheme.PrimaryDark,
            BackColor = DesktopTheme.PrimarySoft,
            Font = DesktopTheme.EyebrowFont(8),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(8, 1, 0, 1)
        };
        connectionBadge.Resize += (_, _) =>
            DesktopTheme.SetRoundedRegion(connectionBadge, 10);
        layout.Controls.Add(connectionBadge, 2, 0);
        footer.Controls.Add(layout);
        return footer;
    }

    private static string ObtenerIconoModulo(string module) =>
        module switch
        {
            "Inicio" => "⌂",
            "Solicitudes" => "◇",
            "Préstamos" => "▤",
            "Devoluciones" => "↩",
            "Catálogo" => "⌕",
            "Inventario" => "▦",
            "Multas" => "!",
            "Auditoría" => "◎",
            "Reportes" => "▥",
            "Administración" => "⚙",
            _ => "•"
        };

    private TabPage CrearInicio()
    {
        var page = new TabPage("Inicio")
        {
            BackColor = DesktopTheme.Background,
            Padding = Padding.Empty
        };
        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = DesktopTheme.Background,
            Padding = new Padding(28, 22, 28, 22)
        };
        var content = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 760,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = DesktopTheme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 224));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var pendingHero = CrearEtiquetaDashboard("—", 34, FontStyle.Bold, Color.White);
        var hero = CrearHeroInicio(pendingHero);
        content.Controls.Add(hero, 0, 0);

        var pendingValue = CrearEtiquetaDashboard("—", 24, FontStyle.Bold, DesktopTheme.Navy);
        var activeValue = CrearEtiquetaDashboard("—", 24, FontStyle.Bold, DesktopTheme.Navy);
        var finesValue = CrearEtiquetaDashboard("—", 24, FontStyle.Bold, DesktopTheme.Navy);
        var metrics = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 8)
        };
        metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334f));
        metrics.Controls.Add(CrearTarjetaMetrica(
            "SOLICITUDES PENDIENTES",
            "Esperan revisión del personal",
            pendingValue,
            DesktopTheme.Primary), 0, 0);
        metrics.Controls.Add(CrearTarjetaMetrica(
            "PRÉSTAMOS ACTIVOS",
            "Materiales fuera de biblioteca",
            activeValue,
            DesktopTheme.PrimaryVivid), 1, 0);
        metrics.Controls.Add(CrearTarjetaMetrica(
            "MULTAS PENDIENTES",
            "Monto pendiente de resolución",
            finesValue,
            DesktopTheme.Cyan), 2, 0);
        content.Controls.Add(metrics, 0, 1);

        var donut = new DashboardDonut { Dock = DockStyle.Fill };
        var distributionTotal = CrearEtiquetaDashboard("0 movimientos", 8, FontStyle.Bold, DesktopTheme.PrimaryDark);
        var pendingBar = new DashboardProgressBar
        {
            Dock = DockStyle.Fill,
            BarColor = Color.FromArgb(148, 163, 184)
        };
        var activeBar = new DashboardProgressBar
        {
            Dock = DockStyle.Fill,
            BarColor = DesktopTheme.Primary
        };
        var overdueBar = new DashboardProgressBar
        {
            Dock = DockStyle.Fill,
            BarColor = DesktopTheme.Danger
        };
        var pendingCount = CrearEtiquetaDashboard("0", 9, FontStyle.Bold, DesktopTheme.Text);
        var activeCount = CrearEtiquetaDashboard("0", 9, FontStyle.Bold, DesktopTheme.Text);
        var overdueCount = CrearEtiquetaDashboard("0", 9, FontStyle.Bold, DesktopTheme.Text);
        var insights = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 10)
        };
        insights.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        insights.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        insights.Controls.Add(CrearTarjetaAtencion(donut), 0, 0);
        insights.Controls.Add(CrearTarjetaDistribucion(
            distributionTotal,
            pendingBar,
            pendingCount,
            activeBar,
            activeCount,
            overdueBar,
            overdueCount), 1, 0);
        content.Controls.Add(insights, 0, 2);

        var activityTitle = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        activityTitle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        activityTitle.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        var titleStack = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        titleStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        titleStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        titleStack.Controls.Add(CrearEtiquetaDashboard(
            "Actividad reciente",
            15,
            FontStyle.Bold,
            DesktopTheme.Navy), 0, 0);
        titleStack.Controls.Add(CrearEtiquetaDashboard(
            "Últimas solicitudes registradas por los usuarios.",
            9,
            FontStyle.Regular,
            DesktopTheme.Muted), 0, 1);
        activityTitle.Controls.Add(titleStack, 0, 0);
        var requestsButton = new AnimatedButton
        {
            Text = "Ver solicitudes",
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Size = new Size(152, 40),
            Margin = new Padding(18, 4, 0, 0)
        };
        DesktopTheme.StylePrimaryButton(requestsButton);
        requestsButton.Click += (_, _) => IrAModulo(requestsButton, "Solicitudes");
        requestsButton.Visible =
            _session.TieneRol("Administrador") ||
            _session.TieneRol("Bibliotecario");
        activityTitle.Controls.Add(requestsButton, 1, 0);
        content.Controls.Add(activityTitle, 0, 3);

        var activityList = new BufferedFlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        activityList.Resize += (_, _) =>
        {
            foreach (Control item in activityList.Controls)
                item.Width = Math.Max(100, activityList.ClientSize.Width - 2);
        };
        content.Controls.Add(activityList, 0, 4);
        scroll.Controls.Add(content);
        page.Controls.Add(scroll);

        page.Tag = new Func<Task>(async () =>
        {
            _status.Text = "Actualizando el resumen operativo…";
            if (!_session.TieneRol("Administrador") && !_session.TieneRol("Bibliotecario"))
            {
                pendingHero.Text = "0";
                pendingValue.Text = "0";
                activeValue.Text = "0";
                finesValue.Text = 0m.ToString("C2");
                donut.Percentage = 0;
                RenderizarActividad(activityList, []);
                _status.Text = "Inicio listo · no hay indicadores operativos para este perfil";
                return;
            }
            await _inicioWarmupTask;
            var summary = await _api.GetAsync("api/ResumenInicio");
            var requests = LeerObjetos(summary, "actividadReciente");
            var pending = LeerEntero(summary, "solicitudesPendientes");
            var activeLoans = LeerEntero(summary, "prestamosActivos");
            var overdueLoans = LeerEntero(summary, "prestamosVencidos");
            var attentionPercentage = LeerEntero(summary, "porcentajeAtencion");
            var fineAmount = LeerDecimal(summary, "montoMultasPendientes");

            pendingHero.Text = pending.ToString();
            pendingValue.Text = pending.ToString();
            activeValue.Text = activeLoans.ToString();
            finesValue.Text = fineAmount.ToString("C2");
            donut.Percentage = attentionPercentage;

            var maximum = Math.Max(1, Math.Max(pending, Math.Max(activeLoans, overdueLoans)));
            pendingBar.Maximum = maximum;
            activeBar.Maximum = maximum;
            overdueBar.Maximum = maximum;
            pendingBar.Value = pending;
            activeBar.Value = activeLoans;
            overdueBar.Value = overdueLoans;
            pendingCount.Text = pending.ToString();
            activeCount.Text = activeLoans.ToString();
            overdueCount.Text = overdueLoans.ToString();
            distributionTotal.Text = $"{pending + activeLoans + overdueLoans} movimientos";
            RenderizarActividad(activityList, requests);
            _status.Text = $"Inicio actualizado · {pending} solicitudes pendientes · {activeLoans} préstamos activos";
        });
        return page;
    }

    private Control CrearHeroInicio(Label pendingHero)
    {
        var hero = new WebGradientPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(30, 18, 24, 18),
            Margin = new Padding(0, 0, 0, 8)
        };
        hero.Resize += (_, _) => DesktopTheme.SetRoundedRegion(hero, 18);
        var layout = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        var copy = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        copy.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        copy.Controls.Add(CrearEtiquetaDashboard(
            "TU BIBLIOTECA, BAJO CONTROL",
            8,
            FontStyle.Bold,
            Color.FromArgb(190, 240, 255)), 0, 0);
        copy.Controls.Add(CrearEtiquetaDashboard(
            $"Hola, {_session.Usuario.Nombre}.",
            27,
            FontStyle.Bold,
            Color.White), 0, 1);
        copy.Controls.Add(CrearEtiquetaDashboard(
            "Supervisa solicitudes, préstamos e inventario desde un solo lugar.",
            10,
            FontStyle.Regular,
            Color.FromArgb(225, 246, 255)), 0, 2);
        layout.Controls.Add(copy, 0, 0);
        var count = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(12, 5, 0, 5)
        };
        count.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
        count.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        pendingHero.TextAlign = ContentAlignment.BottomCenter;
        count.Controls.Add(pendingHero, 0, 0);
        var caption = CrearEtiquetaDashboard(
            "solicitudes por atender",
            8.5f,
            FontStyle.Regular,
            Color.FromArgb(210, 245, 255));
        caption.TextAlign = ContentAlignment.TopCenter;
        count.Controls.Add(caption, 0, 1);
        layout.Controls.Add(count, 1, 0);
        hero.Controls.Add(layout);
        return hero;
    }

    private static Control CrearTarjetaMetrica(
        string title,
        string description,
        Label value,
        Color accent)
    {
        var card = new SurfaceCard
        {
            Dock = DockStyle.Fill,
            BackColor = DesktopTheme.Surface,
            Padding = new Padding(18, 12, 16, 10),
            Margin = new Padding(0, 0, 10, 0)
        };
        card.Paint += (_, eventArgs) =>
        {
            using var accentPen = new Pen(accent, 3);
            eventArgs.Graphics.DrawLine(accentPen, 16, 1, Math.Max(16, card.Width - 16), 1);
        };
        var layout = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(CrearEtiquetaDashboard(title, 8, FontStyle.Bold, DesktopTheme.Muted), 0, 0);
        layout.Controls.Add(value, 0, 1);
        layout.Controls.Add(CrearEtiquetaDashboard(description, 8.5f, FontStyle.Regular, DesktopTheme.Muted), 0, 2);
        card.Controls.Add(layout);
        return card;
    }

    private static Control CrearTarjetaAtencion(DashboardDonut donut)
    {
        var card = new WebGradientPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 20, 18),
            Margin = new Padding(0, 0, 8, 0)
        };
        card.Resize += (_, _) => DesktopTheme.SetRoundedRegion(card, 16);
        var layout = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        var copy = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        copy.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        copy.Controls.Add(CrearEtiquetaDashboard("RENDIMIENTO", 8, FontStyle.Bold, Color.FromArgb(190, 240, 255)), 0, 0);
        copy.Controls.Add(CrearEtiquetaDashboard("Estado de atención", 16, FontStyle.Bold, Color.White), 0, 1);
        copy.Controls.Add(CrearEtiquetaDashboard(
            "Visualiza rápidamente cuánto trabajo de solicitudes ya fue atendido.",
            9,
            FontStyle.Regular,
            Color.FromArgb(225, 246, 255)), 0, 2);
        layout.Controls.Add(copy, 0, 0);
        layout.Controls.Add(donut, 1, 0);
        card.Controls.Add(layout);
        return card;
    }

    private static Control CrearTarjetaDistribucion(
        Label total,
        DashboardProgressBar pendingBar,
        Label pendingCount,
        DashboardProgressBar activeBar,
        Label activeCount,
        DashboardProgressBar overdueBar,
        Label overdueCount)
    {
        var card = new SurfaceCard
        {
            Dock = DockStyle.Fill,
            BackColor = DesktopTheme.Surface,
            Padding = new Padding(20, 16, 20, 14),
            Margin = new Padding(8, 0, 0, 0)
        };
        var root = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var heading = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        heading.Controls.Add(CrearEtiquetaDashboard("ACTIVIDAD", 8, FontStyle.Bold, DesktopTheme.Primary), 0, 0);
        heading.Controls.Add(CrearEtiquetaDashboard("Distribución", 15, FontStyle.Bold, DesktopTheme.Navy), 0, 1);
        total.BackColor = DesktopTheme.PrimarySoft;
        total.TextAlign = ContentAlignment.MiddleCenter;
        total.Margin = new Padding(5, 4, 0, 10);
        total.Resize += (_, _) => DesktopTheme.SetRoundedRegion(total, 10);
        heading.Controls.Add(total, 1, 0);
        heading.SetRowSpan(total, 2);
        root.Controls.Add(heading, 0, 0);

        var rows = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 35));
        for (var index = 0; index < 3; index++)
            rows.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333f));
        AgregarFilaDistribucion(rows, 0, "Pendientes", pendingBar, pendingCount);
        AgregarFilaDistribucion(rows, 1, "Activos", activeBar, activeCount);
        AgregarFilaDistribucion(rows, 2, "Vencidos", overdueBar, overdueCount);
        root.Controls.Add(rows, 0, 1);
        card.Controls.Add(root);
        return card;
    }

    private static void AgregarFilaDistribucion(
        TableLayoutPanel rows,
        int row,
        string label,
        DashboardProgressBar bar,
        Label count)
    {
        rows.Controls.Add(CrearEtiquetaDashboard(label, 9, FontStyle.Regular, DesktopTheme.Text), 0, row);
        bar.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        bar.Margin = new Padding(6, 0, 10, 0);
        rows.Controls.Add(bar, 1, row);
        count.TextAlign = ContentAlignment.MiddleRight;
        rows.Controls.Add(count, 2, row);
    }

    private static Label CrearEtiquetaDashboard(
        string text,
        float size,
        FontStyle style,
        Color color) =>
        new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            BackColor = Color.Transparent,
            ForeColor = color,
            Font = DesktopTheme.Font(size, style),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        };

    private static void RenderizarActividad(
        FlowLayoutPanel container,
        IReadOnlyCollection<JsonElement> requests)
    {
        container.SuspendLayout();
        container.Controls.Clear();
        var recent = requests
            .OrderByDescending(item => LeerFecha(item, "fechaSolicitud"))
            .Take(3)
            .ToArray();
        if (recent.Length == 0)
        {
            var empty = new SurfaceCard
            {
                Height = 76,
                Width = Math.Max(100, container.ClientSize.Width - 2),
                BackColor = DesktopTheme.Surface,
                Padding = new Padding(18),
                Margin = Padding.Empty
            };
            empty.Controls.Add(CrearEtiquetaDashboard(
                "Todavía no hay solicitudes recientes para mostrar.",
                9.5f,
                FontStyle.Regular,
                DesktopTheme.Muted));
            container.Controls.Add(empty);
        }
        foreach (var request in recent)
        {
            var state = LeerTexto(request, "estado");
            var row = new SurfaceCard
            {
                Height = 58,
                Width = Math.Max(100, container.ClientSize.Width - 2),
                BackColor = DesktopTheme.Surface,
                Padding = new Padding(16, 7, 14, 7),
                Margin = new Padding(0, 0, 0, 6)
            };
            var layout = new BufferedTableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            layout.Controls.Add(CrearEtiquetaDashboard(
                LeerTexto(request, "libroTitulo", "Libro solicitado"),
                9.5f,
                FontStyle.Bold,
                DesktopTheme.Navy), 0, 0);
            layout.Controls.Add(CrearEtiquetaDashboard(
                $"Solicitud #{LeerTexto(request, "id", "—")} · {LeerFecha(request, "fechaSolicitud"):dd/MM/yyyy HH:mm}",
                8.5f,
                FontStyle.Regular,
                DesktopTheme.Muted), 0, 1);
            layout.Controls.Add(CrearEtiquetaDashboard(
                LeerTexto(request, "usuarioNombre", "Usuario"),
                9,
                FontStyle.Regular,
                DesktopTheme.Text), 1, 0);
            var userCaption = CrearEtiquetaDashboard(
                "Usuario solicitante",
                8,
                FontStyle.Regular,
                DesktopTheme.Muted);
            layout.Controls.Add(userCaption, 1, 1);
            var stateLabel = CrearEtiquetaDashboard(
                state,
                8.5f,
                FontStyle.Bold,
                state switch
                {
                    "Pendiente" => Color.FromArgb(146, 91, 0),
                    "Aprobada" => DesktopTheme.Success,
                    "Rechazada" or "Cancelada" => DesktopTheme.Danger,
                    _ => DesktopTheme.Text
                });
            stateLabel.TextAlign = ContentAlignment.MiddleCenter;
            stateLabel.BackColor = state switch
            {
                "Pendiente" => Color.FromArgb(255, 247, 220),
                "Aprobada" => Color.FromArgb(226, 247, 235),
                "Rechazada" or "Cancelada" => Color.FromArgb(254, 226, 226),
                _ => DesktopTheme.PrimarySoft
            };
            stateLabel.Margin = new Padding(8, 5, 0, 5);
            stateLabel.Resize += (_, _) => DesktopTheme.SetRoundedRegion(stateLabel, 10);
            layout.Controls.Add(stateLabel, 2, 0);
            layout.SetRowSpan(stateLabel, 2);
            row.Controls.Add(layout);
            container.Controls.Add(row);
        }
        container.ResumeLayout();
    }

    private static IReadOnlyList<JsonElement> LeerObjetos(
        JsonElement json,
        string? propertyName = null)
    {
        if (!string.IsNullOrWhiteSpace(propertyName) &&
            json.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in json.EnumerateObject())
            {
                if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind == JsonValueKind.Array)
                    return property.Value.EnumerateArray()
                        .Where(item => item.ValueKind == JsonValueKind.Object)
                        .ToArray();
            }
        }
        if (json.ValueKind == JsonValueKind.Array)
            return json.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.Object)
                .ToArray();
        if (json.ValueKind == JsonValueKind.Object)
        {
            var values = json.EnumerateObject().FirstOrDefault(property =>
                property.Name.Equals("value", StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.Array);
            if (values.Value.ValueKind == JsonValueKind.Array)
                return values.Value.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.Object)
                    .ToArray();
        }
        return [];
    }

    private static int LeerEntero(JsonElement item, string propertyName) =>
        int.TryParse(LeerTexto(item, propertyName, "0"), out var value)
            ? value
            : 0;

    private static string LeerTexto(
        JsonElement item,
        string propertyName,
        string fallback = "")
    {
        if (item.ValueKind != JsonValueKind.Object)
            return fallback;
        foreach (var property in item.EnumerateObject())
        {
            if (!property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                continue;
            return property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString() ?? fallback
                : property.Value.ToString();
        }
        return fallback;
    }

    private static decimal LeerDecimal(JsonElement item, string propertyName) =>
        decimal.TryParse(
            LeerTexto(item, propertyName, "0"),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var value)
            ? value
            : 0;

    private static DateTimeOffset LeerFecha(JsonElement item, string propertyName) =>
        DateTimeOffset.TryParse(LeerTexto(item, propertyName), out var value)
            ? value.ToLocalTime()
            : DateTimeOffset.MinValue;

    private static JsonElement CrearJsonVacio() =>
        JsonDocument.Parse("[]").RootElement.Clone();

    private static void IrAModulo(Control source, string module)
    {
        Control? current = source;
        while (current is not null && current is not TabPage)
            current = current.Parent;
        if (current is not TabPage page)
            return;
        if (page.Parent is not TabControl tabs)
            return;
        var target = tabs.TabPages.Cast<TabPage>()
            .FirstOrDefault(item => item.Text.Equals(module, StringComparison.OrdinalIgnoreCase));
        if (target is not null)
            tabs.SelectedTab = target;
    }

    private TabPage CrearSolicitudes()
    {
        var (page, grid, toolbar) = CrearPagina("Solicitudes");
        const int tamanoPagina = 10;
        var paginaActual = 1;
        string? estadoActual = "Pendiente";
        var navegacion = new BufferedFlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(0, 6, 0, 0)
        };
        var indicadorPagina = new Label
        {
            AutoSize = true,
            Text = "Página 1",
            ForeColor = DesktopTheme.Muted,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            Margin = new Padding(10, 10, 4, 0)
        };
        var paginador = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = DesktopTheme.Surface,
            Padding = new Padding(0, 0, 0, 4)
        };
        var paginadorLayout = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        paginadorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        paginadorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        paginadorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        paginadorLayout.Controls.Add(navegacion, 1, 0);
        paginador.Controls.Add(paginadorLayout);
        if (grid.Parent is Control tarjetaTabla)
            tarjetaTabla.Controls.Add(paginador);

        async Task CargarPaginaAsync(int pagina)
        {
            var paginaAnterior = paginaActual;
            paginaActual = Math.Max(1, pagina);
            var endpoint = estadoActual is null
                ? $"api/SolicitudesPrestamo?pagina={paginaActual}&tamanoPagina={tamanoPagina}"
                : $"api/SolicitudesPrestamo/estado/{estadoActual}?pagina={paginaActual}&tamanoPagina={tamanoPagina}";
            await CargarAsync(
                grid,
                endpoint);

            // Si no existen registros en la página solicitada, se conserva la última válida.
            if (grid.Rows.Count == 0 && paginaActual > 1)
            {
                paginaActual = paginaAnterior;
                endpoint = estadoActual is null
                    ? $"api/SolicitudesPrestamo?pagina={paginaActual}&tamanoPagina={tamanoPagina}"
                    : $"api/SolicitudesPrestamo/estado/{estadoActual}?pagina={paginaActual}&tamanoPagina={tamanoPagina}";
                await CargarAsync(
                    grid,
                    endpoint);
            }

            indicadorPagina.Text = $"Página {paginaActual} · {tamanoPagina} por página";
        }

        AgregarBoton(toolbar, "Pendientes", () =>
        {
            estadoActual = "Pendiente";
            return CargarPaginaAsync(1);
        });
        AgregarBoton(toolbar, "Ver todas", () =>
        {
            estadoActual = null;
            return CargarPaginaAsync(1);
        });
        AgregarBoton(navegacion, "Anterior", () => CargarPaginaAsync(paginaActual - 1));
        AgregarBoton(navegacion, "Siguiente", () => CargarPaginaAsync(paginaActual + 1));
        navegacion.Controls.Add(indicadorPagina);
        AgregarBoton(toolbar, "Aprobar", async () =>
        {
            if (!ValidarFilaPendiente(grid, "aprobar"))
                return;
            var solicitudId = ObtenerIdSeleccionado(grid, "Solicitud");
            if (solicitudId is null) return;
            var values = Pedir("Aprobar solicitud",
                Fecha("fechaPrestamo", "Fecha de préstamo"));
            if (values is null) return;
            values["solicitudPrestamoId"] = solicitudId.Value;
            values["empleadoPrestamoId"] = _session.Usuario.Id;
            await EjecutarAsync(() => _api.PostAsync("api/Prestamos", values), "Préstamo aprobado");
            estadoActual = "Pendiente";
            await CargarPaginaAsync(1);
        });
        AgregarBoton(toolbar, "Rechazar", async () =>
        {
            if (!ValidarFilaPendiente(grid, "rechazar"))
                return;
            var solicitudId = ObtenerIdSeleccionado(grid, "Solicitud");
            if (solicitudId is null) return;
            var values = Pedir("Rechazar solicitud",
                Texto("motivo", "Motivo"));
            if (values is null) return;
            values["solicitudPrestamoId"] = solicitudId.Value;
            values["empleadoResponsableId"] = _session.Usuario.Id;
            await EjecutarAsync(() => _api.PostAsync("api/Prestamos/solicitudes/rechazar", values), "Solicitud rechazada");
            estadoActual = "Pendiente";
            await CargarPaginaAsync(1);
        });
        page.Tag = new Func<Task>(() => CargarPaginaAsync(1));
        return page;
    }

    private TabPage CrearPrestamos()
    {
        var (page, grid, toolbar) = CrearPagina("Préstamos");
        AgregarBoton(toolbar, "Activos", () => CargarAsync(grid, "api/Prestamos/activos"));
        AgregarBoton(toolbar, "Vencidos", () => CargarAsync(grid, "api/Prestamos/vencidos"));
        AgregarBoton(toolbar, "Ver todos", () => CargarAsync(grid, "api/Prestamos", true));
        AgregarBoton(toolbar, "Por usuario", () => CargarPorIdAsync(grid, "Usuario", "api/Prestamos/usuario/{0}"));
        AgregarBoton(toolbar, "Por libro", () => CargarPorIdAsync(grid, "Libro", "api/Prestamos/libro/{0}"));
        AgregarBoton(toolbar, "Por ejemplar", () => CargarPorIdAsync(grid, "Ejemplar", "api/Prestamos/ejemplar/{0}"));
        AgregarBoton(toolbar, "Cancelar", async () =>
        {
            var prestamoId = ObtenerIdSeleccionado(grid, "Préstamo");
            if (prestamoId is null) return;
            var values = Pedir("Cancelar préstamo",
                Texto("motivo", "Motivo", "Cancelación administrativa"));
            if (values is null) return;
            values["prestamoId"] = prestamoId.Value;
            values["empleadoResponsableId"] = _session.Usuario.Id;
            await EjecutarAsync(() => _api.PostAsync("api/Prestamos/cancelar", values), "Préstamo cancelado");
            await CargarAsync(grid, "api/Prestamos/activos");
        });
        AgregarBoton(toolbar, "Registrar pérdida", async () =>
        {
            var prestamoId = ObtenerIdSeleccionado(grid, "Préstamo");
            if (prestamoId is null) return;
            var values = Pedir("Registrar pérdida",
                Fecha("fechaReporte", "Fecha del reporte"),
                Texto("motivo", "Motivo"));
            if (values is null) return;
            values["prestamoId"] = prestamoId.Value;
            values["empleadoResponsableId"] = _session.Usuario.Id;
            await EjecutarAsync(() => _api.PostAsync("api/Prestamos/perdidas", values), "Pérdida registrada");
            await CargarAsync(grid, "api/Prestamos/activos");
        });
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Prestamos/activos"));
        return page;
    }

    private TabPage CrearDevoluciones()
    {
        var (page, grid, toolbar) = CrearPagina("Devoluciones");
        const string pendientesDevolucionEndpoint = "api/Prestamos/pendientes-devolucion";
        const int tamanoPagina = 10;
        var paginaActual = 1;
        var endpointActual = pendientesDevolucionEndpoint;
        var navegacion = CrearNavegacionPaginada(grid, out var indicadorPagina);

        async Task CargarPaginaAsync(int pagina)
        {
            var paginaAnterior = paginaActual;
            paginaActual = Math.Max(1, pagina);
            await CargarAsync(grid, $"{endpointActual}?pagina={paginaActual}&tamanoPagina={tamanoPagina}", true);
            if (grid.Rows.Count == 0 && paginaActual > 1)
            {
                paginaActual = paginaAnterior;
                await CargarAsync(grid, $"{endpointActual}?pagina={paginaActual}&tamanoPagina={tamanoPagina}", true);
            }
            indicadorPagina.Text = $"Página {paginaActual} · {tamanoPagina} por página";
        }

        AgregarBoton(toolbar, "Por devolver", () =>
        {
            endpointActual = pendientesDevolucionEndpoint;
            return CargarPaginaAsync(1);
        });
        AgregarBoton(toolbar, "Ver todos", () =>
        {
            endpointActual = "api/Prestamos";
            return CargarPaginaAsync(1);
        });
        AgregarBoton(navegacion, "Anterior", () => CargarPaginaAsync(paginaActual - 1));
        AgregarBoton(navegacion, "Siguiente", () => CargarPaginaAsync(paginaActual + 1));
        navegacion.Controls.Add(indicadorPagina);
        AgregarBoton(toolbar, "Historial por usuario", () => CargarPorIdAsync(grid, "Usuario", "api/Devoluciones/usuario/{0}"));
        AgregarBoton(toolbar, "Historial por libro", () => CargarPorIdAsync(grid, "Libro", "api/Devoluciones/libro/{0}"));
        AgregarBoton(toolbar, "Registrar devolución", async () =>
        {
            var prestamoId = ObtenerIdSeleccionado(grid, "Préstamo");
            if (prestamoId is null) return;
            var values = Pedir("Registrar devolución",
                Fecha("fechaRealDevolucion", "Fecha real"));
            if (values is null) return;
            values["prestamoId"] = prestamoId.Value;
            values["empleadoDevolucionId"] = _session.Usuario.Id;
            await EjecutarAsync(() => _api.PostAsync("api/Devoluciones", values), "Devolución registrada");
            endpointActual = pendientesDevolucionEndpoint;
            await CargarPaginaAsync(1);
        });
        AgregarBoton(toolbar, "Devolución con daño", async () =>
        {
            var prestamoId = ObtenerIdSeleccionado(grid, "Préstamo");
            if (prestamoId is null) return;
            var values = Pedir("Devolución con daño",
                Fecha("fechaDevolucion", "Fecha"),
                Texto("motivo", "Descripción del daño"));
            if (values is null) return;
            values["prestamoId"] = prestamoId.Value;
            values["empleadoResponsableId"] = _session.Usuario.Id;
            await EjecutarAsync(() => _api.PostAsync("api/Devoluciones/con-danio", values), "Daño y devolución registrados");
        });
        page.Tag = new Func<Task>(() => CargarPaginaAsync(1));
        return page;
    }

    private TabPage CrearCatalogo()
    {
        var (page, grid, toolbar) = CrearPagina("Catálogo");
        const int tamanoPagina = 10;
        var paginaActual = 1;
        var endpointActual = "api/Libros";
        var navegacion = CrearNavegacionPaginada(grid, out var indicadorPagina);

        async Task CargarPaginaAsync(int pagina)
        {
            var paginaAnterior = paginaActual;
            paginaActual = Math.Max(1, pagina);
            var separador = endpointActual.Contains('?') ? '&' : '?';
            await CargarAsync(grid, $"{endpointActual}{separador}pagina={paginaActual}&tamanoPagina={tamanoPagina}", true);
            if (grid.Rows.Count == 0 && paginaActual > 1)
            {
                paginaActual = paginaAnterior;
                await CargarAsync(grid, $"{endpointActual}{separador}pagina={paginaActual}&tamanoPagina={tamanoPagina}", true);
            }
            indicadorPagina.Text = $"Página {paginaActual} · {tamanoPagina} por página";
        }

        AgregarBoton(toolbar, "Actualizar", () =>
        {
            endpointActual = "api/Libros";
            return CargarPaginaAsync(1);
        });
        AgregarBoton(navegacion, "Anterior", () => CargarPaginaAsync(paginaActual - 1));
        AgregarBoton(navegacion, "Siguiente", () => CargarPaginaAsync(paginaActual + 1));
        navegacion.Controls.Add(indicadorPagina);
        AgregarBoton(toolbar, "Buscar", async () =>
        {
            var values = Pedir("Buscar libros", Texto("termino", "Título o autor"));
            if (values is null) return;
            endpointActual = $"api/Libros/buscar?termino={Uri.EscapeDataString(values["termino"]?.ToString() ?? "")}";
            await CargarPaginaAsync(1);
        });
        AgregarBoton(toolbar, "Nuevo libro", async () =>
        {
            var values = PedirLibro("Registrar libro", incluirDatosRegistro: true);
            if (values is null) return;
            await EjecutarAsync(() => _api.PostAsync("api/Libros", values), "Libro registrado");
            endpointActual = "api/Libros";
            await CargarPaginaAsync(1);
        });
        AgregarBoton(toolbar, "Editar libro", async () =>
        {
            var id = ObtenerIdSeleccionado(grid, "Libro");
            if (id is null) return;
            var values = PedirLibro("Actualizar libro", grid);
            if (values is null) return;
            await EjecutarAsync(() => _api.PutAsync($"api/Libros/{id}", values), "Libro actualizado");
            await CargarPaginaAsync(paginaActual);
        });
        AgregarBoton(toolbar, "Eliminar libro", async () =>
        {
            var id = ObtenerIdSeleccionado(grid, "Libro");
            if (id is null || !Confirmar("¿Deseas eliminar el libro seleccionado?"))
                return;
            await EjecutarAsync(
                () => _api.DeleteAsync($"api/Libros/{id}"),
                "Libro eliminado del catálogo");
            await CargarPaginaAsync(paginaActual);
        });
        page.Tag = new Func<Task>(() => CargarPaginaAsync(1));
        return page;
    }

    private TabPage CrearInventario()
    {
        var (page, grid, toolbar) = CrearPagina("Inventario");
        AgregarBoton(toolbar, "Actualizar", () => CargarAsync(grid, "api/Reportes/inventario", true));
        AgregarBoton(toolbar, "Ver ejemplares", () => CargarPorIdAsync(grid, "Libro", "api/Inventario/libro/{0}/ejemplares"));
        AgregarBoton(toolbar, "Crear inventario", async () =>
        {
            var values = Pedir("Crear inventario",
                Entero("libroId", "ID del libro"),
                Entero("cantidadTotal", "Cantidad total"),
                Texto("motivo", "Motivo", "Inventario inicial"));
            if (values is null) return;
            values["usuarioResponsableId"] = _session.Usuario.Id;
            await EjecutarAsync(() => _api.PostAsync("api/Inventario", values), "Inventario creado");
            await CargarAsync(grid, "api/Reportes/inventario");
        });
        AgregarBoton(toolbar, "Ajustar cantidad", async () =>
        {
            var values = Pedir("Ajustar inventario",
                Entero("inventarioId", "ID del inventario"),
                Entero("nuevaCantidadTotal", "Nueva cantidad total"),
                Texto("motivo", "Motivo"));
            if (values is null) return;
            values["usuarioResponsableId"] = _session.Usuario.Id;
            await EjecutarAsync(() => _api.PutAsync("api/Inventario/ajustar", values), "Inventario ajustado");
            await CargarAsync(grid, "api/Reportes/inventario");
        });
        AgregarBoton(toolbar, "Cambiar estado", async () =>
        {
            var values = Pedir("Cambiar estado de ejemplar",
                Entero("ejemplarId", "ID del ejemplar"),
                Seleccion(
                    "nuevoEstado",
                    "Nuevo estado",
                    "Disponible",
                    "Disponible",
                    "Prestado",
                    "Reservado",
                    "FueraDeServicio",
                    "Perdido",
                    "Danado"),
                Texto("motivo", "Motivo"));
            if (values is null) return;
            values["usuarioResponsableId"] = _session.Usuario.Id;
            await EjecutarAsync(() => _api.PutAsync("api/Inventario/ejemplares/estado", values), "Estado actualizado");
        });
        AgregarBoton(toolbar, "Historial de ejemplar", () => CargarPorIdAsync(grid, "Ejemplar", "api/Inventario/ejemplares/{0}/historial"));
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Reportes/inventario"));
        return page;
    }

    private TabPage CrearMultas()
    {
        var (page, grid, toolbar) = CrearPagina("Multas");
        AgregarBoton(toolbar, "Pendientes", () => CargarAsync(grid, "api/Multas/estado/Pendiente"));
        AgregarBoton(toolbar, "Pagadas", () => CargarAsync(grid, "api/Multas/estado/Pagada"));
        AgregarBoton(toolbar, "Resueltas", () => CargarAsync(grid, "api/Multas/estado/Resuelta"));
        AgregarBoton(toolbar, "Por usuario", () => CargarPorIdAsync(grid, "Usuario", "api/Multas/usuario/{0}"));
        AgregarBoton(toolbar, "Registrar pago", async () =>
        {
            var multaId = ObtenerIdSeleccionado(grid, "Multa");
            if (multaId is null) return;
            if (!Confirmar("¿Confirmas el pago de la multa seleccionada?"))
                return;
            var values = new Dictionary<string, object?>();
            values["multaId"] = multaId.Value;
            values["usuarioResponsableId"] = _session.Usuario.Id;
            await EjecutarAsync(() => _api.PutAsync("api/Multas/pagar", values), "Pago registrado");
            await CargarAsync(grid, "api/Multas/estado/Pendiente");
        });
        AgregarBoton(toolbar, "Resolver", async () =>
        {
            var multaId = ObtenerIdSeleccionado(grid, "Multa");
            if (multaId is null) return;
            var values = Pedir("Resolver multa",
                Fecha("fechaResolucion", "Fecha de resolución"),
                Texto("observacion", "Observación"));
            if (values is null) return;
            values["multaId"] = multaId.Value;
            values["empleadoResolucionId"] = _session.Usuario.Id;
            await EjecutarAsync(() => _api.PutAsync("api/Multas/resolver", values), "Multa resuelta");
            await CargarAsync(grid, "api/Multas/estado/Pendiente");
        });
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Multas/estado/Pendiente"));
        return page;
    }

    private TabPage CrearAuditoria()
    {
        var (page, grid, toolbar) = CrearPagina("Auditoría");
        AgregarBoton(toolbar, "Todo", () => CargarAsync(grid, "api/Auditoria"));
        AgregarBoton(toolbar, "Por usuario", () => CargarPorIdAsync(grid, "Usuario", "api/Auditoria/usuario/{0}"));
        AgregarBoton(toolbar, "Préstamos", () => CargarAsync(grid, "api/Auditoria/modulo/Prestamos"));
        AgregarBoton(toolbar, "Inventario", () => CargarAsync(grid, "api/Auditoria/modulo/Inventario"));
        AgregarBoton(toolbar, "Multas", () => CargarAsync(grid, "api/Auditoria/modulo/Multas"));
        AgregarBoton(toolbar, "Por período", async () =>
        {
            var values = Pedir(
                "Período de auditoría",
                Fecha("desde", "Desde"),
                Fecha("hasta", "Hasta"));
            if (values is null) return;
            var desde = Uri.EscapeDataString(((DateTime)values["desde"]!).ToUniversalTime().ToString("O"));
            var hasta = Uri.EscapeDataString(((DateTime)values["hasta"]!).ToUniversalTime().ToString("O"));
            await CargarAsync(
                grid,
                $"api/Auditoria/rango?fechaDesde={desde}&fechaHasta={hasta}");
        });
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Auditoria"));
        return page;
    }

    private TabPage CrearAdministracion()
    {
        var page = new TabPage("Administración")
        {
            BackColor = DesktopTheme.Background,
            Padding = Padding.Empty
        };
        var tabs = new BufferedTabControl
        {
            Dock = DockStyle.Fill,
            Appearance = TabAppearance.FlatButtons,
            SizeMode = TabSizeMode.Fixed,
            ItemSize = new Size(0, 1),
            Multiline = true,
            Margin = Padding.Empty,
            Padding = Point.Empty
        };
        tabs.TabPages.Add(CrearModuloDiferido("Usuarios", CrearAdministracionUsuarios));
        tabs.TabPages.Add(CrearModuloDiferido("Empleados", CrearAdministracionEmpleados));
        tabs.TabPages.Add(CrearModuloDiferido("Administradores", CrearAdministracionAdministradores));
        tabs.TabPages.Add(CrearModuloDiferido("Cargos", CrearAdministracionCargos));
        tabs.TabPages.Add(CrearModuloDiferido("Roles y permisos", CrearAdministracionRoles));

        var navigationCard = new SurfaceCard
        {
            Dock = DockStyle.Fill,
            BackColor = DesktopTheme.Surface,
            Padding = new Padding(18, 24, 18, 18),
            Margin = new Padding(24, 20, 12, 20)
        };
        var navigationLayout = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        navigationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        navigationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        navigationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        navigationLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        navigationLayout.Controls.Add(new Label
        {
            Text = "ADMINISTRACIÓN",
            Dock = DockStyle.Fill,
            ForeColor = DesktopTheme.Primary,
            Font = DesktopTheme.Font(8, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        navigationLayout.Controls.Add(new Label
        {
            Text = "Gestiona el equipo y los accesos",
            Dock = DockStyle.Fill,
            ForeColor = DesktopTheme.Navy,
            Font = DesktopTheme.TitleFont(14),
            TextAlign = ContentAlignment.TopLeft
        }, 0, 1);
        navigationLayout.Controls.Add(new Label
        {
            Text = "Selecciona una sección para consultar o realizar cambios.",
            Dock = DockStyle.Fill,
            ForeColor = DesktopTheme.Muted,
            Font = DesktopTheme.Font(9),
            TextAlign = ContentAlignment.TopLeft
        }, 0, 2);
        var navigation = new BufferedFlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0),
            Margin = Padding.Empty
        };
        var buttons = new List<Button>();
        for (var index = 0; index < tabs.TabPages.Count; index++)
        {
            var pageIndex = index;
            var button = new AnimatedButton
            {
                Text = ObtenerIconoAdministracion(tabs.TabPages[index].Text) +
                    "  " + tabs.TabPages[index].Text,
                AutoSize = false,
                Width = 228,
                Height = 48,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 8, 0),
                Margin = new Padding(0, 0, 0, 8),
                Tag = pageIndex
            };
            if (index == 0)
                DesktopTheme.StylePrimaryButton(button);
            else
                DesktopTheme.StyleSecondaryButton(button);
            button.Click += (_, _) =>
            {
                tabs.SelectedIndex = pageIndex;
                foreach (var item in buttons)
                {
                    if (Convert.ToInt32(item.Tag) == pageIndex)
                        DesktopTheme.StylePrimaryButton(item);
                    else
                        DesktopTheme.StyleSecondaryButton(item);
                }
            };
            buttons.Add(button);
            navigation.Controls.Add(button);
        }
        navigationLayout.Controls.Add(navigation, 0, 3);
        navigationCard.Controls.Add(navigationLayout);

        var root = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = DesktopTheme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 302));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(navigationCard, 0, 0);
        root.Controls.Add(tabs, 1, 0);

        tabs.SelectedIndexChanged += async (_, _) =>
            await CargarPestanaAsync(tabs.SelectedTab);
        page.Controls.Add(root);
        page.Tag = new Func<Task>(() => CargarPestanaAsync(tabs.SelectedTab));
        return page;
    }

    private static string ObtenerIconoAdministracion(string section) =>
        section switch
        {
            "Usuarios" => "◉",
            "Empleados" => "◆",
            "Administradores" => "★",
            "Cargos" => "▣",
            "Roles y permisos" => "⚿",
            _ => "•"
        };

    private static void ConfigurarSeleccionAdministracion(DataGridView grid)
    {
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 236, 253);
        grid.DefaultCellStyle.SelectionForeColor = DesktopTheme.Text;
        grid.CellPainting += (_, eventArgs) =>
        {
            if (eventArgs.RowIndex < 0 ||
                !eventArgs.State.HasFlag(DataGridViewElementStates.Selected))
                return;

            eventArgs.Paint(
                eventArgs.CellBounds,
                eventArgs.PaintParts & ~DataGridViewPaintParts.Focus);
            eventArgs.Handled = true;
        };
    }

    private TabPage CrearAdministracionUsuarios()
    {
        var (page, grid, toolbar) = CrearPagina("Usuarios");
        ConfigurarSeleccionAdministracion(grid);
        AgregarBoton(toolbar, "Actualizar", () => CargarAsync(grid, "api/Usuarios", true));
        AgregarBoton(toolbar, "Crear", async () =>
        {
            var values = Pedir("Crear usuario",
                Texto("nombre", "Nombre"),
                Texto("apellido", "Apellido"),
                Texto("cedula", "Cédula"),
                Texto("telefono", "Teléfono"),
                Texto("email", "Correo"),
                Password("password", "Contraseña"),
                Seleccion(
                    "tipoUsuario",
                    "Tipo de usuario",
                    "Estudiante",
                    "Estudiante",
                    "Docente",
                    "Administrativo",
                    "Externo"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PostAsync("api/Usuarios", values), "Usuario creado");
            await CargarAsync(grid, "api/Usuarios");
        });
        AgregarBoton(toolbar, "Editar seleccionado", async () =>
        {
            var id = ObtenerIdSeleccionado(grid, "Usuario");
            if (id is null) return;
            var values = Pedir("Actualizar usuario",
                Texto("nombre", "Nombre", ObtenerCelda(grid, "nombre")),
                Texto("apellido", "Apellido", ObtenerCelda(grid, "apellido")),
                Texto("cedula", "Cédula", ObtenerCelda(grid, "cedula")),
                Texto("telefono", "Teléfono", ObtenerCelda(grid, "telefono")),
                Texto("email", "Correo", ObtenerCelda(grid, "email")),
                Seleccion(
                    "tipoUsuario",
                    "Tipo de usuario",
                    ObtenerCelda(grid, "tipoUsuario"),
                    "Estudiante",
                    "Docente",
                    "Administrativo",
                    "Externo"),
                Seleccion(
                    "estado",
                    "Estado de la cuenta",
                    ObtenerCelda(grid, "estado"),
                    "Activo",
                    "Inactivo",
                    "Suspendido"));
            if (values is null) return;
            await EjecutarAsync(
                () => _api.PutAsync($"api/Usuarios/{id}", values),
                "Usuario actualizado");
            await CargarAsync(grid, "api/Usuarios");
        });
        AgregarBoton(toolbar, "Desactivar seleccionado", async () =>
        {
            var id = ObtenerIdSeleccionado(grid, "Usuario");
            if (id is null || !Confirmar("¿Deseas desactivar el usuario seleccionado?"))
                return;
            await EjecutarAsync(
                () => _api.DeleteAsync($"api/Usuarios/{id}"),
                "Usuario desactivado");
            await CargarAsync(grid, "api/Usuarios");
        });
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Usuarios"));
        return page;
    }

    private TabPage CrearAdministracionEmpleados()
    {
        var (page, grid, toolbar) = CrearPagina("Empleados");
        ConfigurarSeleccionAdministracion(grid);
        AgregarBoton(toolbar, "Actualizar", () => CargarAsync(grid, "api/Empleados", true));
        AgregarBoton(toolbar, "Crear", async () =>
        {
            var values = Pedir("Registrar empleado",
                Entero("usuarioId", "ID de usuario"),
                Entero("cargoId", "ID de cargo"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PostAsync("api/Empleados", values), "Empleado registrado");
            await CargarAsync(grid, "api/Empleados");
        });
        AgregarBoton(toolbar, "Editar seleccionado", async () =>
        {
            var id = ObtenerIdSeleccionado(grid, "Empleado");
            if (id is null) return;
            var values = Pedir("Actualizar empleado", Entero("cargoId", "ID de cargo"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PutAsync($"api/Empleados/{id}", values), "Empleado actualizado");
            await CargarAsync(grid, "api/Empleados");
        });
        AgregarBoton(toolbar, "Eliminar seleccionado", async () =>
        {
            var id = ObtenerIdSeleccionado(grid, "Empleado");
            if (id is null ||
                !Confirmar("¿Deseas eliminar el perfil de empleado seleccionado?"))
                return;
            await EjecutarAsync(() => _api.DeleteAsync($"api/Empleados/{id}"), "Empleado eliminado");
            await CargarAsync(grid, "api/Empleados");
        });
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Empleados"));
        return page;
    }

    private TabPage CrearAdministracionAdministradores()
    {
        var (page, grid, toolbar) = CrearPagina("Administradores");
        ConfigurarSeleccionAdministracion(grid);
        AgregarBoton(toolbar, "Actualizar", () => CargarAsync(grid, "api/Administradores", true));
        AgregarBoton(toolbar, "Crear", async () =>
        {
            var values = Pedir("Registrar administrador",
                Entero("usuarioId", "ID de usuario"),
                Entero("cargoId", "ID de cargo"),
                Entero("usuarioResponsableId", "ID responsable", _session.Usuario.Id.ToString()));
            if (values is null) return;
            await EjecutarAsync(
                () => _api.PostAsync("api/Administradores", values),
                "Administrador registrado");
            await CargarAsync(grid, "api/Administradores");
        });
        AgregarBoton(toolbar, "Editar seleccionado", async () =>
        {
            var id = ObtenerIdSeleccionado(grid, "Administrador");
            if (id is null) return;
            var values = Pedir("Actualizar administrador",
                Entero("cargoId", "ID de cargo"),
                Entero("usuarioResponsableId", "ID responsable", _session.Usuario.Id.ToString()));
            if (values is null) return;
            await EjecutarAsync(
                () => _api.PutAsync($"api/Administradores/{id}", values),
                "Administrador actualizado");
            await CargarAsync(grid, "api/Administradores");
        });
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Administradores"));
        return page;
    }

    private TabPage CrearAdministracionCargos()
    {
        var (page, grid, toolbar) = CrearPagina("Cargos");
        ConfigurarSeleccionAdministracion(grid);
        AgregarBoton(toolbar, "Actualizar", () => CargarAsync(grid, "api/Cargos", true));
        AgregarBoton(toolbar, "Crear", async () =>
        {
            var values = Pedir("Crear cargo", Texto("nombre", "Nombre"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PostAsync("api/Cargos", values), "Cargo creado");
            await CargarAsync(grid, "api/Cargos");
        });
        AgregarBoton(toolbar, "Editar seleccionado", async () =>
        {
            var id = ObtenerIdSeleccionado(grid, "Cargo");
            if (id is null) return;
            var values = Pedir("Actualizar cargo",
                Texto("nombre", "Nombre", ObtenerCelda(grid, "nombre")));
            if (values is null) return;
            await EjecutarAsync(() => _api.PutAsync($"api/Cargos/{id}", values), "Cargo actualizado");
            await CargarAsync(grid, "api/Cargos");
        });
        AgregarBoton(toolbar, "Eliminar seleccionado", async () =>
        {
            var id = ObtenerIdSeleccionado(grid, "Cargo");
            if (id is null || !Confirmar("¿Deseas eliminar el cargo seleccionado?"))
                return;
            await EjecutarAsync(() => _api.DeleteAsync($"api/Cargos/{id}"), "Cargo eliminado");
            await CargarAsync(grid, "api/Cargos");
        });
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Cargos"));
        return page;
    }

    private TabPage CrearAdministracionRoles()
    {
        var (page, grid, toolbar) = CrearPagina("Roles y permisos");
        ConfigurarSeleccionAdministracion(grid);
        AgregarBoton(toolbar, "Actualizar", () => CargarAsync(grid, "api/Roles", true));
        AgregarBoton(toolbar, "Crear rol", async () =>
        {
            var values = Pedir("Crear rol",
                Texto("nombre", "Nombre"),
                Texto("descripcion", "Descripción"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PostAsync("api/Roles", values), "Rol creado");
            await CargarAsync(grid, "api/Roles");
        });
        AgregarBoton(toolbar, "Editar rol", async () =>
        {
            var id = ObtenerIdSeleccionado(grid, "Rol");
            if (id is null) return;
            var values = Pedir("Actualizar rol",
                Texto("nombre", "Nombre", ObtenerCelda(grid, "nombre")),
                Texto("descripcion", "Descripción", ObtenerCelda(grid, "descripcion")));
            if (values is null) return;
            await EjecutarAsync(() => _api.PutAsync($"api/Roles/{id}", values), "Rol actualizado");
            await CargarAsync(grid, "api/Roles");
        });
        AgregarBoton(toolbar, "Eliminar rol", async () =>
        {
            var id = ObtenerIdSeleccionado(grid, "Rol");
            if (id is null || !Confirmar("¿Deseas eliminar el rol seleccionado?"))
                return;
            await EjecutarAsync(() => _api.DeleteAsync($"api/Roles/{id}"), "Rol eliminado");
            await CargarAsync(grid, "api/Roles");
        });
        AgregarBoton(toolbar, "Asignar rol", () => GestionarRelacionAsync(
            "Asignar rol",
            "api/Roles/asignar",
            HttpMethod.Post,
            Entero("usuarioId", "ID de usuario"),
            Entero("rolId", "ID de rol")));
        AgregarBoton(toolbar, "Remover rol", () => GestionarRelacionAsync(
            "Remover rol",
            "api/Roles/asignar",
            HttpMethod.Delete,
            Entero("usuarioId", "ID de usuario"),
            Entero("rolId", "ID de rol")));
        AgregarBoton(toolbar, "Crear permiso", async () =>
        {
            var values = Pedir("Crear permiso",
                Texto("nombre", "Nombre"),
                Texto("codigo", "Código"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PostAsync("api/Roles/permisos", values), "Permiso creado");
        });
        AgregarBoton(toolbar, "Asignar permiso", () => GestionarRelacionAsync(
            "Asignar permiso",
            "api/Roles/permisos/asignar",
            HttpMethod.Post,
            Entero("rolId", "ID de rol"),
            Entero("permisoId", "ID de permiso")));
        AgregarBoton(toolbar, "Remover permiso", () => GestionarRelacionAsync(
            "Remover permiso",
            "api/Roles/permisos/asignar",
            HttpMethod.Delete,
            Entero("rolId", "ID de rol"),
            Entero("permisoId", "ID de permiso")));
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Roles"));
        return page;
    }

    private async Task GestionarRelacionAsync(
        string title,
        string endpoint,
        HttpMethod method,
        params InputField[] fields)
    {
        var values = Pedir(title, fields);
        if (values is null) return;
        await EjecutarAsync(
            () => method == HttpMethod.Post
                ? _api.PostAsync(endpoint, values)
                : _api.DeleteAsync(endpoint, values),
            $"{title} completado");
    }

    private TabPage CrearReportes()
    {
        var (page, grid, toolbar) = CrearPagina("Reportes");
        AgregarBoton(toolbar, "Inventario", () => CargarAsync(grid, "api/Reportes/inventario"));
        AgregarBoton(toolbar, "Préstamos por período", async () =>
        {
            var values = Pedir("Período del reporte", Fecha("desde", "Desde"), Fecha("hasta", "Hasta"));
            if (values is null) return;
            var desde = Uri.EscapeDataString(((DateTime)values["desde"]!).ToUniversalTime().ToString("O"));
            var hasta = Uri.EscapeDataString(((DateTime)values["hasta"]!).ToUniversalTime().ToString("O"));
            await CargarAsync(grid, $"api/Reportes/prestamos-fecha?desde={desde}&hasta={hasta}");
        });
        AgregarBoton(toolbar, "Multas por período", async () =>
        {
            var values = Pedir(
                "Período del reporte",
                Fecha("desde", "Desde"),
                Fecha("hasta", "Hasta"));
            if (values is null) return;
            var desde = Uri.EscapeDataString(((DateTime)values["desde"]!).ToUniversalTime().ToString("O"));
            var hasta = Uri.EscapeDataString(((DateTime)values["hasta"]!).ToUniversalTime().ToString("O"));
            await CargarAsync(grid, $"api/Reportes/multas?desde={desde}&hasta={hasta}");
        });
        AgregarBoton(toolbar, "Catálogo y demanda", async () =>
        {
            var values = Pedir(
                "Período del reporte",
                Fecha("desde", "Desde"),
                Fecha("hasta", "Hasta"));
            if (values is null) return;
            var desde = Uri.EscapeDataString(((DateTime)values["desde"]!).ToUniversalTime().ToString("O"));
            var hasta = Uri.EscapeDataString(((DateTime)values["hasta"]!).ToUniversalTime().ToString("O"));
            await CargarAsync(grid, $"api/Reportes/catalogo?desde={desde}&hasta={hasta}");
        });
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Reportes/inventario"));
        return page;
    }

    private async Task CargarPrimeraPestanaAsync(TabControl tabs)
    {
        tabs.SelectedIndexChanged += async (_, _) =>
            await CargarPestanaAsync(tabs.SelectedTab);
        await CargarPestanaAsync(tabs.SelectedTab);
    }

    private async Task PrepararModulosEnSegundoPlanoAsync(TabControl tabs)
    {
        // Entrega primero la pantalla de Inicio y materializa las demás vistas
        // durante pausas breves. Así el primer cambio de módulo no construye toda
        // su jerarquía de controles en el mismo instante del clic.
        await Task.Delay(400);
        foreach (var page in tabs.TabPages.Cast<TabPage>())
        {
            if (IsDisposed || Disposing)
                return;
            if (page.Tag is DeferredModule)
            {
                // Deja intervalos suficientes para que clics, repintados y entrada
                // del usuario siempre tengan prioridad sobre esta preparación.
                await Task.Delay(120);
                if (page.Tag is DeferredModule)
                    MaterializarModulo(page);
            }
        }

        await _inicioWarmupTask;

        var endpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (_session.TieneRol("Administrador") || _session.TieneRol("Bibliotecario"))
        {
            endpoints.Add("api/SolicitudesPrestamo/estado/Pendiente");
            endpoints.Add("api/Libros");
            endpoints.Add("api/Reportes/inventario");
        }
        if (_session.TieneRol("Administrador") || _session.TieneRol("Auditor"))
        {
            endpoints.Add("api/Auditoria");
            endpoints.Add("api/Reportes/inventario");
        }
        if (_session.TieneRol("Administrador"))
            endpoints.Add("api/Usuarios");

        // Las consultas se realizan de forma gradual para no competir con la
        // navegación ni saturar el pool de conexiones inmediatamente tras login.
        foreach (var endpoint in endpoints)
        {
            try
            {
                await _api.GetAsync(endpoint);
            }
            catch
            {
                // La precarga es opcional. El modulo mostrara cualquier error
                // cuando el usuario lo abra y solicite la informacion.
            }
            await Task.Delay(35);
        }

        // A partir de aquí, vistas y datos frecuentes quedan preparados; las
        // operaciones explícitas del usuario siguen solicitando información fresca.
    }

    private async Task CargarPestanaAsync(TabPage? page)
    {
        if (page?.Tag is DeferredModule)
        {
            _status.Text = $"Preparando {page.Text}\u2026";
            await Task.Yield();
            page = MaterializarModulo(page);
            await Task.Yield();
        }
        if (page?.Tag is not Func<Task> load)
            return;
        if (_moduleLoadedAt.TryGetValue(page, out var loadedAt) &&
            DateTime.UtcNow - loadedAt < ModuleRefreshInterval)
        {
            _status.Text = $"{page.Text} listo · mostrando información reciente";
            return;
        }
        if (!_modulesLoading.Add(page))
            return;

        try
        {
            await ProtegerAsync(load);
            _moduleLoadedAt[page] = DateTime.UtcNow;
        }
        finally
        {
            _modulesLoading.Remove(page);
        }
    }

    private async Task CalentarInicioAsync()
    {
        if (!_session.TieneRol("Administrador") && !_session.TieneRol("Bibliotecario"))
            return;

        try
        {
            await _api.GetAsync("api/ResumenInicio");
        }
        catch
        {
            // La precarga no debe impedir la carga normal ni mostrar errores.
        }
    }

    private static TabPage CrearModuloDiferido(string name, Func<TabPage> factory)
    {
        var page = new TabPage(name)
        {
            BackColor = DesktopTheme.Background,
            Padding = Padding.Empty,
            Tag = new DeferredModule(factory)
        };
        page.Controls.Add(new Label
        {
            Text = $"Preparando {name}\u2026",
            Dock = DockStyle.Fill,
            ForeColor = DesktopTheme.Muted,
            BackColor = DesktopTheme.Background,
            Font = DesktopTheme.Font(11),
            TextAlign = ContentAlignment.MiddleCenter
        });
        return page;
    }

    private static TabPage MaterializarModulo(TabPage target)
    {
        if (target.Tag is not DeferredModule deferred)
            return target;

        var built = deferred.Factory();
        target.SuspendLayout();
        foreach (Control existing in target.Controls.Cast<Control>().ToArray())
            existing.Dispose();
        target.Controls.Clear();

        foreach (var control in built.Controls.Cast<Control>().ToArray())
        {
            built.Controls.Remove(control);
            target.Controls.Add(control);
        }

        target.BackColor = built.BackColor;
        target.Padding = built.Padding;
        target.Tag = built.Tag;
        target.ResumeLayout(true);
        built.Dispose();
        return target;
    }

    private BufferedFlowLayoutPanel CrearNavegacionPaginada(
        DataGridView grid,
        out Label indicadorPagina)
    {
        var navegacion = new BufferedFlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(0, 6, 0, 0)
        };
        indicadorPagina = new Label
        {
            AutoSize = true,
            Text = "Página 1 · 10 por página",
            ForeColor = DesktopTheme.Muted,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            Margin = new Padding(10, 10, 4, 0)
        };
        var paginador = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = DesktopTheme.Surface,
            Padding = new Padding(0, 0, 0, 4)
        };
        var layout = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.Controls.Add(navegacion, 1, 0);
        paginador.Controls.Add(layout);
        if (grid.Parent is Control tarjetaTabla)
            tarjetaTabla.Controls.Add(paginador);

        return navegacion;
    }

    private (TabPage Page, DataGridView Grid, FlowLayoutPanel Toolbar) CrearPagina(string name)
    {
        var (title, description) = ObtenerInformacionModulo(name);
        var page = new TabPage(name)
        {
            BackColor = DesktopTheme.Background,
            Padding = Padding.Empty
        };

        var content = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = DesktopTheme.Background,
            Padding = new Padding(28, 24, 28, 24)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 154));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var heading = new WebGradientPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 18, 24, 16),
            Margin = new Padding(0, 0, 0, 14)
        };
        heading.Resize += (_, _) => DesktopTheme.SetRoundedRegion(heading, 16);
        var heroLayout = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        heroLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        heroLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        heroLayout.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            Font = DesktopTheme.TitleFont(23),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        heroLayout.Controls.Add(new Label
        {
            Text = description,
            Dock = DockStyle.Fill,
            Font = DesktopTheme.Font(10),
            ForeColor = Color.FromArgb(222, 245, 255),
            TextAlign = ContentAlignment.TopLeft
        }, 0, 1);
        var recordCount = new Label
        {
            Text = "Sin datos cargados",
            Dock = DockStyle.Fill,
            Font = DesktopTheme.Font(9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(42, 255, 255, 255),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(18, 10, 0, 10)
        };
        recordCount.Resize += (_, _) => DesktopTheme.SetRoundedRegion(recordCount, 12);
        heroLayout.Controls.Add(recordCount, 1, 0);
        heroLayout.SetRowSpan(recordCount, 2);
        heading.Controls.Add(heroLayout);

        var actionSection = new SurfaceCard
        {
            Dock = DockStyle.Fill,
            BackColor = DesktopTheme.Surface,
            Padding = new Padding(18, 10, 18, 10),
            Margin = new Padding(0, 0, 0, 14)
        };
        var actionLayout = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        actionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        actionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        actionLayout.Controls.Add(new Label
        {
            Text = "ACCIONES RÁPIDAS",
            Dock = DockStyle.Fill,
            Font = DesktopTheme.Font(8, FontStyle.Bold),
            ForeColor = DesktopTheme.Primary,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        var toolbar = new BufferedFlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 3, 0, 0),
            AutoScroll = false,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent
        };
        actionLayout.Controls.Add(toolbar, 0, 1);
        actionSection.Controls.Add(actionLayout);

        var grid = new BufferedDataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToOrderColumns = false,
            AllowUserToResizeColumns = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ScrollBars = ScrollBars.Vertical,
            BackgroundColor = DesktopTheme.Surface,
            Margin = Padding.Empty
        };
        DesktopTheme.StyleGrid(grid);
        grid.Tag = recordCount;
        grid.DataBindingComplete += (_, _) => ConfigurarColumnas(grid);
        grid.CellFormatting += (_, eventArgs) => FormatearCeldaEstado(grid, eventArgs);
        grid.CellDoubleClick += (_, eventArgs) =>
        {
            if (eventArgs.RowIndex >= 0)
                MostrarDetallesRegistro(grid, name);
        };
        grid.Paint += (_, eventArgs) =>
        {
            if (grid.Rows.Count != 0)
                return;
            TextRenderer.DrawText(
                eventArgs.Graphics,
                "No hay registros para mostrar.\nUsa una acción de arriba para consultar información.",
                _emptyGridFont,
                grid.ClientRectangle,
                DesktopTheme.Muted,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.WordBreak);
        };

        var gridCard = new SurfaceCard
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(2),
            Margin = Padding.Empty
        };
        gridCard.Controls.Add(grid);
        content.Controls.Add(heading, 0, 0);
        content.Controls.Add(actionSection, 0, 1);
        content.Controls.Add(gridCard, 0, 2);
        page.Controls.Add(content);
        return (page, grid, toolbar);
    }

    private void MostrarDetallesRegistro(DataGridView grid, string moduleName)
    {
        if (grid.CurrentRow is null)
            return;

        var details = grid.Columns
            .Cast<DataGridViewColumn>()
            .Where(column => column.Visible)
            .Select(column => new DetailItem(
                column.HeaderText,
                grid.CurrentRow.Cells[column.Index].Value?.ToString() ?? "Sin información"))
            .ToArray();

        using var dialog = new DetailsDialog($"Detalles · {moduleName}", details);
        dialog.ShowDialog(this);
    }

    private static (string Title, string Description) ObtenerInformacionModulo(string name) =>
        name switch
        {
            "Solicitudes" => ("Solicitudes de préstamo", "Revisa las solicitudes pendientes y aprueba o rechaza la fila seleccionada."),
            "Préstamos" => ("Préstamos", "Consulta préstamos activos o vencidos y gestiona incidencias."),
            "Devoluciones" => ("Devoluciones", "Selecciona un préstamo activo para registrar la devolución del material."),
            "Catálogo" => ("Catálogo de libros", "Busca, registra y actualiza la información bibliográfica."),
            "Inventario" => ("Inventario", "Controla existencias, ejemplares y cambios de disponibilidad."),
            "Multas" => ("Multas", "Consulta multas por estado y registra pagos o resoluciones."),
            "Auditoría" => ("Auditoría", "Consulta el historial de acciones realizadas en el sistema."),
            "Reportes" => ("Reportes", "Genera consultas de inventario, préstamos, multas y demanda."),
            "Usuarios" => ("Usuarios", "Administra las cuentas y su estado de acceso."),
            "Empleados" => ("Empleados", "Relaciona cuentas de usuario con cargos del personal."),
            "Administradores" => ("Administradores", "Gestiona los perfiles con acceso administrativo."),
            "Cargos" => ("Cargos", "Crea y actualiza los cargos institucionales."),
            "Roles y permisos" => ("Roles y permisos", "Configura el nivel de acceso de cada cuenta."),
            _ => (name, "Consulta y administra la información de este módulo.")
        };

    private static void ConfigurarColumnas(DataGridView grid)
    {
        foreach (DataGridViewColumn column in grid.Columns)
        {
            column.HeaderText = ConvertirEncabezado(column.Name);
            column.HeaderCell.ToolTipText = column.HeaderText;
            if (column.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                column.MinimumWidth = 58;
                column.FillWeight = 42;
            }
            else if (EsColumnaId(column.Name))
            {
                column.MinimumWidth = 86;
                column.FillWeight = 72;
            }
            else
            {
                column.MinimumWidth = 96;
                column.FillWeight = 120;
            }
            if (column.Name.Contains("descripcion", StringComparison.OrdinalIgnoreCase) ||
                column.Name.Contains("observacion", StringComparison.OrdinalIgnoreCase) ||
                column.Name.Contains("motivo", StringComparison.OrdinalIgnoreCase) ||
                column.Name.Contains("titulo", StringComparison.OrdinalIgnoreCase))
                column.FillWeight = 185;
            if (column.Name.Contains("fecha", StringComparison.OrdinalIgnoreCase))
                column.FillWeight = 145;
        }
    }

    private static bool EsColumnaId(string name) =>
        name.Equals("id", StringComparison.OrdinalIgnoreCase) ||
        name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("id_", StringComparison.OrdinalIgnoreCase);

    private static string ConvertirEncabezado(string name)
    {
        var especiales = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = "ID",
            ["libroId"] = "ID libro",
            ["usuarioId"] = "ID usuario",
            ["usuarioNombre"] = "Usuario",
            ["nombreCompleto"] = "Nombre completo",
            ["email"] = "Correo electrónico",
            ["cargo"] = "Cargo",
            ["personalAsignado"] = "Personal asignado",
            ["correosAsociados"] = "Correos asociados",
            ["prestamoId"] = "ID préstamo",
            ["solicitudPrestamoId"] = "ID solicitud",
            ["libroTitulo"] = "Libro solicitado",
            ["empleadoResolucionId"] = "Empleado responsable",
            ["cantidadTotal"] = "Total",
            ["cantidadDisponible"] = "Disponibles",
            ["cantidadPrestada"] = "Prestados",
            ["fechaGeneracion"] = "Fecha de generación",
            ["fechaResolucion"] = "Fecha de resolución",
            ["observacionResolucion"] = "Observación",
            ["tipoUsuario"] = "Tipo de usuario",
            ["numeroTelefono"] = "Teléfono"
        };
        if (especiales.TryGetValue(name, out var header))
            return header;

        var text = System.Text.RegularExpressions.Regex.Replace(
            name.Replace('_', ' '),
            "([a-záéíóúñ])([A-Z])",
            "$1 $2");
        return string.IsNullOrWhiteSpace(text)
            ? name
            : char.ToUpperInvariant(text[0]) + text[1..];
    }

    private void FormatearCeldaEstado(
        DataGridView grid,
        DataGridViewCellFormattingEventArgs eventArgs)
    {
        if (eventArgs.RowIndex < 0 ||
            !grid.Columns[eventArgs.ColumnIndex].Name.Equals(
                "estado",
                StringComparison.OrdinalIgnoreCase))
            return;

        eventArgs.CellStyle.ForeColor = eventArgs.Value?.ToString() switch
        {
            "Pendiente" or "Vencido" => Color.FromArgb(146, 91, 0),
            "Aprobada" or "Activo" or "Pagada" or "Resuelta" =>
                DesktopTheme.Success,
            "Rechazada" or "Cancelada" or "Suspendido" =>
                DesktopTheme.Danger,
            _ => DesktopTheme.Text
        };
        eventArgs.CellStyle.Font = _gridStatusFont;
    }

    private void AgregarBoton(FlowLayoutPanel toolbar, string text, Func<Task> action)
    {
        var button = new AnimatedButton
        {
            Text = $"{ObtenerIconoAccion(text)}  {text}",
            AutoSize = true,
            Height = 38,
            Margin = new Padding(4, 0, 4, 0)
        };
        if (text.Contains("Eliminar", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Rechazar", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Cancelar", StringComparison.OrdinalIgnoreCase))
            DesktopTheme.StyleDangerButton(button);
        else if (text.Contains("Actualizar", StringComparison.OrdinalIgnoreCase) ||
                 text.Contains("Buscar", StringComparison.OrdinalIgnoreCase) ||
                 text.StartsWith("Por ", StringComparison.OrdinalIgnoreCase) ||
                 text.Contains("Historial", StringComparison.OrdinalIgnoreCase) ||
                 text is "Todo" or "Activos" or "Vencidos" or "Pendientes" or "Pagadas" or "Resueltas")
            DesktopTheme.StyleSecondaryButton(button);
        else
            DesktopTheme.StylePrimaryButton(button);
        button.Click += async (_, _) =>
        {
            button.Enabled = false;
            try { await ProtegerAsync(action); }
            finally { button.Enabled = true; }
        };
        _toolTips.SetToolTip(button, ObtenerAyudaAccion(text));
        toolbar.Controls.Add(button);
    }

    private async Task CargarAsync(
        DataGridView grid,
        string endpoint,
        bool forceRefresh = false)
    {
        _status.Text = "Actualizando información del módulo…";
        var json = forceRefresh
            ? await _api.GetFreshAsync(endpoint)
            : await _api.GetAsync(endpoint);
        var table = await Task.Run(() => ConvertirEnTabla(json));
        if (endpoint.StartsWith("api/Prestamos", StringComparison.OrdinalIgnoreCase) ||
            endpoint.StartsWith("api/Devoluciones", StringComparison.OrdinalIgnoreCase))
            PrepararTablaPrestamosParaConsulta(table);
        if (endpoint.StartsWith("api/Multas", StringComparison.OrdinalIgnoreCase))
            PrepararTablaMultasParaConsulta(table);
        if (grid is BufferedDataGridView bufferedGrid)
            bufferedGrid.BeginUpdate();
        else
            grid.SuspendLayout();
        try
        {
            grid.DataSource = table;
        }
        finally
        {
            if (grid is BufferedDataGridView completedGrid)
                completedGrid.EndUpdate();
            else
                grid.ResumeLayout(true);
        }
        _status.Text = grid.Rows.Count switch
        {
            0 => "Consulta completada · No hay registros para mostrar",
            1 => "Consulta completada · 1 registro disponible",
            _ => $"Consulta completada · {grid.Rows.Count} registros disponibles"
        };
        if (grid.Tag is Label recordCount)
            recordCount.Text = grid.Rows.Count == 1
                ? "1 registro"
                : $"{grid.Rows.Count} registros";
    }

    private static string ObtenerIconoAccion(string text)
    {
        if (text.Equals("Anterior", StringComparison.OrdinalIgnoreCase)) return "<";
        if (text.Equals("Siguiente", StringComparison.OrdinalIgnoreCase)) return ">";
        if (text.Contains("Actualizar", StringComparison.OrdinalIgnoreCase)) return "↻";
        if (text.Contains("Buscar", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("Por ", StringComparison.OrdinalIgnoreCase)) return "⌕";
        if (text.Contains("Crear", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Nuevo", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Registrar", StringComparison.OrdinalIgnoreCase)) return "+";
        if (text.Contains("Editar", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Ajustar", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Cambiar", StringComparison.OrdinalIgnoreCase)) return "✎";
        if (text.Contains("Eliminar", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Rechazar", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Cancelar", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Remover", StringComparison.OrdinalIgnoreCase)) return "×";
        if (text.Contains("Aprobar", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Resolver", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Asignar", StringComparison.OrdinalIgnoreCase)) return "✓";
        if (text.Contains("Historial", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Ver ", StringComparison.OrdinalIgnoreCase)) return "◉";
        return "•";
    }

    private static string ObtenerAyudaAccion(string text)
    {
        if (text.Contains("seleccionado", StringComparison.OrdinalIgnoreCase) ||
            text is "Aprobar" or "Rechazar" or "Resolver" or "Registrar pago" or "Cancelar")
            return "Esta acción se aplicará a la fila seleccionada.";
        if (text.Contains("Actualizar", StringComparison.OrdinalIgnoreCase))
            return "Vuelve a cargar la información más reciente.";
        if (text.StartsWith("Por ", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Buscar", StringComparison.OrdinalIgnoreCase))
            return "Filtra los registros que aparecen en la tabla.";
        return text;
    }

    private async Task CargarPorIdAsync(DataGridView grid, string entity, string endpoint)
    {
        var id = PedirId(entity);
        if (id is not null)
            await CargarAsync(grid, string.Format(endpoint, id.Value));
    }

    private async Task EjecutarAsync(Func<Task<JsonElement>> action, string success)
    {
        _status.Text = "Guardando los cambios…";
        await action();
        _status.Text = $"{success} correctamente";
        MessageBox.Show(
            this,
            $"{success} correctamente.\n\nLa información ya fue guardada y puedes continuar.",
            "Acción completada",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private async Task ProtegerAsync(Func<Task> action)
    {
        try { await action(); }
        catch (DesktopSessionExpiredException exception)
        {
            MessageBox.Show(
                exception.Message,
                "Sesión finalizada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            CerrarSesionSolicitado = true;
            Close();
        }
        catch (Exception exception)
        {
            var message = ObtenerMensajeParaUsuario(exception);
            _status.Text = "La acción no pudo completarse";
            MessageBox.Show(
                this,
                $"{message}\n\nNo se realizaron cambios.",
                "No pudimos completar la acción",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static string ObtenerMensajeParaUsuario(Exception exception)
    {
        if (exception is TaskCanceledException)
            return "La operación tardó más de lo esperado. Verifica tu conexión e inténtalo nuevamente.";

        if (exception is HttpRequestException &&
            (exception.Message.Contains("actively refused", StringComparison.OrdinalIgnoreCase) ||
             exception.Message.Contains("No connection could be made", StringComparison.OrdinalIgnoreCase) ||
             exception.InnerException is System.Net.Sockets.SocketException))
            return "No fue posible conectarse con SIGEBI. Confirma que la API esté encendida e inténtalo nuevamente.";

        return string.IsNullOrWhiteSpace(exception.Message)
            ? "Ocurrió un problema inesperado. Inténtalo nuevamente."
            : exception.Message.Trim();
    }

    private Dictionary<string, object?>? Pedir(string title, params InputField[] fields)
    {
        using var dialog = new OperationDialog(title, fields);
        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.ObtenerValores() : null;
    }

    private int? PedirId(string entity)
    {
        var values = Pedir($"Consultar {entity}", Entero("id", $"ID de {entity}"));
        return values is null ? null : ObtenerEntero(values, "id");
    }

    private int? ObtenerIdSeleccionado(DataGridView grid, string entity)
    {
        if (grid.CurrentRow is not null)
        {
            var column = grid.Columns
                .Cast<DataGridViewColumn>()
                .FirstOrDefault(item =>
                    item.Name.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                    item.Name.Equals($"id{entity}", StringComparison.OrdinalIgnoreCase));
            if (column is not null &&
                int.TryParse(
                    grid.CurrentRow.Cells[column.Index].Value?.ToString(),
                    out var selectedId))
                return selectedId;
        }

        MessageBox.Show(
            this,
            $"Para continuar, selecciona una fila de {entity.ToLowerInvariant()} en la tabla y vuelve a presionar la acción.",
            "Falta seleccionar un registro",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        return null;
    }

    private bool ValidarFilaPendiente(DataGridView grid, string action)
    {
        if (grid.CurrentRow is null)
        {
            MessageBox.Show(
                this,
                "Selecciona una solicitud pendiente en la tabla y vuelve a intentarlo.",
                "Falta seleccionar una solicitud",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        var state = ObtenerCelda(grid, "estado");
        if (state.Equals("Pendiente", StringComparison.OrdinalIgnoreCase))
            return true;

        MessageBox.Show(
            this,
            $"Esta solicitud ya está en estado «{state}».\n\nSolo puedes {action} solicitudes que todavía estén pendientes.",
            "La solicitud no está pendiente",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        return false;
    }

    private static string ObtenerCelda(DataGridView grid, string columnName)
    {
        if (grid.CurrentRow is null)
            return string.Empty;
        var column = grid.Columns
            .Cast<DataGridViewColumn>()
            .FirstOrDefault(item =>
                item.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
        return column is null
            ? string.Empty
            : grid.CurrentRow.Cells[column.Index].Value?.ToString() ?? string.Empty;
    }

    private bool Confirmar(string message) =>
        MessageBox.Show(
            this,
            message,
            "Confirmar operación",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes;

    private Dictionary<string, object?>? PedirLibro(
        string title,
        DataGridView? grid = null,
        bool incluirDatosRegistro = false)
    {
        var fields = new List<InputField>
        {
            Texto("titulo", "Título", grid is null ? "" : ObtenerCelda(grid, "titulo")),
            Texto("autor", "Autor", grid is null ? "" : ObtenerCelda(grid, "autor"))
        };
        if (incluirDatosRegistro)
            fields.Add(Texto("isbn", "ISBN"));
        fields.Add(Texto(
            "genero",
            "Género",
            grid is null ? "" : ObtenerCelda(grid, "genero")));
        fields.Add(Texto(
            "editorial",
            "Editorial",
            grid is null ? "" : ObtenerCelda(grid, "editorial")));
        fields.Add(Texto(
            "descripcion",
            "Descripci\u00F3n",
            grid is null ? "" : ObtenerCelda(grid, "descripcion")));
        if (incluirDatosRegistro)
            fields.Add(Entero(
                "numeroEjemplares",
                "Cantidad de ejemplares",
                "1"));
        return Pedir(title, fields.ToArray());
    }

    private static int ObtenerEntero(Dictionary<string, object?> values, string name) =>
        Convert.ToInt32(values[name]);

    private static InputField Texto(string name, string label, string defaultValue = "") =>
        new(name, label, InputKind.Text, defaultValue);
    private static InputField Password(string name, string label) =>
        new(name, label, InputKind.Password);
    private static InputField Entero(string name, string label, string defaultValue = "") =>
        new(name, label, InputKind.Integer, defaultValue);
    private static InputField Decimal(string name, string label, string defaultValue = "") =>
        new(name, label, InputKind.Decimal, defaultValue);
    private static InputField Fecha(string name, string label) =>
        new(name, label, InputKind.DateTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
    private static InputField Seleccion(
        string name,
        string label,
        string defaultValue,
        params string[] options) =>
        new(name, label, InputKind.Select, defaultValue, options);

    private static DataTable ConvertirEnTabla(JsonElement json)
    {
        var table = new DataTable();
        var rows = json.ValueKind == JsonValueKind.Array
            ? json.EnumerateArray().ToArray()
            : [json];
        var objects = rows.Where(row => row.ValueKind == JsonValueKind.Object).ToArray();
        var columns = objects
            .SelectMany(row => row.EnumerateObject().Select(property => property.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var column in columns)
            table.Columns.Add(column);

        foreach (var item in objects)
        {
            var row = table.NewRow();
            foreach (var property in item.EnumerateObject())
                row[property.Name] = FormatearValor(property);
            table.Rows.Add(row);
        }
        return table;
    }

    private static void PrepararTablaPrestamosParaConsulta(DataTable table)
    {
        // Estos identificadores se conservan en la respuesta para operaciones internas,
        // pero no aportan contexto al personal en las pantallas de consulta.
        foreach (var column in new[]
                 {
                     "usuarioId", "libroId", "ejemplarId", "solicitudPrestamoId",
                     "empleadoPrestamoId", "empleadoDevolucionId"
                 })
        {
            if (table.Columns.Contains(column))
                table.Columns.Remove(column);
        }
    }

    private static void PrepararTablaMultasParaConsulta(DataTable table)
    {
        // La multa conserva su ID para registrar pagos o resoluciones. Los demás
        // identificadores son técnicos y se sustituyen por datos comprensibles.
        foreach (var column in new[] { "usuarioId", "prestamoId", "empleadoResolucionId" })
        {
            if (table.Columns.Contains(column))
                table.Columns.Remove(column);
        }
    }

    private static object FormatearValor(JsonProperty property)
    {
        if (property.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return DBNull.Value;
        if (property.Value.ValueKind != JsonValueKind.String)
            return property.Value.ToString();

        var value = property.Value.GetString() ?? string.Empty;
        if (property.Name.Contains("fecha", StringComparison.OrdinalIgnoreCase) &&
            DateTimeOffset.TryParse(value, out var date))
            return date.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
        return value;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _toolTips.Dispose();
            _gridStatusFont.Dispose();
            _emptyGridFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
