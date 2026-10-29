using Microsoft.AspNetCore.Mvc;
using TransactionsIngest.Application.Import;
using TransactionsIngest.Application.Import.Models;

namespace TransactionsIngest.Api.Controllers;

[ApiController]
[Route("api/import")]
[Produces("application/json")]
public sealed class ImportController(ITransactionImportService importService, IImportBatchQueryService batches) : ControllerBase
{
    /// <summary>Runs an import of all configured source systems right now.</summary>
    [HttpPost]
    [ProducesResponseType<ImportResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ImportResult>> Run()
    {
        // not passing RequestAborted on purpose - closing the browser tab shouldn't stop an import half way
        var result = await importService.RunAsync("manual", CancellationToken.None);
        return Ok(result);
    }

    [HttpGet("batches")]
    [ProducesResponseType<IReadOnlyList<ImportBatchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ImportBatchDto>>> GetRecentBatches(
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        return Ok(await batches.GetRecentAsync(take, cancellationToken));
    }
}
