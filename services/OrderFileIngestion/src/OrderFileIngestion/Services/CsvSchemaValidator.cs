using System.Globalization;
using CoreFlow.OrderFileIngestion.Validation;
using CsvHelper;
using CsvHelper.Configuration;
using ValidationException = CoreFlow.OrderFileIngestion.Validation.ValidationException;

namespace CoreFlow.OrderFileIngestion.Services;

public sealed class CsvSchemaValidator
{
    public static readonly IReadOnlyList<string> RequiredColumns = new[]
    {
        "order_id",
        "client_id",
        "provider",
        "order_type",
        "asset_symbol",
        "quantity",
        "unit_price",
        "order_date",
    };

    public int ValidateAndCountRecords(Stream csvStream)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
        };

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, config);

        if (!csv.Read() || !csv.ReadHeader())
        {
            throw new ValidationException(
                ValidationFailureKind.InvalidSchema,
                "CSV is missing the header row.");
        }

        var headers = (csv.HeaderRecord ?? Array.Empty<string>())
            .Select(h => h.Trim().ToLowerInvariant())
            .ToHashSet();

        var missing = RequiredColumns.Where(c => !headers.Contains(c)).ToList();
        if (missing.Count > 0)
        {
            throw new ValidationException(
                ValidationFailureKind.InvalidSchema,
                $"CSV is missing required columns: {string.Join(", ", missing)}.");
        }

        var count = 0;
        while (csv.Read())
        {
            count++;
        }

        if (count == 0)
        {
            throw new ValidationException(
                ValidationFailureKind.EmptyFile,
                "CSV has a header but no data rows.");
        }

        return count;
    }
}
