namespace SIGEBI.Application.Dtos.Roles;

public sealed class AsignarRolDto
{
    public int UsuarioId { get; set; }
    public int RolId { get; set; }
}

public sealed class AsignarPermisoDto
{
    public int RolId { get; set; }
    public int PermisoId { get; set; }
}

public sealed class SavePermisoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
}

public sealed class PermisoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
}
