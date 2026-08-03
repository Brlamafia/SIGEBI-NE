namespace SIGEBI.Application.Interfaces.Prestamos;

// Fachada estable para API, Web y Desktop. La implementación delega cada caso
// de uso al servicio especializado correspondiente.
public interface IPrestamoService :
    IPrestamoConsultaService,
    IPrestamoRegistroService,
    ISolicitudPrestamoDecisionService,
    IPrestamoCancelacionService,
    IPrestamoIncidenciaService,
    IPrestamoMantenimientoService;
