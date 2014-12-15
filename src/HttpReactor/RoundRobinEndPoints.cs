using System.Net;
using System.Threading;
using System.Linq;

namespace HttpReactor
{
    public sealed class RoundRobinEndPoints : IEndPoints
    {
        private readonly IPEndPoint[] _ips;
        private long _counter;

        private RoundRobinEndPoints(IPEndPoint[] ips)
        {
            _ips = ips;
            _counter = -1;
        }

        public static RoundRobinEndPoints FromIps(params IPEndPoint[] ips)
        {
            return new RoundRobinEndPoints(ips);
        }

        public static RoundRobinEndPoints FromDns(string hostNameOrAddress,
            int port)
        {
            var hostEntry = Dns.GetHostEntry(hostNameOrAddress);
            var ips = hostEntry.AddressList
                .Select(a => new IPEndPoint(a, port))
                .ToArray();
            return FromIps(ips);
        }

        public EndPoint Next()
        {
            var i = Interlocked.Increment(ref _counter);
            return _ips[i % _ips.Length];
        }
    }
}

