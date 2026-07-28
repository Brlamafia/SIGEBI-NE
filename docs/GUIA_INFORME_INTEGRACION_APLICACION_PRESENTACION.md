# Guía del informe: integración de Aplicación con Presentación

## 1. Hoja de presentación

Incluir:

- Instituto Tecnológico de Las Américas (ITLA).
- Nombre de la asignatura.
- Título: **Integración de la capa de Aplicación con la capa de Presentación**.
- Nombre y matrícula del estudiante.
- Nombre del docente.
- Sección.
- Fecha de entrega.

## 2. Objetivo

Demostrar que el portal Web de SIGEBI integra los casos de uso de la capa
Application mediante interfaces e inyección de dependencias, manteniendo las
reglas de negocio fuera de los controladores y las vistas.

## 3. Evidencia técnica de la integración

Capturar o explicar brevemente:

1. Las referencias a `SIGEBI.Application` y `SIGEBI.IOC` en
   `SIGEBI.Web.csproj`.
2. La llamada `AddSigebiDependencies(connectionString)` en
   `SIGEBI.Web/Program.cs`.
3. Un controlador recibiendo interfaces de Application por constructor. Por
   ejemplo, `HomeController`, `CatalogoController` o `AuthController`.
4. Una vista recibiendo un ViewModel ya preparado, sin consultas directas a la
   base de datos.

## 4. Módulos que se deben documentar

El mínimo solicitado es cinco. Para evidenciar la cobertura completa de la
parte Web se recomienda incluir los siguientes ocho:

### Módulo 1. Autenticación y registro

- Captura del inicio de sesión.
- Captura del registro con las opciones Estudiante y Docente.
- Captura de la solicitud de recuperación y del correo recibido mediante SMTP.
  Ocultar la dirección personal si la captura se compartirá.
- Explicar que `AuthController` utiliza `IAuthenticationService`,
  `IUsuarioService`, `IPasswordRecoveryService` e
  `IPasswordResetEmailSender`.
- Indicar que Estudiante permite hasta 3 préstamos por 7 días y Docente hasta
  5 préstamos por 14 días, según la política del dominio.

### Módulo 2. Inicio

- Captura del dashboard autenticado.
- Debe verse el préstamo por completar, la multa pendiente, las notificaciones
  y la gráfica de solicitudes.
- Explicar que `HomeController` integra servicios de usuarios, préstamos,
  multas, solicitudes, catálogo y notificaciones.

### Módulo 3. Catálogo

- Captura de los 10 libros.
- Captura usando los filtros de género y editorial.
- Mostrar disponibilidad y el botón de solicitud.
- Explicar la integración con `ILibroService`,
  `ISolicitudPrestamoService`, `IMultaService` e `IPrestamoService`.

### Módulo 4. Solicitudes

- Captura con solicitudes pendientes, aprobadas y cerradas.
- Mostrar la acción de cancelar una solicitud pendiente.
- Explicar que las reglas de elegibilidad y cancelación se ejecutan en
  Application/Domain.

### Módulo 5. Préstamos

- Captura del préstamo activo y del historial de préstamos devueltos.
- Mostrar fechas, libro y estado.
- Explicar la integración con `IPrestamoService`.

### Módulo 6. Multas

- Captura de la multa pendiente de RD$50.00.
- Captura del historial de multas pagadas.
- Explicar la integración con `IMultaService`.

### Módulo 7. Notificaciones

- Captura de notificaciones leídas y no leídas.
- Mostrar la acción para marcar como leída.
- Explicar la integración con `INotificacionService`.

### Módulo 8. Mi cuenta

- Captura del perfil mostrando el tipo de usuario.
- Captura del formulario de cambio de contraseña.
- Explicar la integración con `IUsuarioService` y el servicio de
  autenticación.

## 5. Flujo que debe explicarse

```text
Vista Razor
    ↓ envía o solicita información
Controlador Web
    ↓ utiliza una interfaz
Capa Application
    ↓ ejecuta el caso de uso
Capa Domain
    ↓ valida las reglas
Persistence / Supabase
    ↓ devuelve el resultado
ViewModel y Vista Razor
```

## 6. Pruebas realizadas

Incluir el resultado de:

```powershell
dotnet build SIGEBI.slnx -c Release
dotnet test SIGEBI.Tests/SIGEBI.Tests.csproj -c Release --no-build
dotnet ef migrations has-pending-model-changes --project SIGEBI.Persistence --startup-project SIGEBI.API --no-build --configuration Release
```

Resultado esperado:

- 0 errores.
- 0 advertencias de compilación.
- 66 pruebas superadas.
- Ningún cambio pendiente en el modelo de migraciones.

## 7. Conclusión sugerida

La capa de Presentación Web quedó integrada con Application mediante interfaces
e inyección de dependencias. Los módulos de autenticación, inicio, catálogo,
solicitudes, préstamos, multas, notificaciones y cuenta reutilizan los casos de
uso y reglas existentes, sin acceder directamente a Persistence desde las
vistas o controladores. La solución conserva la separación de responsabilidades
definida en la arquitectura.

## 8. División con la presentación Desktop

El portal Web corresponde al autoservicio de estudiantes y docentes. La
aplicación Desktop corresponde al personal bibliotecario, administrativo y de
auditoría. La evidencia de Desktop debe cubrir aprobación o rechazo de
solicitudes, formalización de préstamos, devoluciones, inventario, resolución
de multas, administración de roles, auditoría y reportes mediante la API.
