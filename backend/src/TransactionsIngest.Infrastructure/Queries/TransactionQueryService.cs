using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TransactionsIngest.Application.Common.Models;
using TransactionsIngest.Application.Transactions;
using TransactionsIngest.Application.Transactions.Models;
using TransactionsIngest.Domain.Entities;
using TransactionsIngest.Infrastructure.Persistence;

namespace TransactionsIngest.Infrastructure.Queries;

public sealed class TransactionQueryService(TransactionsDbContext db) : ITransactionQueryService
{
    private static readonly Expression<Func<TransactionRecord, TransactionDto>> ToDto = t => new TransactionDto(
        t.Id, t.SourceSystem, t.TransactionId, t.CardNumber, t.LocationCode, t.ProductName,
        t.Amount, t.TransactionTimeUtc, t.Status, t.RevisionNumber, t.CreatedAtUtc, t.UpdatedAtUtc);

    public async Task<PagedResult<TransactionDto>> SearchAsync(TransactionSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = db.Transactions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(criteria.SourceSystem))
            query = query.Where(t => t.SourceSystem == criteria.SourceSystem);

        if (criteria.Status is { } status)
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var term = criteria.Search.Trim();
            var pattern = $"%{term}%";
            var isId = int.TryParse(term, out var transactionId);

            query = query.Where(t =>
                (isId && t.TransactionId == transactionId) ||
                EF.Functions.Like(t.ProductName, pattern) ||
                EF.Functions.Like(t.LocationCode, pattern) ||
                (t.CardNumber != null && EF.Functions.Like(t.CardNumber, pattern)));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await ApplySort(query, criteria.SortBy, criteria.Descending)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<TransactionDto>(items, criteria.Page, criteria.PageSize, total);
    }

    public Task<TransactionDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        db.Transactions.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AuditEntryDto>?> GetAuditHistoryAsync(long id, CancellationToken cancellationToken = default)
    {
        if (!await db.Transactions.AnyAsync(t => t.Id == id, cancellationToken))
            return null;

        return await db.AuditEntries.AsNoTracking()
            .Where(a => a.TransactionRecordId == id)
            .OrderByDescending(a => a.ChangedAtUtc)
            .ThenByDescending(a => a.Id)
            .Select(a => new AuditEntryDto(
                a.Id, a.TransactionRecordId, a.ImportBatchId, a.ChangeType,
                a.FieldName, a.OldValue, a.NewValue, a.ChangedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<TransactionRecord> ApplySort(IQueryable<TransactionRecord> query, string? sortBy, bool desc)
    {
        IOrderedQueryable<TransactionRecord> ordered = sortBy?.ToLowerInvariant() switch
        {
            "sourcesystem" => desc ? query.OrderByDescending(t => t.SourceSystem) : query.OrderBy(t => t.SourceSystem),
            "transactionid" => desc ? query.OrderByDescending(t => t.TransactionId) : query.OrderBy(t => t.TransactionId),
            "locationcode" => desc ? query.OrderByDescending(t => t.LocationCode) : query.OrderBy(t => t.LocationCode),
            "productname" => desc ? query.OrderByDescending(t => t.ProductName) : query.OrderBy(t => t.ProductName),
            "amount" => desc ? query.OrderByDescending(t => t.Amount) : query.OrderBy(t => t.Amount),
            "status" => desc ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
            _ => desc ? query.OrderByDescending(t => t.TransactionTimeUtc) : query.OrderBy(t => t.TransactionTimeUtc),
        };

        // tie-breaker so paging is stable
        return ordered.ThenBy(t => t.Id);
    }
}
