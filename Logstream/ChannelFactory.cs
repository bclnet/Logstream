using Logstream.Server;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Logstream
{
    /// <summary>
    /// Class ChannelFactory.
    /// </summary>
    public class ChannelFactory
    {
        /// <summary>
        /// Creates the specified host.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="replayBufferSize">Size of the replay buffer.</param>
        /// <returns>IEventChannel.</returns>
        public virtual IEventChannel Create(string host, int port = 8765, X509Certificate certificate = null, int replayBufferSize = 1)
        {
            var channel = new MulticastChannel(replayBufferSize);
            var jsTemplate = GetContent(Assembly.GetExecutingAssembly().GetManifestResourceStream("Logstream.Logstream.js"));
            var htmlTemplate = GetContent(Assembly.GetExecutingAssembly().GetManifestResourceStream("Logstream.Default.htm"));
            var html = htmlTemplate.Replace("HOST", host ?? Dns.GetHostName()).Replace("PORT", port.ToString());
            void handler(HttpContext ctx)
            {
                var httpResponse = new HttpResponse(200, "OK");
                if (ctx.HttpRequest.Uri == "/")
                {
                    httpResponse.Headers.Add("Content-Type", "text/html");
                    httpResponse.Headers.Add("Connection", "close");
                    httpResponse.Content = html;
                    ctx.ResponseChannel.Send(httpResponse, ctx.Token).ContinueWith(t => ctx.ResponseChannel.Close());
                }
                else if (ctx.HttpRequest.Uri.Contains(".js"))
                {
                    var js = jsTemplate.Replace("URL_QUERY", ctx.HttpRequest.Uri);
                    httpResponse.Headers.Add("Content-Type", "text/javascript");
                    httpResponse.Headers.Add("Connection", "close");
                    httpResponse.Content = js;
                    ctx.ResponseChannel.Send(httpResponse, ctx.Token).ContinueWith(t => ctx.ResponseChannel.Close());
                }
                else
                {
                    httpResponse.Headers.Add("Content-Type", "text/event-stream");
                    httpResponse.Headers.Add("Cache-Control", "no-cache");
                    httpResponse.Headers.Add("Connection", "keep-alive");
                    httpResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                    ctx.ResponseChannel.Send(httpResponse, ctx.Token)
                        .ContinueWith(t =>
                        {
                            ctx.ResponseChannel.Send(new ServerSentEvent("INFO", $"Connected successfully on LOG stream from {host}:{port}"), ctx.Token);
                            channel.AddChannel(ctx.ResponseChannel, ctx.Token);
                        });
                }
            }
            var httpServer = new HttpServer(host, port, handler);
            channel.AttachServer(httpServer);
            httpServer.Run(certificate);
            return channel;
        }

        static string GetContent(Stream stream)
        {
            using (var sr = new StreamReader(stream))
                return sr.ReadToEnd();
        }
    }
}
