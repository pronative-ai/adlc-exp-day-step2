using System.ComponentModel.DataAnnotations;

namespace OuterloopLabApi.Configuration;

public sealed class CosmosOptions
{
    [Required]
    public string AccountUri { get; set; } = string.Empty;

    [Required]
    public string DatabaseName { get; set; } = string.Empty;

    [Required]
    public string ContainerName { get; set; } = string.Empty;

    [Required]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    public string ResourceGroup { get; set; } = string.Empty;

    [Required]
    public string Region { get; set; } = string.Empty;

    [Required]
    public string ManagedIdentityClientId { get; set; } = string.Empty;
}
