using System;
using HttpReactor.Protocol;
using HttpReactor.Transport;
using System.Net;
using System.IO;

namespace HttpReactor.Benchmark.Test
{
    internal static class Benchmarks
    {
        private static readonly IPEndPoint ClientEndPoint =
            new IPEndPoint(IPAddress.Loopback, 8081);

        private const int _100ms = 100000;

        public static void Main()
        {
            var buffer = new ArraySegment<byte>(new byte[65536]);

            using (var socket = new HttpSocket())
            using (var message = new HttpMessage(buffer, 8192, socket))
            {
                socket.Connect(ClientEndPoint, _100ms);
                message.WriteMessageStart("POST / HTTP/1.1");
                message.WriteHeader("User-Agent", "curl/7.37.0");
                message.WriteHeader("Host", "localhost");

                using (var writer = new StreamWriter(message.GetBodyStream()))
                {
                    writer.WriteLine("Hello, world!");
                }

                message.Send(_100ms);

                using (var reader = new StreamReader(message.GetBodyStream()))
                {
                    Console.WriteLine(reader.ReadToEnd());
                }

                message.Recycle();
            }

            Console.WriteLine("DONE!");
        }
    }
}
