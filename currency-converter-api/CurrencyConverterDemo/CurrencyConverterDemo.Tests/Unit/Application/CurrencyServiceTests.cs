using CurrencyConverterDemo.Application.DTOs;
using CurrencyConverterDemo.Application.Exceptions;
using CurrencyConverterDemo.Application.Services;
using CurrencyConverterDemo.Domain.Interfaces;
using CurrencyConverterDemo.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CurrencyConverterDemo.Tests.Unit.Application;

public class CurrencyServiceTests
{
    private readonly Mock<ICurrencyProviderFactory> _factoryMock;
    private readonly Mock<ICurrencyProvider> _providerMock;
    private readonly Mock<ILogger<CurrencyService>> _loggerMock;
    private readonly CurrencyService _sut;

    public CurrencyServiceTests()
    {
        _providerMock = new Mock<ICurrencyProvider>();
        _factoryMock = new Mock<ICurrencyProviderFactory>();
        _loggerMock = new Mock<ILogger<CurrencyService>>();
        
        _factoryMock.Setup(f => f.GetDefaultProvider()).Returns(_providerMock.Object);
        
        _sut = new CurrencyService(_factoryMock.Object, _loggerMock.Object);
    }

    #region GetLatestRatesAsync Tests

    [Fact]
    public async Task GetLatestRatesAsync_WithValidBaseCurrency_ReturnsRates()
    {
        // Arrange
        var baseCurrency = "USD";
        var expectedResult = new ExchangeRateResult
        {
            Base = baseCurrency,
            Date = new DateOnly(2024, 2, 6),
            Rates = new Dictionary<string, decimal>
            {
                ["EUR"] = 0.8475m,
                ["GBP"] = 0.7358m,
                ["JPY"] = 157.05m
            }
        };

        _providerMock
            .Setup(p => p.GetLatestRatesAsync(baseCurrency, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.GetLatestRatesAsync(baseCurrency);

        // Assert
        result.Should().NotBeNull();
        result.Base.Should().Be(baseCurrency);
        result.Date.Should().Be(expectedResult.Date);
        result.Rates.Should().HaveCount(3);
        result.Rates.Should().ContainKey("EUR");
        result.Rates.Should().ContainKey("GBP");
        result.Rates.Should().ContainKey("JPY");
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("GBP")]
    [InlineData("EUR")]
    [InlineData("JPY")]
    public async Task GetLatestRatesAsync_WithDifferentBaseCurrencies_ReturnsCorrectRates(string baseCurrency)
    {
        // Arrange
        var expectedResult = new ExchangeRateResult
        {
            Base = baseCurrency,
            Date = new DateOnly(2024, 2, 6),
            Rates = new Dictionary<string, decimal> { ["USD"] = 1.0m }
        };

        _providerMock
            .Setup(p => p.GetLatestRatesAsync(baseCurrency, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.GetLatestRatesAsync(baseCurrency);

        // Assert
        result.Base.Should().Be(baseCurrency);
        _providerMock.Verify(p => p.GetLatestRatesAsync(baseCurrency, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ConvertCurrencyAsync Tests

    [Fact]
    public async Task ConvertCurrencyAsync_WithValidCurrencies_ReturnsConversion()
    {
        // Arrange
        var request = new ConversionRequest
        {
            From = "USD",
            To = "EUR",
            Amount = 100m
        };

        var expectedResult = new ConversionResult
        {
            From = "USD",
            To = "EUR",
            Amount = 100m,
            ConvertedAmount = 84.75m,
            Rate = 0.8475m,
            Date = new DateOnly(2024, 2, 6)
        };

        _providerMock
            .Setup(p => p.ConvertCurrencyAsync("USD", "EUR", 100m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.ConvertCurrencyAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.From.Should().Be("USD");
        result.To.Should().Be("EUR");
        result.Amount.Should().Be(100m);
        result.ConvertedAmount.Should().Be(84.75m);
        result.Rate.Should().Be(0.8475m);
    }

    [Fact]
    public async Task ConvertCurrencyAsync_WithExcludedSourceCurrency_ThrowsCurrencyNotSupportedException()
    {
        // Arrange
        var request = new ConversionRequest
        {
            From = "TRY",
            To = "USD",
            Amount = 100m
        };

        // Act
        var act = () => _sut.ConvertCurrencyAsync(request);

        // Assert
        await act.Should().ThrowAsync<CurrencyNotSupportedException>()
            .WithMessage("*TRY*not supported*");
        
        _providerMock.Verify(
            p => p.ConvertCurrencyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task ConvertCurrencyAsync_WithExcludedTargetCurrency_ThrowsCurrencyNotSupportedException()
    {
        // Arrange
        var request = new ConversionRequest
        {
            From = "USD",
            To = "PLN",
            Amount = 100m
        };

        // Act
        var act = () => _sut.ConvertCurrencyAsync(request);

        // Assert
        await act.Should().ThrowAsync<CurrencyNotSupportedException>()
            .WithMessage("*PLN*not supported*");
    }

    [Theory]
    [InlineData("THB")]
    [InlineData("MXN")]
    public async Task ConvertCurrencyAsync_WithExcludedCurrenciesAnyPosition_ThrowsCurrencyNotSupportedException(string excludedCurrency)
    {
        // Arrange - Test as source
        var requestAsSource = new ConversionRequest
        {
            From = excludedCurrency,
            To = "USD",
            Amount = 100m
        };

        // Act & Assert - Source
        await Assert.ThrowsAsync<CurrencyNotSupportedException>(
            () => _sut.ConvertCurrencyAsync(requestAsSource));

        // Arrange - Test as target
        var requestAsTarget = new ConversionRequest
        {
            From = "USD",
            To = excludedCurrency,
            Amount = 100m
        };

        // Act & Assert - Target
        await Assert.ThrowsAsync<CurrencyNotSupportedException>(
            () => _sut.ConvertCurrencyAsync(requestAsTarget));
    }

    [Fact]
    public async Task ConvertCurrencyAsync_ConversionCalculationIsCorrect()
    {
        // Arrange
        var request = new ConversionRequest
        {
            From = "GBP",
            To = "JPY",
            Amount = 50m
        };

        var expectedResult = new ConversionResult
        {
            From = "GBP",
            To = "JPY",
            Amount = 50m,
            ConvertedAmount = 9262.5m,
            Rate = 185.25m,
            Date = new DateOnly(2024, 2, 6)
        };

        _providerMock
            .Setup(p => p.ConvertCurrencyAsync("GBP", "JPY", 50m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.ConvertCurrencyAsync(request);

        // Assert
        result.ConvertedAmount.Should().Be(expectedResult.ConvertedAmount);
        (result.ConvertedAmount / result.Amount).Should().BeApproximately(result.Rate, 0.01m);
    }

    #endregion

    #region GetHistoricalRatesAsync Tests

    [Fact]
    public async Task GetHistoricalRatesAsync_ReturnsPaginatedResults()
    {
        // Arrange
        var request = new HistoricalRatesRequest
        {
            Base = "EUR",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 1, 10),
            Page = 1,
            PageSize = 3
        };

        var historicalResult = new HistoricalRateResult
        {
            Base = "EUR",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 1, 10),
            Rates = new Dictionary<DateOnly, IReadOnlyDictionary<string, decimal>>
            {
                [new DateOnly(2024, 1, 1)] = new Dictionary<string, decimal> { ["USD"] = 1.10m },
                [new DateOnly(2024, 1, 2)] = new Dictionary<string, decimal> { ["USD"] = 1.11m },
                [new DateOnly(2024, 1, 3)] = new Dictionary<string, decimal> { ["USD"] = 1.12m },
                [new DateOnly(2024, 1, 4)] = new Dictionary<string, decimal> { ["USD"] = 1.13m },
                [new DateOnly(2024, 1, 5)] = new Dictionary<string, decimal> { ["USD"] = 1.14m },
                [new DateOnly(2024, 1, 8)] = new Dictionary<string, decimal> { ["USD"] = 1.15m },
                [new DateOnly(2024, 1, 9)] = new Dictionary<string, decimal> { ["USD"] = 1.16m },
                [new DateOnly(2024, 1, 10)] = new Dictionary<string, decimal> { ["USD"] = 1.17m }
            }
        };

        _providerMock
            .Setup(p => p.GetHistoricalRatesAsync("EUR", request.StartDate, request.EndDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(historicalResult);

        // Act
        var result = await _sut.GetHistoricalRatesAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Base.Should().Be("EUR");
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(3);
        result.TotalCount.Should().Be(8);
        result.TotalPages.Should().Be(3);
        result.Rates.Should().HaveCount(3);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_LastPage_HasFewerItems()
    {
        // Arrange
        var request = new HistoricalRatesRequest
        {
            Base = "EUR",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 1, 10),
            Page = 3,
            PageSize = 3
        };

        var historicalResult = new HistoricalRateResult
        {
            Base = "EUR",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 1, 10),
            Rates = new Dictionary<DateOnly, IReadOnlyDictionary<string, decimal>>
            {
                [new DateOnly(2024, 1, 1)] = new Dictionary<string, decimal> { ["USD"] = 1.10m },
                [new DateOnly(2024, 1, 2)] = new Dictionary<string, decimal> { ["USD"] = 1.11m },
                [new DateOnly(2024, 1, 3)] = new Dictionary<string, decimal> { ["USD"] = 1.12m },
                [new DateOnly(2024, 1, 4)] = new Dictionary<string, decimal> { ["USD"] = 1.13m },
                [new DateOnly(2024, 1, 5)] = new Dictionary<string, decimal> { ["USD"] = 1.14m },
                [new DateOnly(2024, 1, 8)] = new Dictionary<string, decimal> { ["USD"] = 1.15m },
                [new DateOnly(2024, 1, 9)] = new Dictionary<string, decimal> { ["USD"] = 1.16m },
                [new DateOnly(2024, 1, 10)] = new Dictionary<string, decimal> { ["USD"] = 1.17m }
            }
        };

        _providerMock
            .Setup(p => p.GetHistoricalRatesAsync("EUR", request.StartDate, request.EndDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(historicalResult);

        // Act
        var result = await _sut.GetHistoricalRatesAsync(request);

        // Assert
        result.Rates.Should().HaveCount(2); // Last page with remaining 2 items
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_PaginationMetadata_IsCorrect()
    {
        // Arrange
        var request = new HistoricalRatesRequest
        {
            Base = "USD",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 1, 20),
            Page = 2,
            PageSize = 5
        };

        var historicalResult = new HistoricalRateResult
        {
            Base = "USD",
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Rates = Enumerable.Range(1, 15)
                .Select(day => new DateOnly(2024, 1, day))
                .ToDictionary(
                    date => date,
                    date => (IReadOnlyDictionary<string, decimal>)new Dictionary<string, decimal> { ["EUR"] = 0.85m })
        };

        _providerMock
            .Setup(p => p.GetHistoricalRatesAsync("USD", request.StartDate, request.EndDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(historicalResult);

        // Act
        var result = await _sut.GetHistoricalRatesAsync(request);

        // Assert
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(3);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
    }

    #endregion

    #region GetCurrenciesAsync Tests

    [Fact]
    public async Task GetCurrenciesAsync_ReturnsAllCurrencies()
    {
        // Arrange
        var allCurrencies = new Dictionary<string, string>
        {
            ["USD"] = "United States Dollar",
            ["EUR"] = "Euro",
            ["GBP"] = "British Pound",
            ["JPY"] = "Japanese Yen"
        };

        _providerMock
            .Setup(p => p.GetCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allCurrencies);

        // Act
        var result = await _sut.GetCurrenciesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Currencies.Should().HaveCount(4);
        result.Currencies.Should().Contain(c => c.Code == "USD" && c.Name == "United States Dollar");
    }

    [Fact]
    public async Task GetCurrenciesAsync_ExcludesProhibitedCurrencies()
    {
        // Arrange
        var allCurrencies = new Dictionary<string, string>
        {
            ["USD"] = "United States Dollar",
            ["EUR"] = "Euro",
            ["TRY"] = "Turkish Lira",
            ["PLN"] = "Polish Zloty",
            ["THB"] = "Thai Baht",
            ["MXN"] = "Mexican Peso",
            ["GBP"] = "British Pound"
        };

        _providerMock
            .Setup(p => p.GetCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allCurrencies);

        // Act
        var result = await _sut.GetCurrenciesAsync();

        // Assert
        result.Currencies.Should().HaveCount(3); // Only USD, EUR, GBP
        result.Currencies.Should().NotContain(c => c.Code == "TRY");
        result.Currencies.Should().NotContain(c => c.Code == "PLN");
        result.Currencies.Should().NotContain(c => c.Code == "THB");
        result.Currencies.Should().NotContain(c => c.Code == "MXN");
    }

    [Fact]
    public async Task GetCurrenciesAsync_ReturnsOrderedByCode()
    {
        // Arrange
        var allCurrencies = new Dictionary<string, string>
        {
            ["USD"] = "United States Dollar",
            ["AUD"] = "Australian Dollar",
            ["EUR"] = "Euro",
            ["CAD"] = "Canadian Dollar"
        };

        _providerMock
            .Setup(p => p.GetCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allCurrencies);

        // Act
        var result = await _sut.GetCurrenciesAsync();

        // Assert
        result.Currencies.Should().BeInAscendingOrder(c => c.Code);
        result.Currencies.First().Code.Should().Be("AUD");
        result.Currencies.Last().Code.Should().Be("USD");
    }

    #endregion
}
