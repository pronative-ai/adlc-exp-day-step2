using System.Text.RegularExpressions;

namespace OuterloopLabApi.Conversion;

public static class ConversionRequestValidator
{
    private static readonly Regex CurrencyCode = new("^[A-Z]{3}$", RegexOptions.Compiled);

    public static string? Validate(ConversionRequest request)
    {
        if (request is null)
        {
            return "Request body is required.";
        }

        if (request.Amount <= 0)
        {
            return "Amount must be greater than 0.";
        }

        var source = request.SourceCurrency?.Trim();
        var target = request.TargetCurrency?.Trim();
        if (string.IsNullOrWhiteSpace(source) || !CurrencyCode.IsMatch(source))
        {
            return "SourceCurrency must be an uppercase 3-letter currency code.";
        }

        if (string.IsNullOrWhiteSpace(target) || !CurrencyCode.IsMatch(target))
        {
            return "TargetCurrency must be an uppercase 3-letter currency code.";
        }

        request.SourceCurrency = source!;
        request.TargetCurrency = target!;
        return null;
    }
}
