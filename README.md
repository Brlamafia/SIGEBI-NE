# SIGEBI - Biblioteca Nueva Era

SIGEBI está compuesto por una API central en ASP.NET Core, un portal web para
usuarios y una aplicación Windows Forms para el personal bibliotecario.

## Desarrollo local

1. Configure `ConnectionStrings:Supabase` en User Secrets del proyecto
   `SIGEBI.API`.
2. Ejecute `dotnet run --project SIGEBI.API --launch-profile https`.
3. Ejecute `dotnet run --project SIGEBI.Web --launch-profile https`.
4. Abra `https://localhost:7030`.

El portal web consume la URL configurada en `SIGEBI.Web/appsettings*.json`.
Los orígenes permitidos se configuran en `SIGEBI.API/appsettings*.json`.

## Base de datos

La implementación actual utiliza PostgreSQL mediante Npgsql. Las migraciones
se generan en `SIGEBI.Persistence/Migrations`. Para aplicar cambios:

```powershell
dotnet ef database update --project SIGEBI.Persistence --startup-project SIGEBI.API
```

La carga de datos de demostración está deshabilitada por defecto. Puede
activarse con `Database:SeedDevelopmentData=true` en un entorno de desarrollo
vacío. Las credenciales de demostración son `admin@sigebi.local / Admin123` y
`usuario@sigebi.local / Usuario123`.
