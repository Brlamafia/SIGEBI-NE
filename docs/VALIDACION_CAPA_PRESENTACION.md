# Validación de la capa de presentación

## Alcance según la arquitectura

La presentación está dividida por tipo de actor:

- **Portal Web:** autoservicio para estudiantes y docentes.
- **Aplicación Desktop:** operación interna para bibliotecarios,
  administradores y auditores.
- **API:** punto de acceso protegido para las operaciones internas y la
  aplicación Desktop.

Por esta separación, la Web no debe administrar inventario, personal, roles,
permisos, devoluciones ni resolución de multas. Esas funciones corresponden al
personal autorizado y permanecen en Desktop/API.

## Cobertura del portal Web

| Necesidad del usuario | Implementación |
| --- | --- |
| Registro como Estudiante o Docente, inicio y cierre de sesión | `AuthController` y vistas `Views/Auth` |
| Acceso con Google | desafío OAuth y finalización de perfil en `AuthController` |
| Recuperación de contraseña | flujo de solicitud y restablecimiento en `AuthController` |
| Consulta y filtros del catálogo | `CatalogoController` y `Views/Catalogo` |
| Disponibilidad de ejemplares | inventario y ejemplares consultados desde la capa de aplicación |
| Solicitud de préstamo | acción `Catalogo/Solicitar` con validaciones de negocio |
| Cancelación y seguimiento de solicitudes | `SolicitudesController` |
| Préstamos propios | `PrestamosController` |
| Multas propias | `MultasController` |
| Notificaciones propias | `NotificacionesController` |
| Perfil y cambio de contraseña | `CuentaController` |
| Resumen de actividad | `HomeController` |

Los controladores trabajan con interfaces de Application mediante inyección de
dependencias. Las vistas no acceden directamente a Persistence ni contienen
reglas de negocio.

## Cobertura de operaciones internas

La aplicación Desktop integra solicitudes, préstamos, devoluciones, catálogo,
inventario, multas, auditoría, administración y reportes mediante la API. La API
aplica autorización por roles y permisos para impedir que un usuario común
ejecute operaciones administrativas.

## Evidencia preparada

La inicialización de presentación es idempotente y deja disponible:

- 10 libros con editoriales y géneros utilizables en los filtros.
- Ejemplares e inventario coherentes para cada libro.
- Un préstamo activo para mostrar “préstamos por completar”.
- Historial de préstamos devueltos.
- Una multa pendiente y multas pagadas.
- Solicitudes pendientes, aprobadas y cerradas para las gráficas.
- Notificaciones asociadas al usuario.

La identidad visual usa el azul solicitado `#286CF7`, azul institucional ITLA,
rojo institucional para estados cerrados/cancelados, verde para aprobaciones y
gris para pendientes. El logo utilizado tiene fondo transparente.

## Resultado

La Web cubre el alcance de usuario definido por el SRS y la arquitectura, y las
funciones administrativas permanecen en la presentación Desktop. Esta
separación evita duplicar responsabilidades y mantiene las reglas centralizadas
en Application/Domain.
