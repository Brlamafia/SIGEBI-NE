using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using SIGEBI.Web.Services;

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
builder.Services.AddTransient<ApiAuthenticationHandler>();
builder.Services.AddHttpClient<ISigebiApiClient, SigebiApiClient>((services, client) =>
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Debe configurar Api:BaseUrl.");
        client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<ApiAuthenticationHandler>();

var app = builder.Build();

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
