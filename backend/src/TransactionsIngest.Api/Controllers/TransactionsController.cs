using Microsoft.AspNetCore.Mvc;
using TransactionsIngest.Api.Contracts.Requests;
using TransactionsIngest.Application.Common.Models;
using TransactionsIngest.Application.Transactions;
using TransactionsIngest.Application.Transactions.Models;

namespace TransactionsIngest.Api.Controllers;

[ApiController]
[Route("api/transactions")]
[Produces("application/json")]
public sealed class TransactionsController(ITransactionQueryService queries) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<TransactionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<TransactionDto>>> Search(
        [FromQuery] TransactionSearchRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await queries.SearchAsync(request.ToCriteria(), cancellationToken));
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var transaction = await queries.GetByIdAsync(id, cancellationToken);
        return transaction is null ? NotFound() : Ok(transaction);
    }

    [HttpGet("{id:long}/audit")]
    [ProducesResponseType<IReadOnlyList<AuditEntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AuditEntryDto>>> GetAuditHistory(long id, CancellationToken cancellationToken)
    {
        var history = await queries.GetAuditHistoryAsync(id, cancellationToken);
        return history is null ? NotFound() : Ok(history);
    }
}
