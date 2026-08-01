using System.Globalization;
using System.Net;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using OuterloopLabApi.Conversion;
using OuterloopLabApi.Cosmos;
using OuterloopLabApi.External;

var builder = WebApplication.CreateBuilder(args);

// Constraint: read configurations exclusively from runtime environment variables.
builder.Configuration.Sources.Clear();
builder.Configuration.AddEnvironmentVariables();

var cosmosSettings = CosmosSettings.FromEnvironment(builder.Configuration);

var providerBaseUrl = builder.Configuration["CURRENCY_API_BASE_URL"];
if (string.IsNullOrWhiteSpace(providerBaseUrl))
{
    providerBaseUrl = "https://frankfurter.dev";
}

builder.Services.AddHttpClient<ICurrencyConversionProvider, FrankfurterCurrencyConversionProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ManagedIdentityClientId = cosmosSettings.AzureManagedIdentityClientId
});

var cosmosClient = new CosmosClient(
    cosmosSettings.CosmosDbUri,
    credential,
    new CosmosClientOptions { ConnectionMode = ConnectionMode.Direct });

// Required: ARM best-effort attempt inside startup lifecycle, then token-authenticated data-plane create-if-not-exists.
await CosmosProvisioner.TryProvisionWithArmBestEffortAsync(cosmosClient, cosmosSettings, credential);

var container = await CosmosProvisioner.CreateDatabaseAndContainerIfNotExistsAsync(cosmosClient, cosmosSettings);

builder.Services.AddSingleton<IConversionAuditRepository>(_ => new CosmosConversionAuditRepository(container));
builder.Services.AddSingleton(new ConversionProviderOptions { BaseUrl = providerBaseUrl });

var app = builder.Build();

app.MapPost("/api/conversions", async (ConversionRequest request, IConversionAuditRepository repo, ICurrencyConversionProvider provider) =>
{
    var validation = ConversionRequestValidator.Validate(request);
    if (validation is not null)
    {
        return Results.Problem(
            title: "Invalid conversion request",
            detail: validation,
            statusCode: (int)HttpStatusCode.BadRequest,
            type: "https://httpstatuses.com/400");
    }

    ProviderConversionResult providerResult;
    try
    {
        providerResult = await provider.GetLatestRateAsync(request.SourceCurrency, request.TargetCurrency);
    }
    catch (CurrencyProviderUnavailableException)
    {
        return Results.Problem(
            title: "Currency conversion provider is unavailable",
            detail: "Conversion provider is temporarily unavailable.",
            statusCode: (int)HttpStatusCode.ServiceUnavailable,
            type: "https://httpstatuses.com/503");
    }
    catch (Exception)
    {
        // Constraint: never bubble raw serialization/network exceptions.
        return Results.Problem(
            title: "Currency conversion provider is unavailable",
            detail: "Conversion provider is temporarily unavailable.",
            statusCode: (int)HttpStatusCode.ServiceUnavailable,
            type: "https://httpstatuses.com/503");
    }

    var backendExecutionTimestampUtc = DateTimeOffset.UtcNow;
    var auditId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

    var document = ConversionAuditDocument.Create(
        auditId: auditId,
        sourceCurrency: request.SourceCurrency,
        targetCurrency: request.TargetCurrency,
        originalAmount: request.Amount,
        appliedRate: providerResult.Rate,
        convertedAmount: request.Amount * providerResult.Rate,
        providerBaseCurrency: providerResult.ProviderBaseCurrency,
        providerDate: providerResult.ProviderDate,
        providerSequence: providerResult.ProviderSequence,
        backendExecutionTimestampUtc: backendExecutionTimestampUtc);

    await repo.CreateAsync(document);

    return Results.Ok(document.ToResponse());
});

app.MapGet("/api/conversions/{id}", async (string id, IConversionAuditRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(id))
    {
        return Results.Problem(
            title: "Invalid conversion id",
            detail: "Audit id is required.",
            statusCode: (int)HttpStatusCode.BadRequest,
            type: "https://httpstatuses.com/400");
    }

    var doc = await repo.GetByIdAsync(id.Trim());
    if (doc is null)
    {
        return Results.Problem(
            title: "Conversion not found",
            detail: "No audit record exists for the provided id.",
            statusCode: (int)HttpStatusCode.NotFound,
            type: "https://httpstatuses.com/404");
    }

    return Results.Ok(doc.ToResponse());
});

app.Run();
