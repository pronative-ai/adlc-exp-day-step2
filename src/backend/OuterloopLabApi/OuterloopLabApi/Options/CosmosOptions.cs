namespace OuterloopLabApi.Options;

public sealed class CosmosOptions
{
  public string Uri { get; set; } = string.Empty;
  public string DatabaseName { get; set; } = string.Empty;
  public string ContainerName { get; set; } = string.Empty;
  public string ManagedIdentityClientId { get; set; } = string.Empty;
  public string ResourceGroup { get; set; } = string.Empty;
  public string AccountName { get; set; } = string.Empty;
  public string Region { get; set; } = string.Empty;
}
