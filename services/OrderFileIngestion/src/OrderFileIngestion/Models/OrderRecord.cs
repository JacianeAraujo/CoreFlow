namespace CoreFlow.OrderFileIngestion.Models;

public sealed class OrderRecord
{
    public string OrderId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string AssetSymbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime OrderDate { get; set; }
}
