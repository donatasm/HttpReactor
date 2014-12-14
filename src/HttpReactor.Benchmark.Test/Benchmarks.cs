using System;
using HttpReactor.Protocol;
using HttpReactor.Transport;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Net.Sockets;

namespace HttpReactor.Benchmark.Test
{
    internal static class Benchmarks
    {
        private static readonly IPEndPoint ClientEndPoint =
            new IPEndPoint(IPAddress.Loopback, 8080);

        private const int _100ms = 100000;

        public static void Main()
        {
            var buffer = new ArraySegment<byte>(new byte[65536]);

            using (var socket = new HttpSocket())
            using (var message = new HttpMessage(buffer, 8192, socket))
            {
                socket.Connect(ClientEndPoint, _100ms);
                const int iterations = 100000000;
                var stopwatch = Stopwatch.StartNew();

                for (var i = 0; i < iterations; i++)
                {
                    message.WriteMessageStart("GET / HTTP/1.1");
                    message.WriteHeader("User-Agent", "curl/7.37.0");
                    //message.WriteHeader("Connection", "Keep-Alive");
                    message.WriteHeader("Host", "localhost");

                    try
                    {
                        message.Send(_100ms);

                        using (var reader = new StreamReader(message.GetBodyStream()))
                        {
                            reader.ReadToEnd();
                        }
                    }
                    catch (SocketException)
                    {
                        socket.Reconnect(ClientEndPoint, _100ms);

                        message.Send(_100ms);

                        using (var reader = new StreamReader(message.GetBodyStream()))
                        {
                            reader.ReadToEnd();
                        }
                    }
                    finally
                    {
                        message.Recycle();
                    }
                }

                var elapsed = stopwatch.Elapsed;
                Console.WriteLine(elapsed);
                Console.WriteLine("{0} ops/sec",
                    iterations / elapsed.TotalSeconds);
            }
        }
    }
}
