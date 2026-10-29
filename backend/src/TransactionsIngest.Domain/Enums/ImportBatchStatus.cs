namespace TransactionsIngest.Domain.Enums;

public enum ImportBatchStatus
{
    Running = 0,
    Succeeded = 1,
    CompletedWithErrors = 2,
    Failed = 3,
}
