using System;
using System.Net;

namespace HttpReactor.Benchmark.Test
{
    internal abstract class Benchmark : IDisposable
    {
        public abstract void Init();
        public abstract void Run();
        public abstract void Dispose();

        protected const string ListenUrl = "http://localhost:9090";
        protected const int ListenPort = 9090;
        protected static readonly IPEndPoint ListenEndPoint =
            new IPEndPoint(IPAddress.Loopback, 9090);
    }
}
