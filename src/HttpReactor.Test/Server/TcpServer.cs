using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HttpReactor.Test.Server
{
    internal sealed class TcpServer : IDisposable
    {
        private readonly TcpListener _listener;
        private Task _clientTask;
        private readonly CancellationTokenSource _cancellation;

        private TcpServer(IPEndPoint endPoint)
        {
            _listener = new TcpListener(endPoint);
            _cancellation = new CancellationTokenSource();
        }

        public static IDisposable Start(IPEndPoint endPoint,
            Action<Socket> accepted)
        {
            var server = new TcpServer(endPoint);
            server._listener.Start();

            server._clientTask = server._listener
                .AcceptSocketAsync()
                .ContinueWith(t =>
                {
                    var tcpClient = t.Result;

                    try
                    {
                        accepted(tcpClient);
                    }
                    finally
                    {
                        tcpClient.Close();                        
                    }
                }, server._cancellation.Token);

            return server;
        }


        public void Dispose()
        {
            try
            {
                _cancellation.Cancel();

                if (_clientTask != null && !_clientTask.IsCanceled)
                {
                    _clientTask.Wait();
                }
            }
            finally
            {
                _listener.Stop();
                _cancellation.Dispose();
            }
        }
    }
}
