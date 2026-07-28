using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SIGEBI.API.Exceptions;
using SIGEBI.API.Filters;
using SIGEBI.API.Jobs;
using SIGEBI.API.Logging;
using SIGEBI.API.Security;
using SIGEBI.API.Data;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Domain.Policies;
using SIGEBI.IOC.Injection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

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
builder.Services.AddHostedService<PrestamosVencidosBackgroundService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Debe configurar Jwt:Key.");
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

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SIGEBI API",
        Version = "v1",
        Description = "API central del Sistema de Gestión Bibliotecaria SIGEBI. En desarrollo use POST /api/Auth/login con admin@sigebi.local / Admin123 y copie el token en Authorize."
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActualHttp>();
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

if (app.Environment.IsDevelopment() &&
    builder.Configuration.GetValue("Database:SeedDevelopmentData", false))
    await DevelopmentDataSeeder.SeedAsync(app.Services);

await SecurityDataSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseCors("WebClient");
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
