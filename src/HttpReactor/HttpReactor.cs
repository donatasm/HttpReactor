using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using HttpReactor.Client;

namespace HttpReactor
{
    public sealed class HttpReactor : IDisposable
    {
        private readonly ConcurrentQueue<HttpPooledClient> _clientQueue;

        public HttpReactor(IEndPoints endPoints, int maxClients,
            TimeSpan connectTimeout, TimeSpan sendTimeout,
            TimeSpan connectionExpire)
        {
            _clientQueue = new ConcurrentQueue<HttpPooledClient>();

            for (var i = 0; i < maxClients; i++)
            {
                var client = new HttpPooledClient(this, endPoints,
                    connectTimeout, sendTimeout, connectionExpire);

                _clientQueue.Enqueue(client);
            }
        }

        public IHttpClient GetClient()
        {
            HttpPooledClient client;

            if (_clientQueue.TryDequeue(out client))
            {
                return client;
            }

            throw new HttpReactorException("no clients available");
        }

        public void Dispose()
        {
            while (!_clientQueue.IsEmpty)
            {
                HttpPooledClient client;

                if (_clientQueue.TryDequeue(out client))
                {
                    client.Destroy();
                }
            }
        }

        private sealed class HttpPooledClient : IHttpClient
        {
            private readonly HttpReactor _reactor;
            private readonly HttpClient _client;

            public HttpPooledClient(HttpReactor reactor, IEndPoints endPoints,
                TimeSpan connectTimeout, TimeSpan sendTimeout,
                TimeSpan connectionExpire)
            {
                _reactor = reactor;
                _client = new HttpClient(endPoints,
                    connectTimeout, sendTimeout, connectionExpire);
            }

            public void WriteMessageStart(string line)
            {
                _client.WriteMessageStart(line);
            }

            public void WriteHeader(string header, string value)
            {
                _client.WriteHeader(header, value);
            }

            public string Status
            {
                get { return _client.Status; }
            }

            public EndPoint EndPoint
            {
                get { return _client.EndPoint; }
            }

            public Stream GetBodyStream()
            {
                return _client.GetBodyStream();
            }

            public void Send()
            {
                _client.Send();
            }

            public void Dispose()
            {
                _client.Recycle();
                _reactor._clientQueue.Enqueue(this);
            }

            public void Destroy()
            {
                _client.Dispose();
            }
        }
    }
}
