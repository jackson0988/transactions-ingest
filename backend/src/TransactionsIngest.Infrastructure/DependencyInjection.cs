using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TransactionsIngest.Application.Import;
using TransactionsIngest.Application.Sources;
using TransactionsIngest.Application.Transactions;
using TransactionsIngest.Infrastructure.Import;
using TransactionsIngest.Infrastructure.Persistence;
using TransactionsIngest.Infrastructure.Queries;
using TransactionsIngest.Infrastructure.Sources.FileBased;
using TransactionsIngest.Infrastructure.Sources.Options;

namespace TransactionsIngest.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TransactionsDb")
            ?? throw new InvalidOperationException("Connection string 'TransactionsDb' is not configured.");

        services.AddDbContext<TransactionsDbContext>(o => o.UseSqlite(connectionString));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ImportLock>();
        services.AddScoped<ITransactionImportService, TransactionImportService>();
        services.AddScoped<ITransactionQueryService, TransactionQueryService>();
        services.AddScoped<IImportBatchQueryService, ImportBatchQueryService>();

        AddTransactionSources(services, configuration);

        return services;
    }

    private static void AddTransactionSources(IServiceCollection services, IConfiguration configuration)
    {
        var sourcesOptions = configuration.GetSection(TransactionSourcesOptions.SectionName).Get<TransactionSourcesOptions>()
            ?? new TransactionSourcesOptions();

        var duplicate = sourcesOptions.Sources
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Source system '{duplicate.Key}' is configured more than once.");

        services.AddSingleton<SampleDataAnchor>();

        // swap FileTransactionSource for a real HttpClient-based source per system later, the importer won't care
        foreach (var source in sourcesOptions.Sources.Where(s => s.Enabled))
        {
            services.AddSingleton<ITransactionSource>(sp => new FileTransactionSource(
                source,
                ResolvePath(sourcesOptions.SampleDataPath, sp.GetRequiredService<IHostEnvironment>()),
                sp.GetRequiredService<SampleDataAnchor>(),
                sp.GetRequiredService<ILogger<FileTransactionSource>>()));
        }
    }

    private static string ResolvePath(string path, IHostEnvironment env) =>
        Path.IsPathRooted(path) ? path : Path.Combine(env.ContentRootPath, path);
}
