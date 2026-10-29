using TransactionsIngest.Application.Sources;
using TransactionsIngest.Domain.Constants;

namespace TransactionsIngest.Application.Import.Reconciliation;

public static class SourceTransactionValidator
{
    public static IReadOnlyList<string> Validate(SourceTransaction tx, string expectedSourceSystem)
    {
        var errors = new List<string>();

        if (tx.TransactionId <= 0)
            errors.Add("TransactionId must be a positive number.");

        if (!string.IsNullOrWhiteSpace(tx.SourceSystem) &&
            !string.Equals(tx.SourceSystem.Trim(), expectedSourceSystem, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"SourceSystem '{tx.SourceSystem}' does not match the source '{expectedSourceSystem}'.");
        }

        RequiredText(errors, nameof(tx.LocationCode), tx.LocationCode);
        RequiredText(errors, nameof(tx.ProductName), tx.ProductName);
        MaxLength(errors, nameof(tx.CardNumber), tx.CardNumber);

        if (Math.Abs(tx.Amount) > FieldLimits.MaxAbsoluteAmount)
            errors.Add("Amount is out of range for decimal(18,2).");

        if (tx.TransactionTime == default)
            errors.Add("TransactionTime is required.");

        return errors;
    }

    /// <summary>Trim, empty card -> null, round amount, force UTC. Call only after Validate passed.</summary>
    public static SourceTransaction Normalize(SourceTransaction tx, string sourceSystem) => tx with
    {
        SourceSystem = sourceSystem,
        CardNumber = string.IsNullOrWhiteSpace(tx.CardNumber) ? null : tx.CardNumber.Trim(),
        LocationCode = tx.LocationCode.Trim(),
        ProductName = tx.ProductName.Trim(),
        Amount = Math.Round(tx.Amount, 2, MidpointRounding.AwayFromZero),
        TransactionTime = ToUtc(tx.TransactionTime),
    };

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        // sources are documented as UTC, so unspecified == utc
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    private static void RequiredText(List<string> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{field} is required.");
            return;
        }
        MaxLength(errors, field, value);
    }

    private static void MaxLength(List<string> errors, string field, string? value)
    {
        if (value is not null && value.Trim().Length > FieldLimits.MaxStringLength)
            errors.Add($"{field} is longer than {FieldLimits.MaxStringLength} characters.");
    }
}
