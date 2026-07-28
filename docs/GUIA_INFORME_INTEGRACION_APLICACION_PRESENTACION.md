# Guía del informe: integración de Aplicación con Presentación

## Objetivo

Demostrar que el portal Web de SIGEBI consume la API central mediante HTTP y
JWT. La API integra los casos de uso de Application, manteniendo las reglas de
negocio y el acceso a Supabase fuera del proyecto Web.

## Evidencia técnica

Documentar:

1. La ausencia de referencias a `SIGEBI.Persistence` y `SIGEBI.IOC` en
   `SIGEBI.Web.csproj`.
2. El registro de `ISigebiApiClient` mediante `AddHttpClient` en
   `SIGEBI.Web/Program.cs`.
3. Los controladores Web recibiendo `ISigebiApiClient` por constructor.
4. El JWT guardado dentro de la sesión cifrada del portal y enviado como
   `Bearer` a la API.
5. Las vistas recibiendo ViewModels preparados, sin acceso a Supabase.

## Módulos Web

La evidencia debe cubrir:

- Autenticación, registro, Google y recuperación de contraseña.
- Dashboard del lector.
- Catálogo con filtros y disponibilidad.
- Registro y cancelación de solicitudes.
- Préstamos del usuario.
- Multas del usuario.
- Notificaciones y marcado como leídas.
- Perfil y cambio de contraseña.

Todos estos módulos consumen endpoints de la API mediante
`ISigebiApiClient`. Application, Domain y Persistence solamente se ejecutan
detrás de la API central.

## Flujo

```text
Vista Razor
    ↓
Controlador Web
    ↓ ISigebiApiClient + JWT
API central
    ↓
Application
    ↓
Domain
    ↓
Persistence / Supabase
    ↓
API → ViewModel → Vista Razor
```

## Base de datos

El proyecto utiliza **cero migraciones**:

- No existen archivos de migración ni ModelSnapshot.
- No están instalados los paquetes EF Design ni EF Tools.
- API y Web no ejecutan DDL ni cargan datos de demostración al iniciar.
- `GET /health/ready` valida la conexión y el esquema de Supabase en modo de
  solo lectura.

## Verificación

```powershell
dotnet build SIGEBI.slnx -c Release
dotnet test SIGEBI.Tests/SIGEBI.Tests.csproj -c Release --no-build
```

Resultado esperado:

- Cero errores y cero advertencias.
- Todas las pruebas superadas.
- `GET /health/ready` devuelve `Healthy`.
- La prueba Web → API devuelve la respuesta de autenticación de la API.

## División Web/Desktop

El portal Web corresponde al autoservicio de estudiantes y docentes. Desktop
corresponde al personal bibliotecario, administrativo y de auditoría. Ambos
clientes consumen la misma API central; ninguno accede directamente a
Supabase.
