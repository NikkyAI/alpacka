using Microsoft.Extensions.Logging;

namespace GitMC.Lib
{
    public static class Logger
    {
        public static ILoggerFactory Factory { get; } = new LoggerFactory()
            .AddDebug()
            .AddConsole();
        
        public static ILogger Create<T>() => Factory.CreateLogger<T>();
    }
}
