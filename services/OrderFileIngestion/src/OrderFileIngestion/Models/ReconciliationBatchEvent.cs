namespace CoreFlow.OrderFileIngestion.Models;

public sealed record ReconciliationBatchEvent
{
    public required string BatchId { get; init; }
    public required int Records { get; init; }
    public required string Bucket { get; init; }
    public required string Key { get; init; }
    public required string Provider { get; init; }
    public required DateTime ReceivedAt { get; init; }
}
