namespace OuterloopLabApi.Cosmos;

public sealed record CosmosSettings(
  string CosmosUri,
  string DatabaseName,
  string ContainerName,
  string AccountName,
  string ResourceGroupName,
  string Region,
  string ManagedIdentityClientId);
