using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.DataProtection;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Options;
using SIGEBI.Application.Services.Seguridad;
using SIGEBI.IOC.Injection;
using SIGEBI.Persistence;
using SIGEBI.Web.Data;
using SIGEBI.Web.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
var authentication = builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccesoDenegado";
        options.Cookie.Name = "SIGEBI.Web.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
    })
    .AddCookie("External", options =>
    {
        options.Cookie.Name = "SIGEBI.Web.External";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    });
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) &&
    !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authentication.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SignInScheme = "External";
    });
}
builder.Services.AddAuthorization();
builder.Services.AddDataProtection().SetApplicationName("SIGEBI.Web");
builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(SmtpOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("Supabase")
    ?? throw new InvalidOperationException(
        "Debe configurar ConnectionStrings:Supabase mediante User Secrets.");
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
builder.Services.AddScoped<IUsuarioActual, WebUsuarioActual>();
builder.Services.AddSingleton<IPasswordResetTokenProtector,
    DataProtectionPasswordResetTokenProtector>();
builder.Services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();

var app = builder.Build();

await LegacySchemaCompatibility.EnsureAsync(app.Services);
await CatalogDataSeeder.SeedAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
