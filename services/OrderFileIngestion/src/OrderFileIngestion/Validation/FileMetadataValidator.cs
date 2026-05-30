using System.Text.RegularExpressions;
using CoreFlow.OrderFileIngestion.Options;

namespace CoreFlow.OrderFileIngestion.Validation;

public sealed class FileMetadataValidator
{
    // Expected key shape: <provider>/file-YYYY-MM-DD.csv
    private static readonly Regex KeyPattern = new(
        @"^(?<provider>[a-z0-9-]+)/file-(?<date>\d{4}-\d{2}-\d{2})\.csv$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IngestionOptions _options;

    public FileMetadataValidator(IngestionOptions options)
    {
        _options = options;
    }

    public FileMetadata Validate(string key, long size)
    {
        if (size <= 0)
        {
            throw new ValidationException(
                ValidationFailureKind.EmptyFile,
                $"File '{key}' is empty.");
        }

        if (size > _options.MaxFileSizeBytes)
        {
            throw new ValidationException(
                ValidationFailureKind.UnexpectedFileSize,
                $"File '{key}' is {size} bytes, above the limit of {_options.MaxFileSizeBytes}.");
        }

        var match = KeyPattern.Match(key);
        if (!match.Success)
        {
            throw new ValidationException(
                ValidationFailureKind.InvalidFilename,
                $"Key '{key}' does not match expected pattern '<provider>/file-YYYY-MM-DD.csv'.");
        }

        var provider = match.Groups["provider"].Value.ToLowerInvariant();
        if (!_options.AllowedProviders.Contains(provider))
        {
            throw new ValidationException(
                ValidationFailureKind.InvalidProvider,
                $"Provider '{provider}' is not in the allowed list.");
        }

        if (!DateTime.TryParse(match.Groups["date"].Value, out var fileDate))
        {
            throw new ValidationException(
                ValidationFailureKind.InvalidFilename,
                $"Date segment '{match.Groups["date"].Value}' is not a valid date.");
        }

        return new FileMetadata(provider, fileDate);
    }
}

public sealed record FileMetadata(string Provider, DateTime FileDate);
