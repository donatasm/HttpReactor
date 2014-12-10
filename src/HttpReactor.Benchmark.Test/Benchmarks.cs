using System;
using System.IO;
using HttpReactor.Protocol;
using HttpReactor.Transport;
using Owin;

namespace HttpReactor.Benchmark.Test
{
    internal static class Benchmarks
    {
        public static void Main()
        {
            using (var benchmark = new Get34KB())
            {
                benchmark.Init();
                benchmark.Run();
                Console.ReadKey();
            }
        }

        public sealed class Get34KB : Benchmark
        {
            private static readonly string Response = "".PadLeft(34816, '.');
            private const int MaxHeadersLength = 8192;
            private readonly ArraySegment<byte> _buffer =
                new ArraySegment<byte>(new byte[65536]);
            private IDisposable _server;
            private HttpSocket _client;
            private HttpMessage _httpMessage;

            public override void Init()
            {
                _server = HttpServer.Start(ListenUrl, app =>
                {
                    var readBuffer = new char[65536];

                    app.Run(context =>
                    {
                        using (var reader = new StreamReader(context.Request.Body))
                        {
                            while (reader.Read(readBuffer, 0, readBuffer.Length) > 0)
                            {
                            }
                        }

                        context.Response.ContentType = "text/plain";
                        context.Response.ContentLength = Response.Length;
                        return context.Response.WriteAsync(Response);
                    });
                });

                _client = new HttpSocket();
                _client.Connect(ListenEndPoint, 100);
                _httpMessage = new HttpMessage(_buffer, MaxHeadersLength, _client);
            }

            public override void Run()
            {
                _httpMessage.WriteMessageStart("GET / HTTP/1.1");
                _httpMessage.WriteHeader("User-Agent", "curl/7.37.0");
                _httpMessage.WriteHeader("Host", "localhost");
               
                _httpMessage.Send(100);
            }

            public override void Dispose()
            {
                if (_server != null)
                {
                    _server.Dispose();
                }
            }
        }
    }
}
