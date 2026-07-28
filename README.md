# SIGEBI - Biblioteca Nueva Era

SIGEBI está compuesto por una API central en ASP.NET Core, un portal web para
usuarios y una aplicación Windows Forms para el personal bibliotecario.

## Desarrollo local

1. Configure `ConnectionStrings:Supabase` en User Secrets del proyecto
   `SIGEBI.API`.
2. Configure `Jwt:Key` con una clave privada de al menos 32 caracteres. El
   emisor y la audiencia locales ya están definidos en `appsettings.json`.
3. Ejecute `dotnet run --project SIGEBI.API --launch-profile https`.
4. Ejecute `dotnet run --project SIGEBI.Web --launch-profile https`.
5. Abra `https://localhost:7030`.

El portal web integra la capa de aplicación mediante interfaces e inyección de
dependencias, siguiendo el ejercicio de integración entre Presentación y
Aplicación. La aplicación de escritorio consume la API REST central.
Las reglas de negocio permanecen en las capas Application y Domain.
Los orígenes permitidos para los clientes de la API se configuran en
`SIGEBI.API/appsettings*.json`.
Las consultas de catálogo, usuarios y notificaciones aceptan `pagina` y
`tamanoPagina`; el tamaño predeterminado es 50 y el máximo permitido es 200.

## Base de datos

La implementación actual utiliza PostgreSQL mediante Npgsql. Las migraciones
se generan en `SIGEBI.Persistence/Migrations`. Para aplicar cambios:

```powershell
dotnet ef database update --project SIGEBI.Persistence --startup-project SIGEBI.API
```

También se incluye el script idempotente
`SIGEBI.Persistence/Migrations/Scripts/SIGEBI_PostgreSQL_Upgrade.sql` para
entornos donde las migraciones se aplican mediante una consola administrativa.

La carga de datos de demostración está deshabilitada por defecto. Puede
activarse con `Database:SeedDevelopmentData=true` en un entorno de desarrollo
vacío. Las credenciales de demostración son `admin@sigebi.local / Admin123` y
`usuario@sigebi.local / Usuario123`.

Al iniciar la API o el portal web se armonizan de forma idempotente las tablas
heredadas de personal con el modelo actual de cargos y administradores. Al
iniciar la API también se garantiza de forma idempotente la existencia del rol
`Administrador`, el permiso `SIGEBI.ADMIN` y su asignación a los usuarios con
perfil administrativo. Esto evita que una base existente quede sin acceso a la
administración de roles y permisos.

## Recuperación de contraseña por SMTP

El portal genera un enlace protegido de un solo uso, válido durante 30 minutos,
y lo envía mediante el servicio SMTP implementado en Infrastructure. Las
credenciales deben configurarse únicamente mediante User Secrets.

Ejemplo para Gmail:

```powershell
dotnet user-secrets set "Smtp:Enabled" "true" --project SIGEBI.Web
dotnet user-secrets set "Smtp:Host" "smtp.gmail.com" --project SIGEBI.Web
dotnet user-secrets set "Smtp:Port" "587" --project SIGEBI.Web
dotnet user-secrets set "Smtp:Security" "StartTls" --project SIGEBI.Web
dotnet user-secrets set "Smtp:Username" "correo@gmail.com" --project SIGEBI.Web
dotnet user-secrets set "Smtp:Password" "CONTRASENA-DE-APLICACION" --project SIGEBI.Web
dotnet user-secrets set "Smtp:FromEmail" "correo@gmail.com" --project SIGEBI.Web
dotnet user-secrets set "Smtp:FromName" "SIGEBI Nueva Era" --project SIGEBI.Web
```

En Gmail se debe utilizar una contraseña de aplicación, no la contraseña normal
de la cuenta. Si SMTP permanece deshabilitado, Development conserva un enlace
local para probar el resto del flujo.

## Verificación

```powershell
dotnet build SIGEBI.slnx -c Release
dotnet test SIGEBI.Tests/SIGEBI.Tests.csproj -c Release
dotnet ef migrations has-pending-model-changes --project SIGEBI.Persistence --startup-project SIGEBI.API
```
