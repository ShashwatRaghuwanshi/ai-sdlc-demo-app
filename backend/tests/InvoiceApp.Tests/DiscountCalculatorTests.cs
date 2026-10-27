using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class DiscountCalculatorTests
{
    private readonly DiscountCalculator _calculator = new();

    [Theory]
    [InlineData(0, 0.00)]
    [InlineData(9, 0.00)]
    [InlineData(10, 0.05)]
    [InlineData(49, 0.05)]
    [InlineData(50, 0.10)]
    [InlineData(99, 0.10)]
    [InlineData(100, 0.15)]
    public void QuantityDiscount_ReturnsExpectedRate(
        int quantity, decimal expected)
    {
        Assert.Equal(expected, _calculator.QuantityDiscount(quantity));
    }

    [Theory]
    [InlineData(0, 0.00)]
    [InlineData(5, 0.05)]
    [InlineData(10, 0.10)]
    [InlineData(15, 0.10)]
    public void LoyaltyDiscount_ReturnsExpectedRate(
        int years, decimal expected)
    {
        Assert.Equal(expected, _calculator.LoyaltyDiscount(years));
    }

    [Fact]
    public void CombinedDiscount_Nonprofit_AddsFivePercent()
    {
        Assert.Equal(
            0.25m,
            _calculator.CombinedDiscount(100, 5, "Nonprofit"));
    }

    [Fact]
    public void CombinedDiscount_OtherCustomer_DoesNotAddNonprofitDiscount()
    {
        Assert.Equal(
            0.20m,
            _calculator.CombinedDiscount(100, 5, "Standard"));
    }

    [Fact]
    public void ApplyDiscount_RoundsToTwoDecimalPlaces()
    {
        Assert.Equal(85.00m, _calculator.ApplyDiscount(100m, 0.15m));
        Assert.Equal(10.00m, _calculator.ApplyDiscount(10.005m, 0m));
    }
}


