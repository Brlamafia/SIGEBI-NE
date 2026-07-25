using System.Data;
using System.Text.Json;

namespace SIGEBI.Desktop;

public sealed class MainForm : Form
{
    private readonly ApiClient _api;
    private readonly ToolStripStatusLabel _status = new("Listo");

    public MainForm(ApiClient api)
    {
        _api = api;
        Text = "SIGEBI / NE Library - Gestión bibliotecaria";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1100, 700);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CrearSolicitudes());
        tabs.TabPages.Add(CrearPrestamos());
        tabs.TabPages.Add(CrearDevoluciones());
        tabs.TabPages.Add(CrearCatalogo());
        tabs.TabPages.Add(CrearInventario());
        tabs.TabPages.Add(CrearMultas());
        tabs.TabPages.Add(CrearAuditoria());
        tabs.TabPages.Add(CrearAdministracion());
        tabs.TabPages.Add(CrearReportes());

        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(_status);
        Controls.Add(tabs);
        Controls.Add(statusStrip);
        Shown += async (_, _) => await CargarPrimeraPestanaAsync(tabs);
    }

    private TabPage CrearSolicitudes()
    {
        var (page, grid, toolbar) = CrearPagina("Solicitudes");
        AgregarBoton(toolbar, "Actualizar", () => CargarAsync(grid, "api/SolicitudesPrestamo"));
        AgregarBoton(toolbar, "Aprobar", async () =>
        {
            var values = Pedir("Aprobar solicitud",
                Entero("solicitudPrestamoId", "ID de solicitud"),
                Entero("empleadoPrestamoId", "ID de empleado"),
                Fecha("fechaPrestamo", "Fecha de préstamo"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PostAsync("api/Prestamos", values), "Préstamo aprobado");
            await CargarAsync(grid, "api/SolicitudesPrestamo");
        });
        AgregarBoton(toolbar, "Rechazar", async () =>
        {
            var values = Pedir("Rechazar solicitud",
                Entero("solicitudPrestamoId", "ID de solicitud"),
                Entero("empleadoResponsableId", "ID de empleado"),
                Texto("motivo", "Motivo"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PostAsync("api/Prestamos/solicitudes/rechazar", values), "Solicitud rechazada");
            await CargarAsync(grid, "api/SolicitudesPrestamo");
        });
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/SolicitudesPrestamo"));
        return page;
    }

    private TabPage CrearPrestamos()
    {
        var (page, grid, toolbar) = CrearPagina("Préstamos");
        AgregarBoton(toolbar, "Activos", () => CargarAsync(grid, "api/Prestamos/activos"));
        AgregarBoton(toolbar, "Vencidos", () => CargarAsync(grid, "api/Prestamos/vencidos"));
        AgregarBoton(toolbar, "Por usuario", () => CargarPorIdAsync(grid, "Usuario", "api/Prestamos/usuario/{0}"));
        AgregarBoton(toolbar, "Por libro", () => CargarPorIdAsync(grid, "Libro", "api/Prestamos/libro/{0}"));
        AgregarBoton(toolbar, "Por ejemplar", () => CargarPorIdAsync(grid, "Ejemplar", "api/Prestamos/ejemplar/{0}"));
        AgregarBoton(toolbar, "Cancelar", async () =>
        {
            var values = Pedir("Cancelar préstamo",
                Entero("prestamoId", "ID del préstamo"),
                Entero("empleadoResponsableId", "ID del empleado"),
                Texto("motivo", "Motivo", "Cancelación administrativa"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PostAsync("api/Prestamos/cancelar", values), "Préstamo cancelado");
            await CargarAsync(grid, "api/Prestamos/activos");
        });
        AgregarBoton(toolbar, "Registrar pérdida", async () =>
        {
            var values = Pedir("Registrar pérdida",
                Entero("prestamoId", "ID del préstamo"),
                Entero("empleadoResponsableId", "ID del empleado"),
                Fecha("fechaReporte", "Fecha del reporte"),
                Texto("motivo", "Motivo"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PostAsync("api/Prestamos/perdidas", values), "Pérdida registrada");
            await CargarAsync(grid, "api/Prestamos/activos");
        });
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Prestamos/activos"));
        return page;
    }

    private TabPage CrearDevoluciones()
    {
        var (page, grid, toolbar) = CrearPagina("Devoluciones");
        AgregarBoton(toolbar, "Historial por usuario", () => CargarPorIdAsync(grid, "Usuario", "api/Devoluciones/usuario/{0}"));
        AgregarBoton(toolbar, "Historial por libro", () => CargarPorIdAsync(grid, "Libro", "api/Devoluciones/libro/{0}"));
        AgregarBoton(toolbar, "Registrar devolución", async () =>
        {
            var values = Pedir("Registrar devolución",
                Entero("prestamoId", "ID del préstamo"),
                Entero("empleadoDevolucionId", "ID del empleado"),
                Fecha("fechaRealDevolucion", "Fecha real"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PostAsync("api/Devoluciones", values), "Devolución registrada");
            await CargarAsync(grid, "api/Prestamos/activos");
        });
        AgregarBoton(toolbar, "Devolución con daño", async () =>
        {
            var values = Pedir("Devolución con daño",
                Entero("prestamoId", "ID del préstamo"),
                Entero("empleadoResponsableId", "ID del empleado"),
                Fecha("fechaDevolucion", "Fecha"),
                Texto("motivo", "Descripción del daño"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PostAsync("api/Devoluciones/con-danio", values), "Daño y devolución registrados");
        });
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Prestamos/activos"));
        return page;
    }

    private TabPage CrearCatalogo()
    {
        var (page, grid, toolbar) = CrearPagina("Catálogo");
        AgregarBoton(toolbar, "Actualizar", () => CargarAsync(grid, "api/Libros"));
        AgregarBoton(toolbar, "Buscar", async () =>
        {
            var values = Pedir("Buscar libros", Texto("termino", "Título o autor"));
            if (values is null) return;
            await CargarAsync(grid, $"api/Libros/buscar?termino={Uri.EscapeDataString(values["termino"]?.ToString() ?? "")}");
        });
        AgregarBoton(toolbar, "Nuevo libro", async () =>
        {
            var values = PedirLibro("Registrar libro");
            if (values is null) return;
            await EjecutarAsync(() => _api.PostAsync("api/Libros", values), "Libro registrado");
            await CargarAsync(grid, "api/Libros");
        });
        AgregarBoton(toolbar, "Editar libro", async () =>
        {
            var values = PedirLibro("Actualizar libro", incluirId: true);
            if (values is null) return;
            var id = ObtenerEntero(values, "id");
            values.Remove("id");
            await EjecutarAsync(() => _api.PutAsync($"api/Libros/{id}", values), "Libro actualizado");
            await CargarAsync(grid, "api/Libros");
        });
        AgregarBoton(toolbar, "Eliminar libro", async () =>
        {
            var id = PedirId("Libro");
            if (id is null) return;
            await EjecutarAsync(() => _api.DeleteAsync($"api/Libros/{id}"), "Libro eliminado");
            await CargarAsync(grid, "api/Libros");
        });
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Libros"));
        return page;
    }

    private TabPage CrearInventario()
    {
        var (page, grid, toolbar) = CrearPagina("Inventario");
        AgregarBoton(toolbar, "Actualizar", () => CargarAsync(grid, "api/Reportes/inventario"));
        AgregarBoton(toolbar, "Ver ejemplares", () => CargarPorIdAsync(grid, "Libro", "api/Inventario/libro/{0}/ejemplares"));
        AgregarBoton(toolbar, "Crear inventario", async () =>
        {
            var values = Pedir("Crear inventario",
                Entero("libroId", "ID del libro"),
                Entero("cantidadTotal", "Cantidad total"),
                Entero("usuarioResponsableId", "ID responsable"),
                Texto("motivo", "Motivo", "Inventario inicial"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PostAsync("api/Inventario", values), "Inventario creado");
            await CargarAsync(grid, "api/Reportes/inventario");
        });
        AgregarBoton(toolbar, "Ajustar cantidad", async () =>
        {
            var values = Pedir("Ajustar inventario",
                Entero("inventarioId", "ID del inventario"),
                Entero("nuevaCantidadTotal", "Nueva cantidad total"),
                Entero("usuarioResponsableId", "ID responsable"),
                Texto("motivo", "Motivo"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PutAsync("api/Inventario/ajustar", values), "Inventario ajustado");
            await CargarAsync(grid, "api/Reportes/inventario");
        });
        AgregarBoton(toolbar, "Cambiar estado", async () =>
        {
            var values = Pedir("Cambiar estado de ejemplar",
                Entero("ejemplarId", "ID del ejemplar"),
                Texto("nuevoEstado", "Nuevo estado"),
                Entero("usuarioResponsableId", "ID responsable"),
                Texto("motivo", "Motivo"));
            if (values is null) return;
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
            var values = Pedir("Registrar pago",
                Entero("multaId", "ID de multa"),
                Entero("usuarioResponsableId", "ID responsable"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PutAsync("api/Multas/pagar", values), "Pago registrado");
            await CargarAsync(grid, "api/Multas/estado/Pendiente");
        });
        AgregarBoton(toolbar, "Resolver", async () =>
        {
            var values = Pedir("Resolver multa",
                Entero("multaId", "ID de multa"),
                Entero("empleadoResolucionId", "ID empleado"),
                Fecha("fechaResolucion", "Fecha de resolución"),
                Texto("observacion", "Observación"));
            if (values is null) return;
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
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Auditoria"));
        return page;
    }

    private TabPage CrearAdministracion()
    {
        var (page, grid, toolbar) = CrearPagina("Administración");
        AgregarBoton(toolbar, "Usuarios", () => CargarAsync(grid, "api/Usuarios"));
        AgregarBoton(toolbar, "Empleados", () => CargarAsync(grid, "api/Empleados"));
        AgregarBoton(toolbar, "Administradores", () => CargarAsync(grid, "api/Administradores"));
        AgregarBoton(toolbar, "Roles", () => CargarAsync(grid, "api/Roles"));
        AgregarBoton(toolbar, "Cargos", () => CargarAsync(grid, "api/Cargos"));
        AgregarBoton(toolbar, "Nuevo empleado", async () =>
        {
            var values = Pedir("Registrar empleado", Entero("usuarioId", "ID de usuario"), Entero("cargoId", "ID de cargo"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PostAsync("api/Empleados", values), "Empleado registrado");
            await CargarAsync(grid, "api/Empleados");
        });
        AgregarBoton(toolbar, "Nuevo administrador", async () =>
        {
            var values = Pedir("Registrar administrador",
                Entero("usuarioId", "ID de usuario"),
                Entero("cargoId", "ID de cargo"),
                Entero("usuarioResponsableId", "ID responsable"));
            if (values is null) return;
            await EjecutarAsync(() => _api.PostAsync("api/Administradores", values), "Administrador registrado");
            await CargarAsync(grid, "api/Administradores");
        });
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Usuarios"));
        return page;
    }

    private TabPage CrearReportes()
    {
        var (page, grid, toolbar) = CrearPagina("Reportes");
        AgregarBoton(toolbar, "Inventario", () => CargarAsync(grid, "api/Reportes/inventario"));
        AgregarBoton(toolbar, "Préstamos por período", async () =>
        {
            var values = Pedir("Período del reporte", Fecha("desde", "Desde"), Fecha("hasta", "Hasta"));
            if (values is null) return;
            var desde = Uri.EscapeDataString(((DateTime)values["desde"]!).ToString("O"));
            var hasta = Uri.EscapeDataString(((DateTime)values["hasta"]!).ToString("O"));
            await CargarAsync(grid, $"api/Reportes/prestamos-fecha?desde={desde}&hasta={hasta}");
        });
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Reportes/inventario"));
        return page;
    }

    private async Task CargarPrimeraPestanaAsync(TabControl tabs)
    {
        tabs.Selected += async (_, _) =>
        {
            if (tabs.SelectedTab?.Tag is Func<Task> load)
                await ProtegerAsync(load);
        };
        if (tabs.SelectedTab?.Tag is Func<Task> firstLoad)
            await ProtegerAsync(firstLoad);
    }

    private static (TabPage Page, DataGridView Grid, FlowLayoutPanel Toolbar) CrearPagina(string name)
    {
        var page = new TabPage(name);
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 50,
            Padding = new Padding(8),
            AutoScroll = true,
            WrapContents = false
        };
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White
        };
        page.Controls.Add(grid);
        page.Controls.Add(toolbar);
        return (page, grid, toolbar);
    }

    private void AgregarBoton(FlowLayoutPanel toolbar, string text, Func<Task> action)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 32 };
        button.Click += async (_, _) =>
        {
            button.Enabled = false;
            try { await ProtegerAsync(action); }
            finally { button.Enabled = true; }
        };
        toolbar.Controls.Add(button);
    }

    private async Task CargarAsync(DataGridView grid, string endpoint)
    {
        _status.Text = $"Consultando {endpoint}...";
        var json = await _api.GetAsync(endpoint);
        grid.DataSource = ConvertirEnTabla(json);
        _status.Text = $"{grid.Rows.Count} registro(s) cargados";
    }

    private async Task CargarPorIdAsync(DataGridView grid, string entity, string endpoint)
    {
        var id = PedirId(entity);
        if (id is not null)
            await CargarAsync(grid, string.Format(endpoint, id.Value));
    }

    private async Task EjecutarAsync(Func<Task<JsonElement>> action, string success)
    {
        _status.Text = "Procesando...";
        await action();
        _status.Text = success;
        MessageBox.Show(success, "SIGEBI", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task ProtegerAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception exception)
        {
            _status.Text = "Ocurrió un error";
            MessageBox.Show(exception.Message, "SIGEBI", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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

    private Dictionary<string, object?>? PedirLibro(string title, bool incluirId = false)
    {
        var fields = new List<InputField>();
        if (incluirId) fields.Add(Entero("id", "ID"));
        fields.AddRange([
            Texto("titulo", "Título"),
            Texto("autor", "Autor"),
            Texto("isbn", "ISBN"),
            Texto("genero", "Género"),
            Texto("editorial", "Editorial")
        ]);
        return Pedir(title, fields.ToArray());
    }

    private static int ObtenerEntero(Dictionary<string, object?> values, string name) =>
        Convert.ToInt32(values[name]);

    private static InputField Texto(string name, string label, string defaultValue = "") =>
        new(name, label, InputKind.Text, defaultValue);
    private static InputField Entero(string name, string label, string defaultValue = "") =>
        new(name, label, InputKind.Integer, defaultValue);
    private static InputField Decimal(string name, string label, string defaultValue = "") =>
        new(name, label, InputKind.Decimal, defaultValue);
    private static InputField Fecha(string name, string label) =>
        new(name, label, InputKind.DateTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

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
                row[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                    JsonValueKind.Null => DBNull.Value,
                    JsonValueKind.Undefined => DBNull.Value,
                    _ => property.Value.ToString()
                };
            table.Rows.Add(row);
        }
        return table;
    }
}
