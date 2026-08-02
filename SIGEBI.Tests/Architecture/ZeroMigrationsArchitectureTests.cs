namespace SIGEBI.Tests.Architecture;

public sealed class ZeroMigrationsArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Persistence_NoContieneMigracionesNiHerramientasDeMigracion()
    {
        var migrations = Path.Combine(
            RepositoryRoot,
            "SIGEBI.Persistence",
            "Migrations");
        Assert.False(Directory.Exists(migrations) &&
            Directory.EnumerateFiles(migrations, "*", SearchOption.AllDirectories).Any());

        var project = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "SIGEBI.Persistence",
            "SIGEBI.Persistence.csproj"));
        Assert.DoesNotContain("EntityFrameworkCore.Design", project);
        Assert.DoesNotContain("EntityFrameworkCore.Tools", project);
        Assert.False(File.Exists(Path.Combine(
            RepositoryRoot,
            "SIGEBI.Persistence",
            "Context",
            "SigebiContextFactory.cs")));
    }

    [Fact]
    public void Web_NoReferenciaPersistenciaNiIoc()
    {
        var project = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "SIGEBI.Web",
            "SIGEBI.Web.csproj"));
        Assert.DoesNotContain("SIGEBI.Persistence", project);
        Assert.DoesNotContain("SIGEBI.IOC", project);

        var program = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "SIGEBI.Web",
            "Program.cs"));
        Assert.DoesNotContain("ConnectionStrings:Supabase", program);
        Assert.DoesNotContain("AddSigebiDependencies", program);
        Assert.Contains("AddHttpClient<ISigebiApiClient, SigebiApiClient>", program);
    }

    [Fact]
    public void Web_CargaJQueryAntesDeLaValidacionNoIntrusiva()
    {
        var layout = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "SIGEBI.Web",
            "Views",
            "Shared",
            "_Layout.cshtml"));
        var validationPartial = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "SIGEBI.Web",
            "Views",
            "Shared",
            "_ValidationScriptsPartial.cshtml"));

        Assert.Contains("jquery.min.js", layout);
        Assert.Contains("jquery.validate.min.js", validationPartial);
        Assert.Contains("jquery.validate.unobtrusive.min.js", validationPartial);
        Assert.True(
            layout.IndexOf("jquery.min.js", StringComparison.Ordinal) <
            layout.IndexOf("RenderSectionAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void Web_ControladoresDeleganElConsumoHttpAlClienteApi()
    {
        var controllersDirectory = Path.Combine(
            RepositoryRoot,
            "SIGEBI.Web",
            "Controllers");
        var controllers = Directory
            .EnumerateFiles(controllersDirectory, "*.cs")
            .Select(File.ReadAllText)
            .ToArray();

        Assert.NotEmpty(controllers);
        Assert.All(controllers, controller =>
        {
            Assert.DoesNotContain("HttpClient", controller);
            Assert.DoesNotContain("SIGEBI.Persistence", controller);
            Assert.DoesNotContain("DbContext", controller);
        });
        Assert.Contains(controllers, controller =>
            controller.Contains("ISigebiApiClient", StringComparison.Ordinal));
    }

    [Fact]
    public void Web_ReutilizaPartialViewsParaEstadosCompartidos()
    {
        var sharedViews = Path.Combine(
            RepositoryRoot,
            "SIGEBI.Web",
            "Views",
            "Shared");
        var layout = File.ReadAllText(Path.Combine(sharedViews, "_Layout.cshtml"));
        var solicitudes = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "SIGEBI.Web",
            "Views",
            "Solicitudes",
            "Index.cshtml"));

        Assert.True(File.Exists(Path.Combine(sharedViews, "_FlashMessages.cshtml")));
        Assert.True(File.Exists(Path.Combine(sharedViews, "_EmptyState.cshtml")));
        Assert.Contains("_FlashMessages", layout);
        Assert.Contains("_EmptyState", solicitudes);
    }

    [Fact]
    public void Web_ConfirmaLaCancelacionAntesDeEnviarElFormulario()
    {
        var layout = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "SIGEBI.Web",
            "Views",
            "Shared",
            "_Layout.cshtml"));
        var solicitudes = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "SIGEBI.Web",
            "Views",
            "Solicitudes",
            "Index.cshtml"));
        var script = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "SIGEBI.Web",
            "wwwroot",
            "js",
            "site.js"));

        Assert.Contains("confirmationDialog", layout);
        Assert.Contains("data-confirm", solicitudes);
        Assert.Contains("showModal", script);
        Assert.Contains("requestSubmit", script);
    }

    [Fact]
    public void Desktop_ConsumeLaApiSinAccederAPersistencia()
    {
        var project = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "SIGEBI.Desktop",
            "SIGEBI.Desktop.csproj"));
        Assert.DoesNotContain("SIGEBI.Persistence", project);
        Assert.DoesNotContain("SIGEBI.IOC", project);

        var login = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "SIGEBI.Desktop",
            "LoginForm.cs"));
        Assert.DoesNotContain("admin@sigebi.local", login);
        Assert.DoesNotContain("Admin123", login);
        Assert.DoesNotContain("URL de la API", login);
        Assert.DoesNotContain("usuario personal", login.ToLowerInvariant());
        Assert.Contains("credenciales institucionales de empleado", login);

        var client = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "SIGEBI.Desktop",
            "ApiClient.cs"));
        Assert.Contains("AuthenticationHeaderValue(\"Bearer\"", client);
        Assert.Contains("DesktopSessionExpiredException", client);

        var main = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "SIGEBI.Desktop",
            "MainForm.cs"));
        Assert.Contains("\"numeroEjemplares\"", main);
        Assert.Contains("_api.PostAsync(\"api/Libros\"", main);
    }

    [Fact]
    public void Arranque_NoEjecutaDdlNiCargaDatos()
    {
        var startupFiles = new[]
        {
            Path.Combine(RepositoryRoot, "SIGEBI.API", "Program.cs"),
            Path.Combine(RepositoryRoot, "SIGEBI.Web", "Program.cs")
        };
        foreach (var file in startupFiles)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("MigrateAsync", content);
            Assert.DoesNotContain("EnsureCreated", content);
            Assert.DoesNotContain("LegacySchemaCompatibility", content);
            Assert.DoesNotContain("CatalogDataSeeder", content);
            Assert.DoesNotContain("SecurityDataSeeder", content);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SIGEBI.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("No se encontró la raíz de SIGEBI.");
    }
}
