using Logstream.Server;
using NSubstitute;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Logstream.Serilog
{
    public class BrowserConsoleSinkTest
    {
        IEventChannel _channel;
        BrowserConsoleSink _sink;

        [SetUp]
        public void ConfigureLogger()
        {
            var formatter = new MessageTemplateTextFormatter("${date} [${threadid}] ${level} ${logger} ${ndc} - ${message}${newline}", null);

            var channelFactory = Substitute.For<ChannelFactory>();
            _channel = Substitute.For<IEventChannel>();
            channelFactory.Create("localhost", 8765, null, 1).Returns(_channel);
            _sink = new BrowserConsoleSink(channelFactory)
            {
                Active = true,
                Port = 8765,
                ReplayBufferSize = 1,
                LogProperties = false,
                Formatter = formatter,
            };
        }

        [Test]
        public void Should_have_no_side_effect_if_active_flag_set_to_false()
        {
            // given
            var channelFactory = Substitute.For<ChannelFactory>();
            _channel = Substitute.For<IEventChannel>();
            channelFactory.Create(Arg.Any<string>(), 8765, null, 1).Returns(_channel);
            var messageTemplateTextFormatter = Substitute.For<MessageTemplateTextFormatter>(LogstreamExtensions.DefaultOutputTemplate, null);
            var sink = new BrowserConsoleSink(channelFactory)
            {
                Active = false,
                Port = 8765,
                ReplayBufferSize = 1,
                Formatter = messageTemplateTextFormatter,
                LogProperties = false
            };

            var logEvent = new LogEvent(
                DateTime.UtcNow,
                LogEventLevel.Information,
                new Exception("test"),
                GenerateMessageTemplate("message"),
                new LogEventProperty[0]);

            // when
            sink.Emit(logEvent);

            // then
            messageTemplateTextFormatter.DidNotReceiveWithAnyArgs().Format(logEvent, new StringWriter());
        }

        [Test]
        public void Should_send_an_sse_message_when_receiving_a_logging_event()
        {
            // given
            var channelFactory = Substitute.For<ChannelFactory>();
            _channel = Substitute.For<IEventChannel>();
            channelFactory.Create(Arg.Any<string>(), 8765, null, 1).Returns(_channel);
            var messageTemplateTextFormatter = new MessageTemplateTextFormatter(LogstreamExtensions.DefaultOutputTemplate, null);
            var sink = new BrowserConsoleSink(channelFactory)
            {
                Active = true,
                Port = 8765,
                ReplayBufferSize = 1,
                Formatter = messageTemplateTextFormatter,
                LogProperties = false
            };

            var logEvent = new LogEvent(
                DateTime.UtcNow,
                LogEventLevel.Information,
                null,
                GenerateMessageTemplate("message"),
                new[] { new LogEventProperty("test", new ScalarValue("value")) });

            // when
            sink.Emit(logEvent);

            // then
            _channel.Received().Send(Arg.Is<ServerSentEvent>(evt => evt.ToString().Contains("message")), Arg.Any<CancellationToken>());
        }

        static MessageTemplate GenerateMessageTemplate(string text) => new MessageTemplate(text, new List<MessageTemplateToken> { new TextToken(text) });

        [Test]
        public void Should_send_an_sse_message_with_a_type_matching_received_logging_event_level()
        {
            // given
            var channelFactory = Substitute.For<ChannelFactory>();
            _channel = Substitute.For<IEventChannel>();
            channelFactory.Create(Arg.Any<string>(), 8765, null, 1).Returns(_channel);
            var messageTemplateTextFormatter = new MessageTemplateTextFormatter(LogstreamExtensions.DefaultOutputTemplate, null);
            var sink = new BrowserConsoleSink(channelFactory)
            {
                Active = true,
                Port = 8765,
                ReplayBufferSize = 1,
                Formatter = messageTemplateTextFormatter,
                LogProperties = false,
            };

            var logEvent = new LogEvent(
                DateTime.UtcNow,
                LogEventLevel.Warning,
                null,
                GenerateMessageTemplate("message"),
                new[] { new LogEventProperty("test", new ScalarValue("value")) });

            // when
            sink.Emit(logEvent);

            // then
            _channel.Received().Send(Arg.Is<ServerSentEvent>(evt => evt.ToString().StartsWith("event: WARN")), Arg.Any<CancellationToken>());
        }

        [Test]
        public void Should_send_an_sse_error_message_when_received_logging_event_level_similar_using_a_matching()
        {
            // given
            var channelFactory = Substitute.For<ChannelFactory>();
            _channel = Substitute.For<IEventChannel>();
            channelFactory.Create(Arg.Any<string>(), 8765, null, 1).Returns(_channel);
            var messageTemplateTextFormatter = new MessageTemplateTextFormatter(LogstreamExtensions.DefaultOutputTemplate, null);
            var sink = new BrowserConsoleSink(channelFactory)
            {
                Active = true,
                Port = 8765,
                ReplayBufferSize = 1,
                Formatter = messageTemplateTextFormatter,
                LogProperties = false,
            };

            var logEvent = new LogEvent(
                DateTime.UtcNow,
                LogEventLevel.Fatal,
                null,
                GenerateMessageTemplate("message"),
                new[] { new LogEventProperty("test", new ScalarValue("value")) });

            // when
            sink.Emit(logEvent);

            // then
            _channel.Received().Send(Arg.Is<ServerSentEvent>(evt => evt.ToString().StartsWith("event: ERROR")), Arg.Any<CancellationToken>());
        }

        [Test]
        public void Should_send_a_multiline_sse_message_received_logging_event_for_an_exception()
        {
            // given
            var channelFactory = Substitute.For<ChannelFactory>();
            _channel = Substitute.For<IEventChannel>();
            channelFactory.Create(Arg.Any<string>(), 8765, null, 1).Returns(_channel);
            var messageTemplateTextFormatter = new MessageTemplateTextFormatter(LogstreamExtensions.DefaultOutputTemplate, null);
            var sink = new BrowserConsoleSink(channelFactory)
            {
                Active = true,
                Port = 8765,
                ReplayBufferSize = 1,
                Formatter = messageTemplateTextFormatter,
                LogProperties = false,
            };

            var logEvent = new LogEvent(
                DateTime.UtcNow,
                LogEventLevel.Fatal,
                new Exception("exception-message"),
                GenerateMessageTemplate("message"),
                new[] { new LogEventProperty("test", new ScalarValue("value")) });

            // when
            sink.Emit(logEvent);

            // then
            var lineSeparator = new[] { "\r\n" };
            _channel.Received().Send(
                Arg.Is<ServerSentEvent>(
                    evt => evt.ToString()
                        .Split(lineSeparator, StringSplitOptions.RemoveEmptyEntries)
                        .Skip(1)
                        .All(l => l.StartsWith("data:"))
                ), Arg.Any<CancellationToken>());
        }

        [Test]
        public void Should_dispose_channel_on_shutdown()
        {
            // given
            var channelFactory = Substitute.For<ChannelFactory>();
            _channel = Substitute.For<IEventChannel>();
            channelFactory.Create(Arg.Any<string>(), 8765, null, 1).Returns(_channel);
            var sink = new BrowserConsoleSink(channelFactory)
            {
                Active = true,
                Port = 8765,
                ReplayBufferSize = 1,
                Formatter = null,
                LogProperties = false,
            };

            //When
            sink.Dispose();

            // then
            _channel.Received().Dispose();
        }
    }
}
