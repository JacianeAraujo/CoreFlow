using CoreFlow.OrderFileIngestion.Options;
using CoreFlow.OrderFileIngestion.Validation;
using Xunit;

namespace CoreFlow.OrderFileIngestion.Tests;

public class FileMetadataValidatorTests
{
    private static FileMetadataValidator BuildValidator() => new(new IngestionOptions
    {
        ReconciliationTopicArn = "arn:aws:sns:us-east-1:000000000000:topic",
        AllowedProviders = new HashSet<string> { "provider-a", "provider-b" },
        MaxFileSizeBytes = 10_000,
        Environment = "test",
    });

    [Fact]
    public void Validate_AcceptsWellFormedKey()
    {
        var metadata = BuildValidator().Validate("provider-a/file-2026-05-18.csv", 500);

        Assert.Equal("provider-a", metadata.Provider);
        Assert.Equal(new DateTime(2026, 5, 18), metadata.FileDate);
    }

    [Fact]
    public void Validate_RejectsEmptyFile()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            BuildValidator().Validate("provider-a/file-2026-05-18.csv", 0));

        Assert.Equal(ValidationFailureKind.EmptyFile, ex.Kind);
    }

    [Fact]
    public void Validate_RejectsOversizedFile()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            BuildValidator().Validate("provider-a/file-2026-05-18.csv", 20_000));

        Assert.Equal(ValidationFailureKind.UnexpectedFileSize, ex.Kind);
    }

    [Fact]
    public void Validate_RejectsUnknownProvider()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            BuildValidator().Validate("provider-z/file-2026-05-18.csv", 500));

        Assert.Equal(ValidationFailureKind.InvalidProvider, ex.Kind);
    }

    [Fact]
    public void Validate_RejectsMalformedKey()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            BuildValidator().Validate("garbage.csv", 500));

        Assert.Equal(ValidationFailureKind.InvalidFilename, ex.Kind);
    }
}
