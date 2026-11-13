using System.IO;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace SmartTask.Infrastructure.Serilog;

public static class SerilogConfiguration
{
    public static void Initialize()
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logPath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(logPath, "app-.log"))
            .CreateLogger();
    }
}
