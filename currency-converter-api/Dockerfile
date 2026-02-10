# currency-converter-api/Dockerfile
# Stage 1: Base - SDK for building and testing
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /src

# Stage 2: Restore - Restore dependencies separately for better caching
FROM base AS restore
COPY CurrencyConverterDemo/*.slnx ./
COPY CurrencyConverterDemo/CurrencyConverterDemo.Api/*.csproj CurrencyConverterDemo.Api/
COPY CurrencyConverterDemo/CurrencyConverterDemo.Application/*.csproj CurrencyConverterDemo.Application/
COPY CurrencyConverterDemo/CurrencyConverterDemo.Domain/*.csproj CurrencyConverterDemo.Domain/
COPY CurrencyConverterDemo/CurrencyConverterDemo.Infrastructure/*.csproj CurrencyConverterDemo.Infrastructure/
COPY CurrencyConverterDemo/CurrencyConverterDemo.Tests/*.csproj CurrencyConverterDemo.Tests/

RUN dotnet restore

# Stage 3: Build - Compile the application
FROM restore AS build
COPY CurrencyConverterDemo/ ./
RUN dotnet build CurrencyConverterDemo.Api -c Release --no-restore

# Stage 4: Test - Run tests (optional target)
FROM build AS test
RUN dotnet test CurrencyConverterDemo.Tests \
    -c Release \
    --no-build \
    --verbosity normal \
    --logger "trx;LogFileName=test-results.trx" \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=cobertura \
    /p:Threshold=80

# Stage 5: Publish - Create publish output
FROM build AS publish
RUN dotnet publish CurrencyConverterDemo.Api \
    -c Release \
    -o /app/publish \
    --no-build \
    --no-restore

# Stage 6: Runtime - Final minimal production image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory with proper permissions
RUN mkdir -p /var/log/currency-converter && \
    chown -R appuser:appuser /var/log/currency-converter

# Switch to non-root user
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CurrencyConverterDemo.Api.dll"]
