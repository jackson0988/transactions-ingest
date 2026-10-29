namespace TransactionsIngest.Application.Sources;

/// <summary>A transaction exactly as the source system sent it (not validated yet).</summary>
public sealed record SourceTransaction(
    string SourceSystem,
    int TransactionId,
    string? CardNumber,
    string LocationCode,
    string ProductName,
    decimal Amount,
    DateTime TransactionTime);
