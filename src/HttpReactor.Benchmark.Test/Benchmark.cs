using System;
using System.Net;
using HttpReactor.Client;
using System.IO;
using System.Net.Sockets;
using System.Diagnostics;

namespace HttpReactor.Benchmark.Test
{
    internal static class Benchmark
    {
        public static void Main()
        {
            const int iterations = 100000;
            var endPoint = new SingleEndPoint("127.0.0.1", 80);
            var connectTimeout = TimeSpan.FromMilliseconds(100);
            var sendTimeout = TimeSpan.FromMilliseconds(100);
            var http200s = 0;
            var socketExceptions = 0;
            var timeoutExceptions = 0;

            using (var client = new HttpClient(endPoint, connectTimeout, sendTimeout))
            {
                var stopwatch = Stopwatch.StartNew();

                for (var i = 0; i < iterations; i++)
                {
                    client.WriteMessageStart("GET / HTTP/1.1");
                    client.WriteHeader("User-Agent", "curl/7.37.0");
                    client.WriteHeader("Host", "localhost");

                    try
                    {
                        client.Send();

                        if (client.Status == "OK")
                        {
                            http200s++;
                        }

                        using (var reader = new StreamReader(client.GetBodyStream()))
                        {
                            reader.ReadToEnd();
                        }
                    }
                    catch (SocketException)
                    {
                        socketExceptions++;
                    }
                    catch (TimeoutException)
                    {
                        timeoutExceptions++;
                    }
                    finally
                    {
                        client.Recycle();
                    }
                }

                var elapsed = stopwatch.Elapsed;
                Console.WriteLine(elapsed);
                Console.WriteLine("{0} ops/sec",
                    iterations / elapsed.TotalSeconds);
                Console.WriteLine();
                Console.WriteLine("Iterations: {0}", iterations);
                Console.WriteLine("Http 200: {0}", http200s);
                Console.WriteLine("Socket exceptions: {0}", socketExceptions);
                Console.WriteLine("Timeout exception: {0}", timeoutExceptions);
            }
        }

        private sealed class SingleEndPoint : IEndPoints
        {
            private readonly IPEndPoint _endPoint;

            public SingleEndPoint(string ip, int port)
            {
                _endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            }

            public EndPoint Next()
            {
                return _endPoint;
            }
        }
    }
}
