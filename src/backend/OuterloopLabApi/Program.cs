using Azure.Identity;
using Microsoft.Azure.Cosmos;
using OuterloopLabApi.Configuration;
using OuterloopLabApi.Json;
using OuterloopLabApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Backend reads all configuration exclusively from runtime environment variables,
// using the exact keys defined in docs\CONTAINER_ENVIRONMENT_VARIABLES.md.
builder.Configuration.Sources.Clear();
builder.Configuration.AddEnvironmentVariables();

var cosmosOptions = new CosmosDbOptions
{
    Uri = Require(builder.Configuration, "COSMOS_DB_URI"),
    Database = builder.Configuration["COSMOS_DB_DATABASE"] ?? "currency-conversion-db",
    Container = builder.Configuration["COSMOS_DB_CONTAINER"] ?? "currencyconversion",
    AccountName = Require(builder.Configuration, "COSMOS_DB_ACCOUNT_NAME"),
    ResourceGroup = Require(builder.Configuration, "COSMOS_DB_RESOURCE_GROUP"),
    Region = builder.Configuration["COSMOS_DB_REGION"] ?? "Central India",
    ManagedIdentityClientId = Require(builder.Configuration, "AZURE_MANAGED_IDENTITY_CLIENT_ID"),
};

var currencyApiBaseUrl = builder.Configuration["CURRENCY_API_BASE_URL"] ?? "https://frankfurter.dev";

var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ManagedIdentityClientId = cosmosOptions.ManagedIdentityClientId,
});

builder.Services.AddSingleton(cosmosOptions);
builder.Services.AddSingleton(credential);
builder.Services.AddSingleton(new CosmosClient(cosmosOptions.Uri, credential, new CosmosClientOptions
{
    ConnectionMode = ConnectionMode.Direct,
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
        Indented = false,
    },
}));
builder.Services.AddSingleton<IRateFallbackStore, InMemoryRateFallbackStore>();
builder.Services.AddSingleton<IAuditRepository, CosmosAuditRepository>();
builder.Services.AddSingleton<CurrencyConversionService>();
builder.Services.AddHttpClient<ICurrencyRateProvider, FrankfurterRateProvider>(client =>
{
    client.BaseAddress = new Uri(currencyApiBaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeOffsetConverter());
});

var app = builder.Build();

try
{
    await CosmosDbInitializer.EnsureReadyAsync(cosmosOptions, credential, app.Logger, CancellationToken.None);

    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Startup failed; the application will exit.");
    Environment.ExitCode = 1;
}

static string Require(IConfiguration configuration, string key)
{
    var value = configuration[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException(
            $"Required environment variable '{key}' is not set. See docs\\CONTAINER_ENVIRONMENT_VARIABLES.md.");
    }

    return value.Trim();
}
