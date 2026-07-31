using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using OuterloopLabApi.Cosmos;
using OuterloopLabApi.Controllers;
using OuterloopLabApi.Models;
using OuterloopLabApi.Repositories;
using OuterloopLabApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Strictly read configuration from runtime environment variables.
Func<string, string?> env = Environment.GetEnvironmentVariable;

string? cosmosUri = env("COSMOS_DB_URI");
string? cosmosDb = env("COSMOS_DB_DATABASE");
string? cosmosContainer = env("COSMOS_DB_CONTAINER");
string? miClientId = env("AZURE_MANAGED_IDENTITY_CLIENT_ID");

var hasCosmosConfig = !string.IsNullOrWhiteSpace(cosmosUri)
                     && !string.IsNullOrWhiteSpace(cosmosDb)
                     && !string.IsNullOrWhiteSpace(cosmosContainer)
                     && !string.IsNullOrWhiteSpace(miClientId);

builder.Services.AddSingleton(sp =>
{
  var settings = new CosmosSettings(
    CosmosUri: cosmosUri!,
    DatabaseName: cosmosDb!,
    ContainerName: cosmosContainer!,
    AccountName: env("COSMOS_DB_ACCOUNT_NAME")!,
    ResourceGroupName: env("COSMOS_DB_RESOURCE_GROUP")!,
    Region: env("COSMOS_DB_REGION")!,
    ManagedIdentityClientId: miClientId!);
  return settings;
});

builder.Services.AddHttpClient<ICurrencyRateClient, CurrencyRateClient>((client) =>
{
  var baseUrl = env("CURRENCY_API_BASE_URL") ?? "https://frankfurter.dev";
  if (!baseUrl.EndsWith('/')) baseUrl += '/';
  client.BaseAddress = new Uri(baseUrl);
  client.Timeout = TimeSpan.FromSeconds(5);
});

if (hasCosmosConfig)
{
  builder.Services.AddSingleton<IAuditTrailRepository>(sp =>
  {
    var settings = sp.GetRequiredService<CosmosSettings>();
    var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
      ManagedIdentityClientId = settings.ManagedIdentityClientId
    });
    return new CosmosAuditTrailRepository(settings, credential);
  });
}
else
{
  // CI/tests fallback: no Cosmos configuration present.
  builder.Services.AddSingleton<IAuditTrailRepository, InMemoryAuditTrailRepository>();
}

builder.Services.AddScoped<CurrencyConversionService>();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
  // Ensure [ApiController] doesn't override our explicit ProblemDetails responses.
  options.SuppressModelStateInvalidFilter = true;
});

var app = builder.Build();

// Startup provisioning must happen before the web app runs.
if (hasCosmosConfig)
{
  using var scope = app.Services.CreateScope();
  var settings = scope.ServiceProvider.GetRequiredService<CosmosSettings>();
  var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
  {
    ManagedIdentityClientId = settings.ManagedIdentityClientId
  });

  // ARM provisioning best-effort; data-plane create-if-not-exists is required.
  await CosmosProvisioning.TryProvisionWithArmBestEffortAsync(settings, credential);
  await CosmosProvisioning.EnsureDatabaseAndContainerExistAsync(settings, credential);
}

app.MapControllers();

app.Run();

public partial class Program { }
