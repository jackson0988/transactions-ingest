namespace TransactionsIngest.Application.Import.Reconciliation;

public sealed record RejectedTransaction(int TransactionId, string Reason);
