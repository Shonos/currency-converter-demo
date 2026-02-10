using CurrencyConverterDemo.Domain.Constants;
using FluentAssertions;

namespace CurrencyConverterDemo.Tests.Unit.Domain;

public class ExcludedCurrenciesTests
{
    [Theory]
    [InlineData("TRY")]
    [InlineData("PLN")]
    [InlineData("THB")]
    [InlineData("MXN")]
    public void IsExcluded_ExcludedCurrency_ReturnsTrue(string code)
    {
        // Act
        var result = ExcludedCurrencies.IsExcluded(code);

        // Assert
        result.Should().BeTrue($"{code} should be excluded");
    }

    [Theory]
    [InlineData("try")]  // lowercase
    [InlineData("Pln")]  // mixed case
    [InlineData("thb")]  // lowercase
    [InlineData("mXn")]  // mixed case
    public void IsExcluded_ExcludedCurrencyCaseInsensitive_ReturnsTrue(string code)
    {
        // Act
        var result = ExcludedCurrencies.IsExcluded(code);

        // Assert
        result.Should().BeTrue($"{code} should be excluded (case-insensitive)");
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("AUD")]
    [InlineData("CAD")]
    [InlineData("CHF")]
    public void IsExcluded_AllowedCurrency_ReturnsFalse(string code)
    {
        // Act
        var result = ExcludedCurrencies.IsExcluded(code);

        // Assert
        result.Should().BeFalse($"{code} should NOT be excluded");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("INVALID")]
    [InlineData("XYZ")]
    public void IsExcluded_InvalidOrUnknownCurrency_ReturnsFalse(string code)
    {
        // Act
        var result = ExcludedCurrencies.IsExcluded(code);

        // Assert
        result.Should().BeFalse($"{code} is not a known excluded currency");
    }

    [Fact]
    public void Codes_ContainsExactlyFourCurrencies()
    {
        // Assert
        ExcludedCurrencies.Codes.Should().HaveCount(4);
        ExcludedCurrencies.Codes.Should().BeEquivalentTo(new[] { "TRY", "PLN", "THB", "MXN" });
    }

    [Fact]
    public void Codes_IsCaseInsensitive()
    {
        // Assert
        ExcludedCurrencies.Codes.Should().Contain("TRY");
        ExcludedCurrencies.Codes.Should().Contain("try");
        ExcludedCurrencies.Codes.Should().Contain("Try");
    }
}
