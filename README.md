# SIGEBI - Biblioteca Nueva Era

SIGEBI está compuesto por una API central en ASP.NET Core, un portal web para
lectores y una aplicación Windows Forms para el personal bibliotecario.

## Arquitectura

La API es el único componente autorizado para conectarse a Supabase. Tanto el
portal Web como Desktop consumen la API REST; ningún proyecto de presentación
referencia Persistence ni IOC.

El proyecto trabaja con **cero migraciones de Entity Framework**. La aplicación
no crea tablas, no ejecuta `ALTER TABLE` y no inserta datos de demostración al
iniciar. El esquema y los datos iniciales se administran explícitamente en
Supabase mediante el SQL aprobado por el equipo.

Luego de cargar el esquema, ejecute `database/performance-indexes.sql` desde el
SQL Editor de Supabase. Es un script idempotente de índices y estadísticas; no
es una migración y no modifica los datos existentes.

`GET /health/ready` comprueba la conexión y verifica, en modo de solo lectura,
que Supabase tenga las tablas y columnas requeridas.

## Configuración local

Configure la API sin guardar secretos en archivos versionados:

```powershell
dotnet user-secrets set "ConnectionStrings:Supabase" "CONEXION-SUPABASE" --project SIGEBI.API
dotnet user-secrets set "Jwt:Key" "CLAVE-ALEATORIA-DE-AL-MENOS-32-CARACTERES" --project SIGEBI.API
```

La API usa `https://localhost:7279` y `http://localhost:5297`; el portal usa
`https://localhost:7030`. En Development, la comunicación interna Web → API
usa `http://localhost:5297` para no depender de la confianza del certificado
HTTPS local. En producción se debe configurar `Api:BaseUrl` con la dirección
HTTPS publicada.

Para habilitar Google, configure las credenciales OAuth en Web y la misma clave
privada de comunicación en ambos proyectos:

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "CLIENT-ID" --project SIGEBI.Web
dotnet user-secrets set "Authentication:Google:ClientSecret" "CLIENT-SECRET" --project SIGEBI.Web
dotnet user-secrets set "Authentication:WebClientSecret" "CLAVE-ALEATORIA" --project SIGEBI.Web
dotnet user-secrets set "Authentication:WebClientSecret" "CLAVE-ALEATORIA" --project SIGEBI.API
```

La recuperación de contraseña y el envío SMTP son responsabilidad de la API.
Las credenciales SMTP también deben configurarse mediante User Secrets en
`SIGEBI.API`.

## Ejecución

En dos terminales:

```powershell
dotnet run --project SIGEBI.API --launch-profile https
dotnet run --project SIGEBI.Web --launch-profile https
```

Abra `https://localhost:7030`.

La aplicación del personal se ejecuta en una tercera terminal:

```powershell
$env:SIGEBI_API_URL = "https://localhost:7279"
dotnet run --project SIGEBI.Desktop
```

Desktop autentica contra la API y conserva el token únicamente durante la
sesión actual. Solo permite el acceso a usuarios con rol `Administrador`,
`Bibliotecario` o `Auditor`; las opciones visibles se ajustan al rol. No
contiene credenciales predeterminadas ni se conecta directamente a Supabase.

## Verificación

```powershell
dotnet build SIGEBI.slnx -c Release
dotnet test SIGEBI.Tests/SIGEBI.Tests.csproj -c Release
dotnet format SIGEBI.slnx --verify-no-changes --no-restore
```

El flujo `.github/workflows/quality.yml` ejecuta estas comprobaciones en cada
push a `master` y en cada pull request.
