namespace CoreFlow.OrderFileIngestion.Options;

public sealed class IngestionOptions
{
    public required string ReconciliationTopicArn { get; init; }
    public required IReadOnlySet<string> AllowedProviders { get; init; }
    public required long MaxFileSizeBytes { get; init; }
    public required string Environment { get; init; }

    public static IngestionOptions FromEnvironment()
    {
        var providersRaw = System.Environment.GetEnvironmentVariable("ALLOWED_PROVIDERS") ?? string.Empty;
        var providers = providersRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.ToLowerInvariant())
            .ToHashSet();

        var maxSize = long.TryParse(
            System.Environment.GetEnvironmentVariable("MAX_FILE_SIZE_BYTES"),
            out var parsed) ? parsed : 50L * 1024 * 1024;

        return new IngestionOptions
        {
            ReconciliationTopicArn = System.Environment.GetEnvironmentVariable("RECONCILIATION_TOPIC_ARN")
                ?? throw new InvalidOperationException("RECONCILIATION_TOPIC_ARN not configured"),
            AllowedProviders = providers,
            MaxFileSizeBytes = maxSize,
            Environment = System.Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "dev",
        };
    }
}
