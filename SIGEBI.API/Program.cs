using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SIGEBI.API.Exceptions;
using SIGEBI.API.Filters;
using SIGEBI.API.Jobs;
using SIGEBI.API.Data;
using SIGEBI.IOC.Injection;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

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

builder.Services.AddSigebiDependencies(options =>
    options.UseInMemoryDatabase("SIGEBI_Swagger")
        .ConfigureWarnings(warnings =>
            warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

var app = builder.Build();

await DevelopmentDataSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
