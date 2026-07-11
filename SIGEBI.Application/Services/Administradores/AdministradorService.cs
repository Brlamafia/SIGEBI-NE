using AutoMapper;
using SIGEBI.Application.Dtos.Administradores;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Administradores;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using AuditoriaEntidad = SIGEBI.Domain.Entities.Auditoria.Auditoria;

namespace SIGEBI.Application.Services.Administradores;

public sealed class AdministradorService : IAdministradorService
{
    private readonly IAdministradorRepository _administradores;
    private readonly IUsuarioRepository _usuarios;
    private readonly ICargoRepository _cargos;
    private readonly IAuditoriaRepository _auditorias;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AdministradorService(
        IAdministradorRepository administradores,
        IUsuarioRepository usuarios,
        ICargoRepository cargos,
        IAuditoriaRepository auditorias,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _administradores = administradores;
        _usuarios = usuarios;
        _cargos = cargos;
        _auditorias = auditorias;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<AdministradorDto>> ObtenerTodosAsync(
        CancellationToken cancellationToken = default)
        => _mapper.Map<IReadOnlyCollection<AdministradorDto>>(
            await _administradores.ObtenerTodosAsync(cancellationToken));

    public async Task<AdministradorDto> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default)
        => _mapper.Map<AdministradorDto>(
            await _administradores.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Administrador), id));

    public async Task<AdministradorDto> CrearAsync(
        SaveAdministradorDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidarResponsable(dto.UsuarioResponsableId);
        Administrador? creado = null;

        await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
        {
            await ValidarRelacionesAsync(dto.UsuarioId, dto.CargoId, ct);
            if (await _administradores.ObtenerPorUsuarioIdAsync(dto.UsuarioId, ct) is not null)
                throw new BusinessRuleException("El usuario ya está registrado como administrador.");

            creado = new Administrador(dto.UsuarioId, dto.CargoId);
            await _administradores.AgregarAsync(creado, ct);
            await RegistrarAuditoriaAsync(dto.UsuarioResponsableId, AccionAuditoria.Registrar,
                $"Registro del administrador para el usuario {dto.UsuarioId}.", ct);
        }, cancellationToken);

        return _mapper.Map<AdministradorDto>(creado!);
    }

    public async Task<AdministradorDto> ActualizarAsync(
        int id,
        UpdateAdministradorDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidarResponsable(dto.UsuarioResponsableId);
        Administrador? actualizado = null;

        await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
        {
            actualizado = await _administradores.ObtenerPorIdAsync(id, ct)
                ?? throw new NotFoundException(nameof(Administrador), id);
            if (await _cargos.ObtenerPorIdAsync(dto.CargoId, ct) is null)
                throw new NotFoundException(nameof(Cargo), dto.CargoId);

            actualizado.ActualizarCargo(dto.CargoId);
            _administradores.Actualizar(actualizado);
            await RegistrarAuditoriaAsync(dto.UsuarioResponsableId, AccionAuditoria.Ajustar,
                $"Actualización del cargo del administrador {id} a {dto.CargoId}.", ct);
        }, cancellationToken);

        return _mapper.Map<AdministradorDto>(actualizado!);
    }

    private async Task ValidarRelacionesAsync(int usuarioId, int cargoId, CancellationToken ct)
    {
        if (await _usuarios.ObtenerPorIdAsync(usuarioId, ct) is null)
            throw new NotFoundException(nameof(Usuario), usuarioId);
        if (await _cargos.ObtenerPorIdAsync(cargoId, ct) is null)
            throw new NotFoundException(nameof(Cargo), cargoId);
    }

    private Task RegistrarAuditoriaAsync(
        int responsableId,
        AccionAuditoria accion,
        string descripcion,
        CancellationToken ct)
        => _auditorias.AgregarAsync(new AuditoriaEntidad(
            responsableId,
            ModuloAuditoria.Administracion,
            accion,
            descripcion,
            ResultadoAuditoria.Exitoso), ct);

    private static void ValidarResponsable(int responsableId)
    {
        if (responsableId <= 0)
            throw new BusinessRuleException("Debe indicar el usuario responsable.");
    }
}
