using Azure.Core;
using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Known Constraint: runtime env vars only (no local settings fallbacks).
builder.Configuration.Sources.Clear();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddProblemDetails();

builder.Services.AddSingleton<CurrencyConversionService>();
builder.Services.AddSingleton<ExternalCurrencyRateProviderAdapter>();

builder.Services.AddHttpClient<CurrencyRateProviderClient>((sp, client) =>
{
    var currencyBaseUrl = Environment.GetEnvironmentVariable("CURRENCY_API_BASE_URL")
                           ?? "https://frankfurter.dev";
    client.BaseAddress = new Uri(currencyBaseUrl.TrimEnd('/'));
});

// Cosmos + ARM provisioning must happen before the app starts.
var cosmosOptions = CosmosOptions.FromEnvironment();
var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ManagedIdentityClientId = cosmosOptions.ManagedIdentityClientId,
});

var cosmosClient = new CosmosClient(cosmosOptions.CosmosDbUri, credential);

// Best-effort ARM provisioning.
await CosmosArmProvisioner.TryProvisionAsync(
    credential,
    cosmosOptions,
    appDatabaseName: cosmosOptions.DatabaseName,
    appContainerName: cosmosOptions.ContainerName);

// Data-plane create-if-not-exists must run with token auth and must fail startup if it fails.
await CosmosDataPlaneProvisioner.EnsureDatabaseAndContainerAsync(
    cosmosClient,
    cosmosOptions.DatabaseName,
    cosmosOptions.ContainerName);

builder.Services.AddSingleton(cosmosClient);
builder.Services.AddSingleton<IConversionAuditRepository>(_ =>
    new CosmosConversionAuditRepository(
        cosmosClient,
        cosmosOptions.DatabaseName,
        cosmosOptions.ContainerName,
        cosmosOptions.PartitionKeyValue));

builder.Services.AddSingleton<ConversionAuditApi>(sp =>
    new ConversionAuditApi(
        sp.GetRequiredService<CurrencyConversionService>(),
        sp.GetRequiredService<IConversionAuditRepository>(),
        sp.GetRequiredService<CurrencyRateProviderClient>()));

var app = builder.Build();

app.MapPost(
    "/api/conversions",
    async (CreateConversionRequest request, ConversionAuditApi api, HttpContext http) =>
    {
        if (!request.IsValid(out var validationProblem))
        {
            return Results.Problem(
                detail: validationProblem,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid conversion request",
                instance: http.Request.Path);
        }

        return await api.CreateConversionAsync(request);
    })
    .Accepts<CreateConversionRequest>("application/json")
    .Produces<CreateConversionResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status502BadGateway)
    .ProducesProblem(StatusCodes.Status500InternalServerError)
    .WithName("CreateConversion");

app.MapGet(
    "/api/conversions",
    async (int? limit, ConversionAuditApi api, HttpContext http) =>
    {
        var safeLimit = Math.Clamp(limit ?? 20, 1, 100);
        var items = await api.GetRecentConversionsAsync(safeLimit);
        return Results.Ok(new { items });
    })
    .Produces(StatusCodes.Status200OK);

app.Run();

// ------------------- Domain / DTOs -------------------

public static class EnvVar
{
    public static string Required(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Missing required environment variable: {key}");
        return value;
    }
}

public sealed record CosmosOptions(
    string CosmosDbUri,
    string DatabaseName,
    string ContainerName,
    string AccountName,
    string ResourceGroup,
    string Region,
    string ManagedIdentityClientId,
    string PartitionKeyValue)
{
    public static CosmosOptions FromEnvironment()
    {
        // Known Constraint: exact keys from docs/CONTAINER_ENVIRONMENT_VARIABLES.md
        var cosmosDbUri = EnvVar.Required("COSMOS_DB_URI");
        var databaseName = EnvVar.Required("COSMOS_DB_DATABASE");
        var containerName = EnvVar.Required("COSMOS_DB_CONTAINER");
        var accountName = EnvVar.Required("COSMOS_DB_ACCOUNT_NAME");
        var resourceGroup = EnvVar.Required("COSMOS_DB_RESOURCE_GROUP");
        var region = EnvVar.Required("COSMOS_DB_REGION");
        var managedIdentityClientId = EnvVar.Required("AZURE_MANAGED_IDENTITY_CLIENT_ID");

        return new CosmosOptions(
            CosmosDbUri: cosmosDbUri,
            DatabaseName: databaseName,
            ContainerName: containerName,
            AccountName: accountName,
            ResourceGroup: resourceGroup,
            Region: region,
            ManagedIdentityClientId: managedIdentityClientId,
            PartitionKeyValue: "all");
    }
}

