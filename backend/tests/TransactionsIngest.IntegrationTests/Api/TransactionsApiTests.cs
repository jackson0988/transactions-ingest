using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TransactionsIngest.Application.Common.Models;
using TransactionsIngest.Application.Import.Models;
using TransactionsIngest.Application.Transactions.Models;
using TransactionsIngest.IntegrationTests.Fixtures;

namespace TransactionsIngest.IntegrationTests.Api;

public sealed class TransactionsApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Import_then_list_returns_transactions()
    {
        var importResponse = await _client.PostAsync("/api/import", null);
        importResponse.EnsureSuccessStatusCode();
        var import = await importResponse.Content.ReadFromJsonAsync<ImportResult>(Json);

        var page = await _client.GetFromJsonAsync<PagedResult<TransactionDto>>("/api/transactions?pageSize=50", Json);

        Assert.NotNull(import);
        Assert.NotNull(page);
        Assert.Equal(3, import.Sources.Count);
        Assert.NotEmpty(page.Items);
        Assert.True(page.TotalCount >= import.Inserted);
    }

    [Fact]
    public async Task Unknown_transaction_returns_404()
    {
        var response = await _client.GetAsync("/api/transactions/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_page_size_returns_400()
    {
        var response = await _client.GetAsync("/api/transactions?pageSize=5000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
