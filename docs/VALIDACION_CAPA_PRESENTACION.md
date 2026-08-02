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
| Detalle bibliográfico y disponibilidad por libro | `Catalogo/Details` mediante `GET api/Libros/{id}` |
| Disponibilidad de ejemplares | inventario y ejemplares consultados desde la capa de aplicación |
| Solicitud de préstamo | acción `Catalogo/Solicitar` con validaciones de negocio |
| Cancelación y seguimiento de solicitudes | `SolicitudesController` |
| Confirmación de operaciones sensibles | diálogo reutilizable `data-confirm` antes de cancelar |
| Préstamos propios | `PrestamosController` |
| Multas propias | `MultasController` |
| Notificaciones propias | `NotificacionesController` |
| Perfil y cambio de contraseña | `CuentaController` |
| Resumen de actividad | `HomeController` |

Los controladores trabajan con `ISigebiApiClient`, la interfaz de consumo propia
de Presentación, mediante inyección de dependencias y reutilizan los DTO de
Application como contratos de intercambio. Las vistas no acceden directamente
a Persistence ni contienen reglas de negocio.

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

## Cumplimiento de los lineamientos de presentación

| Lineamiento | Evidencia en la solución |
| --- | --- |
| Consumo desacoplado de API Web | `ISigebiApiClient` registrado mediante `IHttpClientFactory` |
| DTO y ViewModels | contratos de Application y modelos de `SIGEBI.Web/Models` |
| GET, POST y DELETE en Web | consulta del catálogo y cuenta, registro y cancelación de solicitudes |
| Validación distribuida | DataAnnotations/validación no intrusiva en UI y validadores en Application/API |
| Manejo de 400, 401, 403, 404, 409 y 500 | `SigebiApiClient` y filtro MVC global |
| API caída y timeout | mensajes Web 503/504 y pruebas automatizadas de transporte |
| Componentes reutilizables | Partial Views `_EmptyState` y `_FlashMessages` |
| Flujo UI → API → UI | pruebas `WebApiPresentationTests` y `SigebiApiClientTests` |
| Documento técnico de máximo tres páginas | `docs/DOCUMENTO_TECNICO_CAPA_PRESENTACION.md` |

## Auditoría final de trazabilidad Web

- Todos los métodos de `ISigebiApiClient` tienen consumidores reales en los
  controladores y cobertura automatizada sobre los flujos principales.
- Todas las acciones MVC están vinculadas desde navegación, formularios,
  redirecciones o configuración de autenticación y errores.
- Los recursos propios (`site.css`, `site.js`, logo y sprite de portadas) se
  cargan correctamente; se retiraron Bootstrap, variantes de scripts no
  cargadas, la vista Privacy y estilos de plantilla sin consumidor.
- Las paginaciones conservan el tamaño contractual y comprueban la página
  siguiente sin omitir ni duplicar registros.
- Registro, recuperación y cambio de contraseña comparten los requisitos de
  complejidad aplicados por Application; el cambio exige confirmación.
- El acceso externo distingue una cuenta SIGEBI inexistente de errores de
  configuración, indisponibilidad o timeout y no deriva estos últimos al
  formulario de registro.
- La compilación completa no presenta advertencias, los analizadores de Web no
  reportan hallazgos y las pruebas automatizadas finalizan sin omisiones.
