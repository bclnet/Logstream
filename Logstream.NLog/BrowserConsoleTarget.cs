using Logstream.Server;
using NLog;
using NLog.Targets;
using System;
using System.Threading;

namespace Logstream
{
    /// <summary>
    /// Class BrowserConsoleTarget.
    /// </summary>
    /// <seealso cref="NLog.Targets.TargetWithLayout" />
    [Target("BrowserConsole")]
    public class BrowserConsoleTarget : TargetWithLayout
    {
        readonly ChannelFactory _channelFactory = new ChannelFactory();
        IEventChannel _channel;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="BrowserConsoleTarget"/> is active.
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
        /// Initializes a new instance of the <see cref="BrowserConsoleTarget"/> class.
        /// </summary>
        ///<remarks>The default value of the layout is: <code>${longdate}|${level:uppercase=true}|${logger}|${message}</code></remarks>
        public BrowserConsoleTarget() => Name = "BrowserConsole";
        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserConsoleTarget"/> class.
        /// </summary>
        /// <param name="channelFactory">The channel factory.</param>
        public BrowserConsoleTarget(ChannelFactory channelFactory) => _channelFactory = channelFactory;

        /// <summary>
        /// Initializes the target. Can be used by inheriting classes
        /// to initialize logging.
        /// </summary>
        protected override void InitializeTarget()
        {
            base.InitializeTarget();
            _channel = Active ? _channelFactory.Create(Host, Port, HttpServer.FindCertificate(Certificate), ReplayBufferSize) : null;
        }

        /// <summary>
        /// Writes logging event to the log target. Must be overridden in inheriting
        /// classes.
        /// </summary>
        /// <param name="logEvent">Logging event to be written out.</param>
        protected override void Write(LogEventInfo logEvent)
        {
            if (!Active)
                return;
            var message = Layout.Render(logEvent);
            var sse = new ServerSentEvent(logEvent.Level.Name.ToUpperInvariant(), message);
            _channel.Send(sse, new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        }

        /// <summary>
        /// Closes the target and releases any unmanaged resources.
        /// </summary>
        protected override void CloseTarget()
        {
            _channel?.Dispose();
            base.CloseTarget();
        }
    }
}