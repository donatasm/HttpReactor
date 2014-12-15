using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HttpReactor.Benchmark.Test
{
    internal static class Benchmark
    {
        private static readonly SingleEndPoint EndPoint = new SingleEndPoint("127.0.0.1", 80);
        private const int MaxClients = 64;
        private const int Iterations = 100000;
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan SendTimeout = TimeSpan.FromMilliseconds(100);

        public static void Main()
        {
            using (var reactor =
                new HttpReactor(EndPoint, MaxClients,
                    ConnectTimeout, SendTimeout))
            {
                var runnerTasks = new Task[MaxClients];

                for (var i = 0; i < MaxClients; i++)
                {
                    runnerTasks[i] = new Runner(reactor).Start(Iterations);
                }

                Task.WaitAll(runnerTasks);
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

        private sealed class Runner
        {
            private static readonly object _syncRoot = new object();
            private readonly HttpReactor _reactor;

            public Runner(HttpReactor reactor)
            {
                _reactor = reactor;
            }

            public Task Start(int iterations)
            {
                return Task.Factory.StartNew(() =>
                {
                    var http200s = 0;
                    var socketExceptions = new Dictionary<int, int>();
                    var timeoutExceptions = 0;
                    var stopwatch = Stopwatch.StartNew();

                    for (var i = 0; i < iterations; i++)
                    {
                        using (var client = _reactor.GetClient())
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

                                using (var reader =
                                    new StreamReader(client.GetBodyStream()))
                                {
                                    reader.ReadToEnd();
                                }
                            }
                            catch (SocketException e)
                            {
                                int count;

                                if (!socketExceptions.TryGetValue(e.ErrorCode, out count))
                                {
                                    count = 1;
                                }
                                else
                                {
                                    count = count + 1;
                                }

                                socketExceptions[e.ErrorCode] = count;
                            }
                            catch (TimeoutException)
                            {
                                timeoutExceptions++;
                            }
                        }
                    }

                    var elapsed = stopwatch.Elapsed;

                    lock (_syncRoot)
                    {
                        Console.WriteLine("Runner #{0}", Task.CurrentId);
                        Console.WriteLine("------------------------------");
                        Console.WriteLine(elapsed);
                        Console.WriteLine("{0} ops/sec",
                            iterations/elapsed.TotalSeconds);
                        Console.WriteLine();
                        Console.WriteLine("Iterations: {0}", iterations);
                        Console.WriteLine("Http 200: {0}", http200s);
                        Console.WriteLine("Socket exceptions: {0}",
                            SocketExceptionsString(socketExceptions));
                        Console.WriteLine("Timeout exception: {0}", timeoutExceptions);
                        Console.WriteLine();
                    }

                }, TaskCreationOptions.LongRunning);
            }

            private static string SocketExceptionsString(
                IEnumerable<KeyValuePair<int, int>> socketExceptions)
            {
                var exceptions = String.Join(", ",
                    socketExceptions.Select(_ =>
                        String.Format("{0}: {1}", _.Key, _.Value)));

                return "[" + exceptions + "]";
            }
        }
    }
}
