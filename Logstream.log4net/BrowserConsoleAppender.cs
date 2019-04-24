using log4net.Appender;
using log4net.Core;
using Logstream.Server;
using System;
using System.Threading;

namespace Logstream
{
    /// <summary>
    /// Class BrowserConsoleAppender.
    /// </summary>
    /// <seealso cref="log4net.Appender.AppenderSkeleton" />
    public class BrowserConsoleAppender : AppenderSkeleton
    {
        readonly ChannelFactory _channelFactory = new ChannelFactory();
        IEventChannel _channel;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="BrowserConsoleAppender"/> is active.
        /// </summary>
        /// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
        public bool Active { get; set; } = true;
        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The port.</value>
        public int Port { get; set; } = 8765;
        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        /// <value>The host.</value>
        public string Host { get; set; }
        /// <summary>
        /// Gets or sets the certificate.
        /// </summary>
        /// <value>The certificate.</value>
        public string Certificate { get; set; }
        /// <summary>
        /// Gets or sets the size of the replay buffer.
        /// </summary>
        /// <value>The size of the replay buffer.</value>
        public int ReplayBufferSize { get; set; } = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserConsoleAppender"/> class.
        /// </summary>
        public BrowserConsoleAppender() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserConsoleAppender"/> class.
        /// </summary>
        /// <param name="channelFactory">The channel factory.</param>
        public BrowserConsoleAppender(ChannelFactory channelFactory) => _channelFactory = channelFactory;

        /// <summary>
        /// Activates the options.
        /// </summary>
        public override void ActivateOptions()
        {
            base.ActivateOptions();
            _channel = Active ? _channelFactory.Create(Host, Port, HttpServer.FindCertificate(Certificate), ReplayBufferSize) : null;
        }

        /// <summary>
        /// Pres the append check.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool PreAppendCheck() => !Active ? false : base.PreAppendCheck();

        /// <summary>
        /// Appends the specified logging event.
        /// </summary>
        /// <param name="loggingEvent">The logging event.</param>
        protected override void Append(LoggingEvent loggingEvent)
        {
            if (!Active)
                return;
            var message = RenderLoggingEvent(loggingEvent);
            var sse = new ServerSentEvent(loggingEvent.Level.DisplayName, message);
            _channel.Send(sse, new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        }

        /// <summary>
        /// Raises the Close event.
        /// </summary>
        protected override void OnClose()
        {
            _channel?.Dispose();
            base.OnClose();
        }
    }
}