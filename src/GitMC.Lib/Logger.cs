using System;
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
    
    internal class ConsoleLogger : ILogger
    {
        private static readonly string[] _logLevelLookup = { "TRACE", "DEBUG", "INFO", "WARN", "ERROR" };
        
        public IDisposable BeginScope<TState>(TState state) =>
            new NullDisposable();
        
        public bool IsEnabled(LogLevel logLevel) =>
            (logLevel > LogLevel.Information);
        
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                                Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && (exception == null)) return;
            
            // FIXME
            Console.Write($"{ _logLevelLookup[(int)logLevel] }: { message }");
        }
        
        class NullDisposable : IDisposable
            { public void Dispose() {  } }
    }
}