public sealed record CreateConversionRequest
{
    public required decimal Amount { get; init; }
    public required string SourceCurrency { get; init; }
    public required string TargetCurrency { get; init; }

    public bool IsValid(out string validationProblem)
    {
        validationProblem = "";
        if (Amount <= 0) { validationProblem = "amount must be > 0"; return false; }
        if (string.IsNullOrWhiteSpace(SourceCurrency) || SourceCurrency.Length < 3) { validationProblem = "sourceCurrency must be a currency code"; return false; }
        if (string.IsNullOrWhiteSpace(TargetCurrency) || TargetCurrency.Length < 3) { validationProblem = "targetCurrency must be a currency code"; return false; }
        return true;
    }
}

public sealed record CreateConversionResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    [JsonPropertyName("sourceCurrency")]
    public required string SourceCurrency { get; init; }
    [JsonPropertyName("targetCurrency")]
    public required string TargetCurrency { get; init; }
    [JsonPropertyName("originalAmount")]
    public required decimal OriginalAmount { get; init; }
    [JsonPropertyName("conversionRate")]
    public required decimal ConversionRate { get; init; }
    [JsonPropertyName("convertedAmount")]
    public required decimal ConvertedAmount { get; init; }
    [JsonPropertyName("providerDateMarker")]
    public string? ProviderDateMarker { get; init; }
    [JsonPropertyName("providerSequenceMarker")]
    public string? ProviderSequenceMarker { get; init; }
    [JsonPropertyName("executedAtUtc")]
    public required DateTime ExecutedAtUtc { get; init; }
}

// ------------------- External Provider Adapter -------------------

public sealed record FxRateResult(decimal Rate, string? ProviderDateMarker, string? ProviderSequenceMarker);

public sealed class ExternalCurrencyRateProviderAdapter
{
    public FxRateResult Parse(string payloadJson, string sourceCurrency, string targetCurrency)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            var rate = TryExtractRate(root, sourceCurrency, targetCurrency, out var extractedRate)
                ? extractedRate
                : throw new InvalidOperationException("Could not extract conversion rate from provider payload");

            var providerDate = TryExtractString(root, new[] { "date", "provider_date", "providerDate" });
            var providerSequence = TryExtractString(root, new[] { "sequence", "provider_sequence", "providerSequence", "timestamp", "time" });

            return new FxRateResult(rate, providerDate, providerSequence);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to parse provider payload", ex);
        }
    }

    private static bool TryExtractRate(System.Text.Json.JsonElement root, string sourceCurrency, string targetCurrency, out decimal rate)
    {
        rate = 0m;

        // Support explicit `rate`.
        if (root.ValueKind == System.Text.Json.JsonValueKind.Object && root.TryGetProperty("rate", out var rateEl))
        {
            if (TryGetDecimal(rateEl, out rate)) return true;
        }

        // Support mapping-based schemas: `rates` and `conversion_rates`.
        if (root.TryGetProperty("rates", out var ratesEl) && ratesEl.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (TryGetDecimalFromMapping(ratesEl, targetCurrency, out rate)) return true;
        }

        if (root.TryGetProperty("conversion_rates", out var conversionRatesEl) && conversionRatesEl.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (TryGetDecimalFromMapping(conversionRatesEl, targetCurrency, out rate)) return true;

            // Sometimes the mapping key is like FROM_TO.
            var composite1 = $"{sourceCurrency}_{targetCurrency}";
            var composite2 = $"{sourceCurrency}-{targetCurrency}";
            if (TryGetDecimalFromMapping(conversionRatesEl, composite1, out rate)) return true;
            if (TryGetDecimalFromMapping(conversionRatesEl, composite2, out rate)) return true;
        }

        return false;
    }

    private static bool TryGetDecimal(System.Text.Json.JsonElement el, out decimal value)
    {
        value = 0m;
        if (el.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
            if (el.TryGetDecimal(out value)) return true;
            if (el.TryGetDouble(out var d)) { value = (decimal)d; return true; }
        }
        if (el.ValueKind == System.Text.Json.JsonValueKind.String && decimal.TryParse(el.GetString(), out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    private static bool TryGetDecimalFromMapping(System.Text.Json.JsonElement mappingObj, string key, out decimal value)
    {
        value = 0m;
        foreach (var prop in mappingObj.EnumerateObject())
        {
            if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase))
            {
                return TryGetDecimal(prop.Value, out value);
            }
        }
        return false;
    }

    private static string? TryExtractString(System.Text.Json.JsonElement root, IEnumerable<string> candidateKeys)
    {
        foreach (var key in candidateKeys)
        {
            if (root.ValueKind == System.Text.Json.JsonValueKind.Object && root.TryGetProperty(key, out var el))
            {
                if (el.ValueKind == System.Text.Json.JsonValueKind.String) return el.GetString();
                if (el.ValueKind == System.Text.Json.JsonValueKind.Number) return el.GetRawText();
            }
        }
        return null;
    }
}

