using CurrencyConverterDemo.Tests.Fixtures;

namespace CurrencyConverterDemo.Tests.Integration;

/// <summary>
/// Defines a collection for integration tests that share the same CustomWebApplicationFactory instance.
/// Tests in this collection will not run in parallel with each other to avoid conflicts.
/// </summary>
[CollectionDefinition("Integration Tests", DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
