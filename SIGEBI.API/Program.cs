using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SIGEBI.API.Exceptions;
using SIGEBI.API.Filters;
using SIGEBI.API.Jobs;
using SIGEBI.API.Logging;
using SIGEBI.API.Security;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Options;
using SIGEBI.Application.Services.Seguridad;
using SIGEBI.Domain.Policies;
using SIGEBI.IOC.Injection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.DataProtection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var logDirectory = builder.Configuration["Logging:FileDirectory"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "logs");
builder.Logging.AddProvider(new DailyFileLoggerProvider(logDirectory));

builder.Services.AddControllers(options =>
    options.Filters.Add<FluentValidationActionFilter>())
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddScoped<FluentValidationActionFilter>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.Configure<PrestamosVencidosOptions>(
    builder.Configuration.GetSection(PrestamosVencidosOptions.SectionName));
builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddHostedService<DatabaseWarmupHostedService>();
builder.Services.AddHostedService<PrestamosVencidosBackgroundService>();
builder.Services.AddDataProtection().SetApplicationName("SIGEBI.API");

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Debe configurar Jwt:Key.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException(
        "Jwt:Key debe contener al menos 32 bytes para firmar tokens de forma segura.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Debe configurar Jwt:Issuer.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Debe configurar Jwt:Audience.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization(options =>
    options.AddPolicy(
        "AdministracionCompleta",
        policy => policy.RequireAssertion(context =>
            context.User.IsInRole("Administrador") ||
            context.User.HasClaim("permission", "SIGEBI.ADMIN"))));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SIGEBI API",
        Version = "v1",
        Description = "API central del Sistema de Gestión Bibliotecaria SIGEBI. Obtenga un JWT mediante POST /api/Auth/login."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Introduzca el token JWT. Swagger agregará automáticamente el prefijo Bearer.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
    });
});

var connectionString = builder.Configuration.GetConnectionString("Supabase")
    ?? throw new InvalidOperationException(
        "Debe configurar ConnectionStrings:Supabase mediante una variable de entorno o User Secrets.");

builder.Services.AddSigebiDependencies(connectionString);
builder.Services.AddSingleton(new AuthenticationOptions
{
    MaxFailedAttempts = builder.Configuration.GetValue(
        "Authentication:MaxFailedAttempts",
        5),
    LockoutMinutes = builder.Configuration.GetValue(
        "Authentication:LockoutMinutes",
        15)
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActualHttp>();
builder.Services.AddSingleton<IPasswordResetTokenProtector,
    DataProtectionPasswordResetTokenProtector>();
builder.Services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
builder.Services.AddCors(options =>
    options.AddPolicy("WebClient", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["https://localhost:7030", "http://localhost:5065"])
            .AllowAnyHeader()
            .AllowAnyMethod()));

var politicaPrestamos = builder.Configuration
    .GetSection("PoliticasPrestamo")
    .Get<PoliticaPrestamosOptions>() ?? new PoliticaPrestamosOptions();
builder.Services.AddSingleton(new PoliticaPrestamos(politicaPrestamos));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseCors("WebClient");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();
app.MapControllers();
app.Run();
