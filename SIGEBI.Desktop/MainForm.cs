using System.Data;
using System.Text.Json;

namespace SIGEBI.Desktop;

public sealed class MainForm : Form
{
    private readonly ApiClient _api;
    private readonly DesktopSession _session;
    private readonly ToolStripStatusLabel _status = new("Listo");

    public MainForm(ApiClient api, DesktopSession session)
    {
        _api = api;
        _session = session;
        Text = "SIGEBI / NE Library - Gestión bibliotecaria";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1100, 700);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        if (session.TieneRol("Administrador") || session.TieneRol("Bibliotecario"))
        {
            tabs.TabPages.Add(CrearSolicitudes());
            tabs.TabPages.Add(CrearPrestamos());
            tabs.TabPages.Add(CrearDevoluciones());
            tabs.TabPages.Add(CrearCatalogo());
            tabs.TabPages.Add(CrearInventario());
            tabs.TabPages.Add(CrearMultas());
        }
        if (session.TieneRol("Administrador") || session.TieneRol("Auditor"))
        {
            tabs.TabPages.Add(CrearAuditoria());
            tabs.TabPages.Add(CrearReportes());
        }
        if (session.TieneRol("Administrador"))
            tabs.TabPages.Add(CrearAdministracion());

        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(_status);
        statusStrip.Items.Add(new ToolStripStatusLabel
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleRight,
            Text = $"{session.Usuario.Nombre} {session.Usuario.Apellido} · {string.Join(", ", session.Roles)}"
        });
        var logout = new ToolStripDropDownButton("Sesión");
        logout.DropDownItems.Add("Cerrar sesión", null, (_, _) =>
        {
            CerrarSesionSolicitado = true;
            Close();
        });
        statusStrip.Items.Add(logout);
        Controls.Add(tabs);
        Controls.Add(statusStrip);
        Shown += async (_, _) => await CargarPrimeraPestanaAsync(tabs);
    }

    public bool CerrarSesionSolicitado { get; private set; }

    private TabPage CrearSolicitudes()
    {
        var (page, grid, toolbar) = CrearPagina("Solicitudes");
        AgregarBoton(toolbar, "Actualizar", () => CargarAsync(grid, "api/SolicitudesPrestamo"));
        AgregarBoton(toolbar, "Aprobar", async () =>
        {
            var solicitudId = ObtenerIdSeleccionado(grid, "Solicitud");
            if (solicitudId is null) return;
            var values = Pedir("Aprobar solicitud",
                Fecha("fechaPrestamo", "Fecha de préstamo"));
            if (values is null) return;
            values["solicitudPrestamoId"] = solicitudId.Value;
            values["empleadoPrestamoId"] = _session.Usuario.Id;
            await EjecutarAsync(() => _api.PostAsync("api/Prestamos", values), "Préstamo aprobado");
            await CargarAsync(grid, "api/SolicitudesPrestamo");
        });
        AgregarBoton(toolbar, "Rechazar", async () =>
        {
            var solicitudId = ObtenerIdSeleccionado(grid, "Solicitud");
            if (solicitudId is null) return;
            var values = Pedir("Rechazar solicitud",
                Texto("motivo", "Motivo"));
            if (values is null) return;
            values["solicitudPrestamoId"] = solicitudId.Value;
            values["empleadoResponsableId"] = _session.Usuario.Id;
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
            await CargarAsync(grid, "api/Prestamos/activos");
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
            var id = ObtenerIdSeleccionado(grid, "Libro");
            if (id is null) return;
            var values = PedirLibro("Actualizar libro");
            if (values is null) return;
            await EjecutarAsync(() => _api.PutAsync($"api/Libros/{id}", values), "Libro actualizado");
            await CargarAsync(grid, "api/Libros");
        });
        AgregarBoton(toolbar, "Eliminar libro", async () =>
        {
            var id = ObtenerIdSeleccionado(grid, "Libro");
            if (id is null || !Confirmar("¿Deseas eliminar el libro seleccionado?"))
                return;
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
                Texto("nuevoEstado", "Nuevo estado"),
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
            var desde = Uri.EscapeDataString(((DateTime)values["desde"]!).ToString("O"));
            var hasta = Uri.EscapeDataString(((DateTime)values["hasta"]!).ToString("O"));
            await CargarAsync(grid, $"api/Auditoria/rango?desde={desde}&hasta={hasta}");
        });
        page.Tag = new Func<Task>(() => CargarAsync(grid, "api/Auditoria"));
        return page;
    }

    private TabPage CrearAdministracion()
    {
        var page = new TabPage("Administración");
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CrearAdministracionUsuarios());
        tabs.TabPages.Add(CrearAdministracionEmpleados());
        tabs.TabPages.Add(CrearAdministracionAdministradores());
        tabs.TabPages.Add(CrearAdministracionCargos());
        tabs.TabPages.Add(CrearAdministracionRoles());
        tabs.Selected += async (_, _) =>
        {
            if (tabs.SelectedTab?.Tag is Func<Task> load)
                await ProtegerAsync(load);
        };
        page.Controls.Add(tabs);
        page.Tag = new Func<Task>(async () =>
        {
            if (tabs.SelectedTab?.Tag is Func<Task> load)
                await load();
        });
        return page;
    }

    private TabPage CrearAdministracionUsuarios()
    {
        var (page, grid, toolbar) = CrearPagina("Usuarios");
        AgregarBoton(toolbar, "Actualizar", () => CargarAsync(grid, "api/Usuarios"));
        AgregarBoton(toolbar, "Crear", async () =>
        {
            var values = Pedir("Crear usuario",
                Texto("nombre", "Nombre"),
                Texto("apellido", "Apellido"),
                Texto("cedula", "Cédula"),
                Texto("telefono", "Teléfono"),
                Texto("email", "Correo"),
                Password("password", "Contraseña"),
                Texto("tipoUsuario", "Tipo", "Estudiante"));
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
                Texto("tipoUsuario", "Tipo", ObtenerCelda(grid, "tipoUsuario")),
                Texto("estado", "Estado", ObtenerCelda(grid, "estado")));
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
        AgregarBoton(toolbar, "Actualizar", () => CargarAsync(grid, "api/Empleados"));
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
        AgregarBoton(toolbar, "Actualizar", () => CargarAsync(grid, "api/Administradores"));
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
        AgregarBoton(toolbar, "Actualizar", () => CargarAsync(grid, "api/Cargos"));
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
        AgregarBoton(toolbar, "Actualizar", () => CargarAsync(grid, "api/Roles"));
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
            var desde = Uri.EscapeDataString(((DateTime)values["desde"]!).ToString("O"));
            var hasta = Uri.EscapeDataString(((DateTime)values["hasta"]!).ToString("O"));
            await CargarAsync(grid, $"api/Reportes/prestamos-fecha?desde={desde}&hasta={hasta}");
        });
        AgregarBoton(toolbar, "Multas por período", async () =>
        {
            var values = Pedir(
                "Período del reporte",
                Fecha("desde", "Desde"),
                Fecha("hasta", "Hasta"));
            if (values is null) return;
            var desde = Uri.EscapeDataString(((DateTime)values["desde"]!).ToString("O"));
            var hasta = Uri.EscapeDataString(((DateTime)values["hasta"]!).ToString("O"));
            await CargarAsync(grid, $"api/Reportes/multas?desde={desde}&hasta={hasta}");
        });
        AgregarBoton(toolbar, "Catálogo y demanda", async () =>
        {
            var values = Pedir(
                "Período del reporte",
                Fecha("desde", "Desde"),
                Fecha("hasta", "Hasta"));
            if (values is null) return;
            var desde = Uri.EscapeDataString(((DateTime)values["desde"]!).ToString("O"));
            var hasta = Uri.EscapeDataString(((DateTime)values["hasta"]!).ToString("O"));
            await CargarAsync(grid, $"api/Reportes/catalogo?desde={desde}&hasta={hasta}");
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

        return PedirId(entity);
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
    private static InputField Password(string name, string label) =>
        new(name, label, InputKind.Password);
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
