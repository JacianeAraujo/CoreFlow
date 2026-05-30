using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CoreFlow.OrderFileIngestion.Models;
using Microsoft.Extensions.Logging;

namespace CoreFlow.OrderFileIngestion.Services;

public sealed class ReconciliationEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IAmazonSimpleNotificationService _sns;
    private readonly ILogger<ReconciliationEventPublisher> _logger;

    public ReconciliationEventPublisher(
        IAmazonSimpleNotificationService sns,
        ILogger<ReconciliationEventPublisher> logger)
    {
        _sns = sns;
        _logger = logger;
    }

    public async Task PublishAsync(
        string topicArn,
        ReconciliationBatchEvent batch,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(batch, JsonOptions);

        var request = new PublishRequest
        {
            TopicArn = topicArn,
            Message = payload,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["provider"] = new() { DataType = "String", StringValue = batch.Provider },
                ["eventType"] = new() { DataType = "String", StringValue = "ReconciliationBatchReadyEvent" },
            },
        };

        var response = await _sns.PublishAsync(request, cancellationToken);

        _logger.LogInformation(
            "Published reconciliation batch {BatchId} (records={Records}) to SNS messageId={MessageId}",
            batch.BatchId,
            batch.Records,
            response.MessageId);
    }
}
