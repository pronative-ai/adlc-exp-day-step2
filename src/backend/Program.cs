using System.ComponentModel.DataAnnotations;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using OuterloopLabApi.Configuration;
using OuterloopLabApi.Endpoints;
using OuterloopLabApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

builder.Services
    .AddOptions<CurrencyApiOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        options.BaseUrl = NormalizeCurrencyApiBaseUrl(configuration["CURRENCY_API_BASE_URL"]);
    })
    .Validate(static options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "CURRENCY_API_BASE_URL must be an absolute URL.")
    .ValidateOnStart();

builder.Services
    .AddOptions<CosmosOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        options.AccountUri = GetRequiredEnvironmentValue(configuration, "COSMOS_DB_URI");
        options.DatabaseName = GetRequiredEnvironmentValue(configuration, "COSMOS_DB_DATABASE");
        options.ContainerName = GetRequiredEnvironmentValue(configuration, "COSMOS_DB_CONTAINER");
        options.AccountName = GetRequiredEnvironmentValue(configuration, "COSMOS_DB_ACCOUNT_NAME");
        options.ResourceGroup = GetRequiredEnvironmentValue(configuration, "COSMOS_DB_RESOURCE_GROUP");
        options.Region = GetRequiredEnvironmentValue(configuration, "COSMOS_DB_REGION");
        options.ManagedIdentityClientId = GetRequiredEnvironmentValue(configuration, "AZURE_MANAGED_IDENTITY_CLIENT_ID");
    })
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<TokenCredential>(static serviceProvider =>
{
    CosmosOptions options = serviceProvider.GetRequiredService<IOptions<CosmosOptions>>().Value;

    return new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        ManagedIdentityClientId = options.ManagedIdentityClientId,
    });
});

builder.Services.AddSingleton(static serviceProvider =>
{
    CosmosOptions options = serviceProvider.GetRequiredService<IOptions<CosmosOptions>>().Value;
    TokenCredential credential = serviceProvider.GetRequiredService<TokenCredential>();

    return new CosmosClient(
        options.AccountUri,
        credential,
        new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
            },
        });
});

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<ICosmosStartupInitializer, CosmosStartupInitializer>();
builder.Services.AddSingleton<ICurrencyConversionAuditRepository, CurrencyConversionAuditRepository>();
builder.Services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();

builder.Services
    .AddHttpClient<IExternalRateProviderClient, ExternalRateProviderClient>((serviceProvider, httpClient) =>
    {
        CurrencyApiOptions options = serviceProvider.GetRequiredService<IOptions<CurrencyApiOptions>>().Value;
        httpClient.BaseAddress = new Uri(options.BaseUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(15);
    });

var app = builder.Build();

app.MapCurrencyConversionEndpoints();

using (IServiceScope scope = app.Services.CreateScope())
{
    ICosmosStartupInitializer startupInitializer = scope.ServiceProvider.GetRequiredService<ICosmosStartupInitializer>();
    await startupInitializer.InitializeAsync(app.Lifetime.ApplicationStopping);
}

app.Run();

return;

static string GetRequiredEnvironmentValue(IConfiguration configuration, string key)
{
    string? value = configuration[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new ValidationException($"Required environment variable '{key}' was not provided.");
    }

    return value;
}

static string NormalizeCurrencyApiBaseUrl(string? configuredValue)
{
    if (string.IsNullOrWhiteSpace(configuredValue))
    {
        return "https://frankfurter.dev";
    }

    return configuredValue.TrimEnd('/');
}

public partial class Program;
