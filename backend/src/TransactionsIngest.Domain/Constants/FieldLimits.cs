namespace TransactionsIngest.Domain.Constants;

public static class FieldLimits
{
    // spec says "length < 20" for all the string columns
    public const int MaxStringLength = 19;

    // decimal(18,2) -> 16 digits before the point
    public const decimal MaxAbsoluteAmount = 9_999_999_999_999_999.99m;

    public const int MaxAuditValueLength = 256;

    public const int MaxImportErrorLength = 2000;
}
