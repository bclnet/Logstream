using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting.Display;
using System;

namespace Logstream
{
    /// <summary>
    /// Class LogstreamExtensions.
    /// </summary>
    public static class LogstreamExtensions
    {
        /// <summary>
        /// The default output template
        /// </summary>
        public const string DefaultOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}";
        /// <summary>
        /// Browsers the console.
        /// </summary>
        /// <param name="sinkConfiguration">The sink configuration.</param>
        /// <param name="restrictedToMinimumLevel">The restricted to minimum level.</param>
        /// <param name="active">if set to <c>true</c> [active].</param>
        /// <param name="port">The port.</param>
        /// <param name="replayBufferSize">Size of the replay buffer.</param>
        /// <param name="outputTemplate">The output template.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <param name="logProperties">if set to <c>true</c> [log properties].</param>
        /// <returns>LoggerConfiguration.</returns>
        public static LoggerConfiguration BrowserConsole(this LoggerSinkConfiguration sinkConfiguration, LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum, bool active = false, int port = 8765, int replayBufferSize = 100, string outputTemplate = DefaultOutputTemplate, IFormatProvider formatProvider = null, bool logProperties = false) =>
            sinkConfiguration.Sink(new BrowserConsoleSink
            {
                Active = active,
                Port = port,
                ReplayBufferSize = replayBufferSize,
                LogProperties = logProperties,
                Formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider),
            }, restrictedToMinimumLevel);
    }
}
