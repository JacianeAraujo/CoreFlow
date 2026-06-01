using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Compact;

namespace CoreFlow.OrderFileIngestion.Logging;

public static class LoggingConfiguration
{
    public static ILoggerFactory CreateLoggerFactory()
    {
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(new CompactJsonFormatter())
            .CreateLogger();

        return new SerilogLoggerFactory(serilogLogger, dispose: true);
    }
}
