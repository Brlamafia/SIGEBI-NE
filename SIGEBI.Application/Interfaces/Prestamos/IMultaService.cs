namespace SIGEBI.Application.Interfaces.Prestamos
{
    public interface IMultaService
    {
        Task MarcarComoPagadaAsync(
            int multaId,
            int usuarioResponsableId,
            CancellationToken cancellationToken = default);

        Task ResolverAsync(
            int multaId,
            int empleadoResolucionId,
            DateTime fechaResolucion,
            string observacion,
            CancellationToken cancellationToken = default);
    }
}
