using System.Text;
using CoreFlow.OrderFileIngestion.Services;
using CoreFlow.OrderFileIngestion.Validation;
using Xunit;

namespace CoreFlow.OrderFileIngestion.Tests;

public class CsvSchemaValidatorTests
{
    private static Stream AsStream(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public void ValidateAndCountRecords_CountsDataRows()
    {
        const string csv = "order_id,client_id,provider,order_type,asset_symbol,quantity,unit_price,order_date\n" +
                           "o1,c1,provider-a,BUY,AAPL,10,150.50,2026-05-18\n" +
                           "o2,c2,provider-a,SELL,MSFT,5,330.10,2026-05-18\n";

        var count = new CsvSchemaValidator().ValidateAndCountRecords(AsStream(csv));

        Assert.Equal(2, count);
    }

    [Fact]
    public void ValidateAndCountRecords_RejectsMissingColumn()
    {
        const string csv = "order_id,client_id,provider,order_type,asset_symbol,quantity,unit_price\n" +
                           "o1,c1,provider-a,BUY,AAPL,10,150.50\n";

        var ex = Assert.Throws<ValidationException>(() =>
            new CsvSchemaValidator().ValidateAndCountRecords(AsStream(csv)));

        Assert.Equal(ValidationFailureKind.InvalidSchema, ex.Kind);
    }

    [Fact]
    public void ValidateAndCountRecords_RejectsHeaderOnly()
    {
        const string csv = "order_id,client_id,provider,order_type,asset_symbol,quantity,unit_price,order_date\n";

        var ex = Assert.Throws<ValidationException>(() =>
            new CsvSchemaValidator().ValidateAndCountRecords(AsStream(csv)));

        Assert.Equal(ValidationFailureKind.EmptyFile, ex.Kind);
    }
}