public sealed class CurrencyRateProviderClient
{
    private readonly HttpClient _http;
    private readonly ExternalCurrencyRateProviderAdapter _adapter;

    public CurrencyRateProviderClient(HttpClient http, ExternalCurrencyRateProviderAdapter adapter)
    {
        _http = http;
        _adapter = adapter;
    }

    public async Task<FxRateResult> GetRateAsync(string sourceCurrency, string targetCurrency, CancellationToken ct)
    {
        var source = Uri.EscapeDataString(sourceCurrency.ToUpperInvariant());
        var target = Uri.EscapeDataString(targetCurrency.ToUpperInvariant());

        // Frankfurter-like endpoints: /latest?from=...&to=... (amount optional).
        var url = $"latest?from={source}&to={target}";

        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url, ct);
        }
        catch (Exception ex)
        {
            throw new CurrencyProviderUnavailableException("Currency provider request failed", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new CurrencyProviderUnavailableException($"Currency provider returned {(int)response.StatusCode}");
        }

        var payload = await response.Content.ReadAsStringAsync(ct);
        try
        {
            return _adapter.Parse(payload, sourceCurrency, targetCurrency);
        }
        catch (Exception ex)
        {
            throw new CurrencyProviderUnavailableException("Currency provider response could not be interpreted", ex);
        }
    }
}

public sealed class CurrencyProviderUnavailableException : Exception
{
    public CurrencyProviderUnavailableException(string message, Exception? innerException = null) : base(message, innerException) { }
}

// ------------------- Audit Persistence -------------------

public interface IConversionAuditRepository
{
    Task<string> AddAsync(ConversionAuditEntity entity, CancellationToken ct);
    Task<IReadOnlyList<ConversionAuditEntity>> GetRecentAsync(int limit, CancellationToken ct);
}

public sealed class ConversionAuditEntity
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    [JsonPropertyName("pk")]
    public required string Pk { get; set; }

    [JsonPropertyName("sourceCurrency")]
    public required string SourceCurrency { get; set; }
    [JsonPropertyName("targetCurrency")]
    public required string TargetCurrency { get; set; }
    [JsonPropertyName("originalAmount")]
    public required decimal OriginalAmount { get; set; }
    [JsonPropertyName("conversionRate")]
    public required decimal ConversionRate { get; set; }
    [JsonPropertyName("convertedAmount")]
    public required decimal ConvertedAmount { get; set; }
    [JsonPropertyName("providerDateMarker")]
    public string? ProviderDateMarker { get; set; }
    [JsonPropertyName("providerSequenceMarker")]
    public string? ProviderSequenceMarker { get; set; }
    [JsonPropertyName("executedAtUtc")]
    public required DateTime ExecutedAtUtc { get; set; }
}

public sealed class CosmosConversionAuditRepository : IConversionAuditRepository
{
    private readonly Container _container;
    private readonly string _pkValue;

    public CosmosConversionAuditRepository(CosmosClient cosmosClient, string databaseName, string containerName, string pkValue)
    {
        _container = cosmosClient.GetContainer(databaseName, containerName);
        _pkValue = pkValue;
    }

    public async Task<string> AddAsync(ConversionAuditEntity entity, CancellationToken ct)
    {
        // Ensure deterministic partition key for ordered queries.
        entity.Pk = _pkValue;
        await _container.UpsertItemAsync(entity, new PartitionKey(entity.Pk), cancellationToken: ct);
        return entity.Id;
    }

    public async Task<IReadOnlyList<ConversionAuditEntity>> GetRecentAsync(int limit, CancellationToken ct)
    {
        var query = new Microsoft.Azure.Cosmos.QueryDefinition(
            "SELECT * FROM c WHERE c.pk = @pk ORDER BY c.executedAtUtc DESC OFFSET 0 LIMIT @limit");
        query.WithParameter("@pk", _pkValue);
        query.WithParameter("@limit", limit);

        using var iterator = _container.GetItemQueryIterator<ConversionAuditEntity>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(_pkValue) });

        var results = new List<ConversionAuditEntity>(capacity: limit);
        while (iterator.HasMoreResults)
        {
            foreach (var item in await iterator.ReadNextAsync(ct))
                results.Add(item);
        }
        return results;
    }
}

public sealed class CosmosDataPlaneProvisioner
{
    public static async Task EnsureDatabaseAndContainerAsync(CosmosClient cosmosClient, string databaseName, string containerName)
    {
        // Must run with token-authenticated managed identity; startup must fail if this throws.
        var dbResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
        var db = cosmosClient.GetDatabase(databaseName);

        var containerProperties = new ContainerProperties(containerName, "/pk")
        {
            DefaultTimeToLive = -1
        };

        await db.CreateContainerIfNotExistsAsync(containerProperties, throughput: 400);
    }
}

