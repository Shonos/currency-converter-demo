using CurrencyConverterDemo.Domain.Enums;
using CurrencyConverterDemo.Domain.Interfaces;
using CurrencyConverterDemo.Infrastructure.Configuration;
using CurrencyConverterDemo.Infrastructure.Factories;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace CurrencyConverterDemo.Tests.Unit.Infrastructure;

public class CurrencyProviderFactoryTests
{
    [Fact]
    public void GetProvider_WithKnownType_ReturnsCorrectProvider()
    {
        // Arrange
        var provider1 = new Mock<ICurrencyProvider>();
        provider1.Setup(p => p.ProviderName).Returns("Frankfurter");

        var provider2 = new Mock<ICurrencyProvider>();
        provider2.Setup(p => p.ProviderName).Returns("TestProvider");

        var providers = new[] { provider1.Object, provider2.Object };
        var options = Options.Create(new CurrencyProviderSettings
        {
            DefaultProvider = CurrencyProviderType.Frankfurter
        });

        var factory = new CurrencyProviderFactory(providers, options);

        // Act
        var result = factory.GetProvider(CurrencyProviderType.Frankfurter);

        // Assert
        result.Should().NotBeNull();
        result.ProviderName.Should().Be("Frankfurter");
        result.Should().BeSameAs(provider1.Object);
    }

    [Fact]
    public void GetProvider_WithUnknownType_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = new Mock<ICurrencyProvider>();
        provider.Setup(p => p.ProviderName).Returns("Frankfurter");

        var providers = new[] { provider.Object };
        var options = Options.Create(new CurrencyProviderSettings
        {
            DefaultProvider = CurrencyProviderType.Frankfurter
        });

        var factory = new CurrencyProviderFactory(providers, options);

        // Act
        var act = () => factory.GetProvider((CurrencyProviderType)999);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not registered*");
    }

    [Fact]
    public void GetDefaultProvider_ReturnsConfiguredDefault()
    {
        // Arrange
        var provider = new Mock<ICurrencyProvider>();
        provider.Setup(p => p.ProviderName).Returns("Frankfurter");

        var providers = new[] { provider.Object };
        var options = Options.Create(new CurrencyProviderSettings
        {
            DefaultProvider = CurrencyProviderType.Frankfurter
        });

        var factory = new CurrencyProviderFactory(providers, options);

        // Act
        var result = factory.GetDefaultProvider();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(provider.Object);
    }

    [Fact]
    public void GetProvider_CaseInsensitiveMatch_ReturnsProvider()
    {
        // Arrange
        var provider = new Mock<ICurrencyProvider>();
        provider.Setup(p => p.ProviderName).Returns("frankfurter"); // lowercase

        var providers = new[] { provider.Object };
        var options = Options.Create(new CurrencyProviderSettings
        {
            DefaultProvider = CurrencyProviderType.Frankfurter
        });

        var factory = new CurrencyProviderFactory(providers, options);

        // Act
        var result = factory.GetProvider(CurrencyProviderType.Frankfurter);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(provider.Object);
    }
}
