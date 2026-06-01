using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using CoreFlow.OrderFileIngestion.Logging;
using CoreFlow.OrderFileIngestion.Models;
using CoreFlow.OrderFileIngestion.Options;
using CoreFlow.OrderFileIngestion.Services;
using CoreFlow.OrderFileIngestion.Validation;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CoreFlow.OrderFileIngestion;

public sealed class Function
{
    private readonly IAmazonS3 _s3;
    private readonly IAmazonSimpleNotificationService _sns;
    private readonly IngestionOptions _options;
    private readonly FileMetadataValidator _metadataValidator;
    private readonly CsvSchemaValidator _schemaValidator;
    private readonly ReconciliationEventPublisher _publisher;
    private readonly ILogger<Function> _logger;

    public Function()
        : this(new AmazonS3Client(), new AmazonSimpleNotificationServiceClient(), IngestionOptions.FromEnvironment())
    {
    }

    public Function(
        IAmazonS3 s3,
        IAmazonSimpleNotificationService sns,
        IngestionOptions options)
    {
        _s3 = s3;
        _sns = sns;
        _options = options;

        var loggerFactory = LoggingConfiguration.CreateLoggerFactory();

        _logger = loggerFactory.CreateLogger<Function>();
        _metadataValidator = new FileMetadataValidator(options);
        _schemaValidator = new CsvSchemaValidator();
        _publisher = new ReconciliationEventPublisher(
            sns,
            loggerFactory.CreateLogger<ReconciliationEventPublisher>());
    }

    public async Task HandleAsync(S3Event s3Event, ILambdaContext context)
    {
        if (s3Event.Records is null || s3Event.Records.Count == 0)
        {
            _logger.LogWarning("Received S3 event with no records. RequestId={RequestId}", context.AwsRequestId);
            return;
        }

        foreach (var record in s3Event.Records)
        {
            var bucket = record.S3.Bucket.Name;
            var key = Uri.UnescapeDataString(record.S3.Object.Key.Replace('+', ' '));
            var size = record.S3.Object.Size;

            try
            {
                await ProcessAsync(bucket, key, size, context.AwsRequestId, CancellationToken.None);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(
                    "Validation failed for s3://{Bucket}/{Key}. Kind={Kind} Message={Message} RequestId={RequestId}",
                    bucket, key, ex.Kind, ex.Message, context.AwsRequestId);
                // Validation failures are terminal for this file; do not rethrow so the
                // Lambda invocation succeeds and S3 does not retry indefinitely.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected failure processing s3://{Bucket}/{Key}. RequestId={RequestId}",
                    bucket, key, context.AwsRequestId);
                throw; // surface to Lambda so the retry/DLQ policy takes over
            }
        }
    }

    private async Task ProcessAsync(
        string bucket,
        string key,
        long size,
        string requestId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Ingestion started bucket={Bucket} key={Key} size={Size} requestId={RequestId}",
            bucket, key, size, requestId);

        var metadata = _metadataValidator.Validate(key, size);

        using var response = await _s3.GetObjectAsync(bucket, key, cancellationToken);
        await using var stream = response.ResponseStream;

        var records = _schemaValidator.ValidateAndCountRecords(stream);

        var batch = new ReconciliationBatchEvent
        {
            BatchId = Guid.NewGuid().ToString(),
            Records = records,
            Bucket = bucket,
            Key = key,
            Provider = metadata.Provider,
            ReceivedAt = DateTime.UtcNow,
            CorrelationId = requestId
        };

        await _publisher.PublishAsync(_options.ReconciliationTopicArn, batch, cancellationToken);

        _logger.LogInformation(
            "Ingestion completed batchId={BatchId} records={Records} provider={Provider} key={Key}",
            batch.BatchId, batch.Records, batch.Provider, key);
    }
}
