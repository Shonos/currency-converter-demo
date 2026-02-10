using CurrencyConverterDemo.Application.Validators;
using FluentAssertions;

namespace CurrencyConverterDemo.Tests.Unit.Application;

public class CurrencyValidatorTests
{
    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("AUD")]
    public void IsValid_WithValidCurrency_ReturnsTrue(string currencyCode)
    {
        // Act
        var result = CurrencyValidator.IsValid(currencyCode);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("TRY")]
    [InlineData("PLN")]
    [InlineData("THB")]
    [InlineData("MXN")]
    public void IsValid_WithExcludedCurrency_ReturnsFalse(string currencyCode)
    {
        // Act
        var result = CurrencyValidator.IsValid(currencyCode);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsValid_WithNullOrWhitespace_ReturnsFalse(string? currencyCode)
    {
        // Act
        var result = CurrencyValidator.IsValid(currencyCode!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AreValid_WithAllValidCurrencies_ReturnsTrue()
    {
        // Act
        var result = CurrencyValidator.AreValid("USD", "EUR", "GBP");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AreValid_WithOneExcludedCurrency_ReturnsFalse()
    {
        // Act
        var result = CurrencyValidator.AreValid("USD", "TRY", "GBP");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AreValid_WithAllExcludedCurrencies_ReturnsFalse()
    {
        // Act
        var result = CurrencyValidator.AreValid("TRY", "PLN", "THB");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetFirstInvalid_WithAllValid_ReturnsNull()
    {
        // Act
        var result = CurrencyValidator.GetFirstInvalid("USD", "EUR", "GBP");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetFirstInvalid_WithOneExcluded_ReturnsThatCurrency()
    {
        // Act
        var result = CurrencyValidator.GetFirstInvalid("USD", "TRY", "EUR");

        // Assert
        result.Should().Be("TRY");
    }

    [Fact]
    public void GetFirstInvalid_WithMultipleExcluded_ReturnsFirstOne()
    {
        // Act
        var result = CurrencyValidator.GetFirstInvalid("USD", "TRY", "PLN", "EUR");

        // Assert
        result.Should().Be("TRY");
    }

    [Fact]
    public void GetFirstInvalid_WithEmptyString_ReturnsThatEmpty()
    {
        // Act
        var result = CurrencyValidator.GetFirstInvalid("USD", "", "EUR");

        // Assert
        result.Should().Be("");
    }

    [Theory]
    [InlineData("try")]
    [InlineData("Pln")]
    [InlineData("thb")]
    [InlineData("MxN")]
    public void IsValid_WithExcludedCurrencyCaseInsensitive_ReturnsFalse(string currencyCode)
    {
        // Act
        var result = CurrencyValidator.IsValid(currencyCode);

        // Assert
        result.Should().BeFalse();
    }
}