public sealed class CosmosArmProvisioner
{
    public static async Task TryProvisionAsync(
        TokenCredential credential,
        CosmosOptions cosmosOptions,
        string appDatabaseName,
        string appContainerName)
    {
        try
        {
            var subscriptionId = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
            if (string.IsNullOrWhiteSpace(subscriptionId)) return;

            var armClient = new ArmClient(credential, subscriptionId);

            var account = await armClient.GetCosmosDBAccounts().GetAsync(cosmosOptions.ResourceGroup, cosmosOptions.AccountName);

            // Best-effort SQL database/container provisioning.
            // Use dynamic to avoid hard-coding fragile SDK model types; any ARM RBAC issues are ignored.
            dynamic sqlDatabases = account.Value.GetSqlDatabases();
            await sqlDatabases.CreateOrUpdateAsync(WaitUntil.Completed, appDatabaseName, null);

            dynamic sqlDatabase = sqlDatabases.Get(appDatabaseName).Value;
            dynamic sqlContainers = sqlDatabase.GetSqlContainers();
            await sqlContainers.CreateOrUpdateAsync(WaitUntil.Completed, appContainerName, null);
        }
        catch
        {
            // Spec: ARM provisioning is best-effort; Managed Identity RBAC may differ.
        }
    }
}

// ------------------- Conversion Workflow -------------------

public sealed class CurrencyConversionService
{
    public decimal ComputeConvertedAmount(decimal amount, decimal conversionRate)
    {
        // Monetary values rounded to 2 fractional digits for display.
        return Math.Round(amount * conversionRate, 2, MidpointRounding.AwayFromZero);
    }
}

public sealed class ConversionAuditApi
{
    private readonly CurrencyConversionService _conversionService;
    private readonly IConversionAuditRepository _auditRepository;
    private readonly CurrencyRateProviderClient _rateProvider;

    public ConversionAuditApi(
        CurrencyConversionService conversionService,
        IConversionAuditRepository auditRepository,
        CurrencyRateProviderClient rateProvider)
    {
        _conversionService = conversionService;
        _auditRepository = auditRepository;
        _rateProvider = rateProvider;
    }

    public async Task<IResult> CreateConversionAsync(CreateConversionRequest request)
    {
        var executedAtUtc = DateTime.UtcNow;
        var source = request.SourceCurrency.Trim().ToUpperInvariant();
        var target = request.TargetCurrency.Trim().ToUpperInvariant();

        try
        {
            var fx = await _rateProvider.GetRateAsync(source, target, CancellationToken.None);
            var converted = _conversionService.ComputeConvertedAmount(request.Amount, fx.Rate);

            var entity = new ConversionAuditEntity
            {
                Id = Guid.NewGuid().ToString(),
                Pk = "all",
                SourceCurrency = source,
                TargetCurrency = target,
                OriginalAmount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
                ConversionRate = fx.Rate,
                ConvertedAmount = converted,
                ProviderDateMarker = fx.ProviderDateMarker,
                ProviderSequenceMarker = fx.ProviderSequenceMarker,
                ExecutedAtUtc = executedAtUtc,
            };

            await _auditRepository.AddAsync(entity, CancellationToken.None);

            var response = new CreateConversionResponse
            {
                Id = entity.Id,
                SourceCurrency = entity.SourceCurrency,
                TargetCurrency = entity.TargetCurrency,
                OriginalAmount = entity.OriginalAmount,
                ConversionRate = entity.ConversionRate,
                ConvertedAmount = entity.ConvertedAmount,
                ProviderDateMarker = entity.ProviderDateMarker,
                ProviderSequenceMarker = entity.ProviderSequenceMarker,
                ExecutedAtUtc = entity.ExecutedAtUtc,
            };

            return Results.Ok(response);
        }
        catch (CurrencyProviderUnavailableException)
        {
            return Results.Problem(
                detail: "Unable to retrieve live conversion rate at this time.",
                statusCode: StatusCodes.Status502BadGateway,
                title: "Currency provider error",
                instance: $"/api/conversions");
        }
        catch (Exception)
        {
            return Results.Problem(
                detail: "Conversion failed due to an internal server error.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Conversion failed",
                instance: $"/api/conversions");
        }
    }

    public async Task<IReadOnlyList<ConversionAuditEntity>> GetRecentConversionsAsync(int limit)
    {
        return await _auditRepository.GetRecentAsync(limit, CancellationToken.None);
    }
}

// DI uses only the repository abstraction; no additional cosmos plumbing types are required.
