using Logstream.Server;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Logstream
{
    /// <summary>
    /// Class BrowserConsoleSink.
    /// </summary>
    /// <seealso cref="Serilog.Core.ILogEventSink" />
    /// <seealso cref="System.IDisposable" />
    public class BrowserConsoleSink : ILogEventSink, IDisposable
    {
        readonly ChannelFactory _channelFactory = new ChannelFactory();
        IEventChannel _channel;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="BrowserConsoleSink"/> is initialized.
        /// </summary>
        /// <value><c>true</c> if initialized; otherwise, <c>false</c>.</value>
        public bool Initialized { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="BrowserConsoleSink"/> is active.
        /// </summary>
        /// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
        public bool Active { get; set; } = true;
        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        /// <value>The host.</value>
        public string Host { get; set; }
        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The port.</value>
        public int Port { get; set; } = 8765;
        /// <summary>
        /// Gets or sets the certificate.
        /// </summary>
        /// <value>The certificate.</value>
        public string Certificate { get; set; }
        /// <summary>
        /// Gets or sets the size of the replay buffer.
        /// </summary>
        /// <value>The size of the replay buffer.</value>
        public int ReplayBufferSize { get; set; } = 10;
        /// <summary>
        /// Gets or sets a value indicating whether [log properties].
        /// </summary>
        /// <value><c>true</c> if [log properties]; otherwise, <c>false</c>.</value>
        public bool LogProperties { get; set; }
        /// <summary>
        /// Gets or sets the formatter.
        /// </summary>
        /// <value>The formatter.</value>
        public MessageTemplateTextFormatter Formatter { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserConsoleSink"/> class.
        /// </summary>
        public BrowserConsoleSink() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserConsoleSink"/> class.
        /// </summary>
        /// <param name="channelFactory">The channel factory.</param>
        public BrowserConsoleSink(ChannelFactory channelFactory) => _channelFactory = channelFactory;

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            Initialized = true;
            _channel = Active ? _channelFactory.Create(Host, Port, HttpServer.FindCertificate(Certificate), ReplayBufferSize) : null;
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            if (!Initialized)
                Initialize();
            if (!Active)
                return;
            var b = new StringWriter();
            Formatter.Format(logEvent, b);
            if (LogProperties)
                b.WriteLine(Environment.NewLine + string.Join(Environment.NewLine, logEvent.Properties.Select(x => $"[{x.Key},{x.Value}]")));
            var sse = new ServerSentEvent(MatchLevel(logEvent.Level), b.ToString());
            _channel.Send(sse, new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!Initialized)
                Initialize();
            _channel?.Dispose();
        }

        string MatchLevel(LogEventLevel level)
        {
            switch (level)
            {
                case LogEventLevel.Debug:
                case LogEventLevel.Verbose: return "DEBUG";
                default:
                case LogEventLevel.Information: return "INFO";
                case LogEventLevel.Warning: return "WARN";
                case LogEventLevel.Error:
                case LogEventLevel.Fatal: return "ERROR";
            }
        }
    }
}