using System.Net;
using System.Text.Json.Serialization;
using OuterloopLabApi.Endpoints;
using OuterloopLabApi.Infrastructure;
using OuterloopLabApi.Options;
using OuterloopLabApi.Repositories;
using OuterloopLabApi.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ProviderOptions>(o =>
{
  o.ProviderBaseUrl = Environment.GetEnvironmentVariable("EXCHANGE_RATE_PROVIDER_BASE_URL")
                       ?? "https://api.exchangerate.host/convert";
});

builder.Services.Configure<CosmosOptions>(o =>
{
  o.Uri = Environment.GetEnvironmentVariable("COSMOS_DB_URI")
          ?? throw new InvalidOperationException("COSMOS_DB_URI env var is required.");
  o.DatabaseName = Environment.GetEnvironmentVariable("COSMOS_DB_DATABASE")
                    ?? "currency-conversion-db";
  o.ContainerName = Environment.GetEnvironmentVariable("COSMOS_DB_CONTAINER")
                     ?? "currencyconversion";
  o.ManagedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_MANAGED_IDENTITY_CLIENT_ID")
                                ?? throw new InvalidOperationException("AZURE_MANAGED_IDENTITY_CLIENT_ID env var is required.");
  o.ResourceGroup = Environment.GetEnvironmentVariable("COSMOS_DB_RESOURCE_GROUP") ?? string.Empty;
  o.AccountName = Environment.GetEnvironmentVariable("COSMOS_DB_ACCOUNT_NAME") ?? string.Empty;
  o.Region = Environment.GetEnvironmentVariable("COSMOS_DB_REGION") ?? string.Empty;
});

builder.Services.AddHttpClient<IExchangeRateClient, ExchangeRateClient>();

builder.Services.AddSingleton<IConversionAuditRepository, CosmosConversionAuditRepository>();
builder.Services.AddSingleton<ConversionQuoteService>();
builder.Services.AddSingleton<CosmosProvisioner>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
  options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.UseExceptionHandler(_ => Results.Problem(
  title: "Internal Server Error",
  detail: "An unexpected error occurred.",
  statusCode: (int)HttpStatusCode.InternalServerError));

// Ensure Cosmos resources exist.
// This is best-effort so local builds/tests don't require Azure access.
app.Lifetime.ApplicationStarted.Register(() =>
{
  _ = app.Services.GetRequiredService<CosmosProvisioner>().EnsureResourcesAsync(CancellationToken.None);
});

ConversionsEndpoints.Map(app);

app.Run();
