using TransactionsIngest.Api.Extensions;
using TransactionsIngest.Application;
using TransactionsIngest.Infrastructure;
using TransactionsIngest.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseApi();

app.Run();

public partial class Program;
