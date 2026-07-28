using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SIGEBI.Persistence.Context;

public sealed class SigebiContextFactory : IDesignTimeDbContextFactory<SigebiContext>
{
    public SigebiContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SIGEBI_DESIGN_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=sigebi;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<SigebiContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new SigebiContext(options);
    }
}
