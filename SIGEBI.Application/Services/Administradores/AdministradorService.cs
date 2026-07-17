using AutoMapper;
using Microsoft.Extensions.Logging;
using SIGEBI.Application.Dtos.Administradores;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Administradores;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuditoriaEntidad = SIGEBI.Domain.Entities.Auditoria.Auditoria;

namespace SIGEBI.Application.Services.Administradores
{
    public sealed class AdministradorService : IAdministradorService
    {
        private readonly IAdministradorRepository _administradores;
        private readonly IUsuarioRepository _usuarios;
        private readonly ICargoRepository _cargos;
        private readonly IAuditoriaRepository _auditorias;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AdministradorService> _logger;

        public AdministradorService(
            IAdministradorRepository administradores,
            IUsuarioRepository usuarios,
            ICargoRepository cargos,
            IAuditoriaRepository auditorias,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AdministradorService> logger)
        {
            _administradores = administradores;
            _usuarios = usuarios;
            _cargos = cargos;
            _auditorias = auditorias;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<AdministradorDto>> ObtenerTodosAsync(CancellationToken ct = default)
            => _mapper.Map<IReadOnlyCollection<AdministradorDto>>(await _administradores.ObtenerTodosAsync(ct));

        public async Task<AdministradorDto> ObtenerPorIdAsync(int id, CancellationToken ct = default)
            => _mapper.Map<AdministradorDto>(await _administradores.ObtenerPorIdAsync(id, ct) ?? throw new NotFoundException(nameof(Administrador), id));

        public async Task<AdministradorDto> CrearAsync(SaveAdministradorDto dto, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ValidarResponsable(dto.UsuarioResponsableId);
            try
            {
                Administrador? creado = null;
                await _unitOfWork.EjecutarEnTransaccionAsync(async c =>
                {
                    await ValidarRelacionesAsync(dto.UsuarioId, dto.CargoId, c);
                    if (await _administradores.ObtenerPorUsuarioIdAsync(dto.UsuarioId, c) is not null)
                        throw new BusinessRuleException("El usuario ya está registrado como administrador.");

                    creado = new Administrador(dto.UsuarioId, dto.CargoId);
                    await _administradores.AgregarAsync(creado, c);
                    await RegistrarAuditoriaAsync(dto.UsuarioResponsableId, AccionAuditoria.Registrar, $"Registro administrador {dto.UsuarioId}.", c);
                }, ct);
                return _mapper.Map<AdministradorDto>(creado!);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error al crear administrador para usuario {Id}", dto.UsuarioId); throw; }
        }

        public async Task<AdministradorDto> ActualizarAsync(int id, UpdateAdministradorDto dto, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ValidarResponsable(dto.UsuarioResponsableId);
            try
            {
                Administrador? actualizado = null;
                await _unitOfWork.EjecutarEnTransaccionAsync(async c =>
                {
                    actualizado = await _administradores.ObtenerPorIdAsync(id, c) ?? throw new NotFoundException(nameof(Administrador), id);
                    if (await _cargos.ObtenerPorIdAsync(dto.CargoId, c) is null) throw new NotFoundException(nameof(Cargo), dto.CargoId);

                    actualizado.ActualizarCargo(dto.CargoId);
                    _administradores.Actualizar(actualizado);
                    await RegistrarAuditoriaAsync(dto.UsuarioResponsableId, AccionAuditoria.Ajustar, $"Actualización cargo administrador {id}.", c);
                }, ct);
                return _mapper.Map<AdministradorDto>(actualizado!);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error al actualizar administrador {Id}", id); throw; }
        }

        private async Task ValidarRelacionesAsync(int usuarioId, int cargoId, CancellationToken ct)
        {
            if (await _usuarios.ObtenerPorIdAsync(usuarioId, ct) is null) throw new NotFoundException(nameof(Usuario), usuarioId);
            if (await _cargos.ObtenerPorIdAsync(cargoId, ct) is null) throw new NotFoundException(nameof(Cargo), cargoId);
        }

        private Task RegistrarAuditoriaAsync(int rId, AccionAuditoria acc, string desc, CancellationToken ct)
            => _auditorias.AgregarAsync(new AuditoriaEntidad(rId, ModuloAuditoria.Administracion, acc, desc, ResultadoAuditoria.Exitoso), ct);

        private static void ValidarResponsable(int rId)
        {
            if (rId <= 0) throw new BusinessRuleException("Debe indicar el usuario responsable.");
        }
    }
}