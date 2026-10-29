using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TransactionsIngest.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        if (db.Database.GetMigrations().Any())
        {
            logger.LogInformation("Applying EF Core migrations");
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            // no migrations generated yet (see README) - create the schema straight from the model
            logger.LogWarning("No migrations found, creating database with EnsureCreated");
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }
    }
}
