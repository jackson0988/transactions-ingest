using TransactionsIngest.Application.Import.Reconciliation;
using static TransactionsIngest.UnitTests.TestData.TransactionTestData;

namespace TransactionsIngest.UnitTests.Import.Reconciliation;

public class SourceTransactionValidatorTests
{
    [Fact]
    public void Valid_transaction_has_no_errors()
    {
        Assert.Empty(SourceTransactionValidator.Validate(Tx(1), "Store Uptown"));
    }

    [Fact]
    public void Row_from_another_source_is_invalid()
    {
        var errors = SourceTransactionValidator.Validate(Tx(1, source: "Online"), "Store Uptown");

        Assert.Single(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Non_positive_transaction_id_is_invalid(int id)
    {
        Assert.NotEmpty(SourceTransactionValidator.Validate(Tx(id), "Store Uptown"));
    }

    [Fact]
    public void Text_longer_than_19_chars_is_invalid()
    {
        var errors = SourceTransactionValidator.Validate(Tx(1, product: "USB-C Docking Station"), "Store Uptown");

        Assert.Contains(errors, e => e.Contains("ProductName"));
    }

    [Fact]
    public void Normalize_trims_text_and_turns_empty_card_into_null()
    {
        var normalized = SourceTransactionValidator.Normalize(Tx(1, card: "  ", product: " Mouse "), "Store Uptown");

        Assert.Null(normalized.CardNumber);
        Assert.Equal("Mouse", normalized.ProductName);
    }

    [Fact]
    public void Normalize_treats_unspecified_time_as_utc()
    {
        var local = Tx(1) with { TransactionTime = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Unspecified) };

        var normalized = SourceTransactionValidator.Normalize(local, "Store Uptown");

        Assert.Equal(DateTimeKind.Utc, normalized.TransactionTime.Kind);
        Assert.Equal(10, normalized.TransactionTime.Hour);
    }
}
