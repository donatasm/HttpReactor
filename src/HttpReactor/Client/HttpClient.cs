using System;
using System.Net;
using HttpReactor.Transport;
using HttpReactor.Protocol;
using System.IO;
using System.Net.Sockets;
using HttpReactor.Util;

namespace HttpReactor.Client
{
    internal sealed class HttpClient : IHttpClient
    {
        private const int DefaultBufferSize = 65536;
        private const int DefaultMaxHeadersSize = 8192;
        private static readonly TimeSpan DefaultConnectionExpire =
            TimeSpan.MaxValue;
        private readonly HttpMessage _message;
        private readonly IEndPoints _endPoints;
        private readonly int _connectTimeoutMicros;
        private readonly int _sendTimeoutMicros;
        private readonly long _connectionExpireMicros;
        private HttpSocket _socket;
        private long _connectTimestamp;
        private bool _isConnected;
        private bool _isConnecting;
        private bool _isFaulted;

        public HttpClient(IEndPoints endPoints,
            TimeSpan connectTimeout, TimeSpan sendTimeout)
            : this(endPoints, connectTimeout, sendTimeout,
                DefaultConnectionExpire)
        {
        }

        public HttpClient(IEndPoints endPoints,
            TimeSpan connectTimeout, TimeSpan sendTimeout,
            TimeSpan connectionExpire)
        {
            var buffer = new ArraySegment<byte>(new byte[DefaultBufferSize]);
            _message = new HttpMessage(buffer, DefaultMaxHeadersSize);
            _endPoints = endPoints;
            _connectTimeoutMicros = connectTimeout.TotalMicroseconds();
            _sendTimeoutMicros = sendTimeout.TotalMicroseconds();

            _connectionExpireMicros = (connectionExpire == DefaultConnectionExpire)
                ? Int64.MaxValue : connectionExpire.TotalMicroseconds();

            SocketInit();
        }

        public void WriteMessageStart(string line)
        {
            _message.WriteMessageStart(line);
        }

        public void WriteHeader(string header, string value)
        {
            _message.WriteHeader(header, value);
        }

        public string Status
        {
            get { return _message.Status; }
        }

        public Stream GetBodyStream()
        {
            return _message.GetBodyStream();
        }

        public EndPoint EndPoint { get; private set; }

        public void Send()
        {
            if (_isFaulted)
            {
                SocketReInit();
            }

            try
            {
                EnsureSocketConnected();

                _message.Send(_sendTimeoutMicros);
            }
            catch (SocketException)
            {
                _isFaulted = true;
                throw;
            }
            catch (TimeoutException)
            {
                _isFaulted = true;
                throw;
            }
            finally
            {
                if (!_message.ShouldKeepAlive || IsConnectionExpired)
                {
                    SocketReInit();
                    SocketConnectDontBlock();
                }
            }
        }

        public void Recycle()
        {
            _message.Recycle();
        }

        public void Dispose()
        {
            _message.Dispose();
            _socket.Dispose();
        }

        private void EnsureSocketConnected()
        {
            try
            {
                if (!_isConnected)
                {
                    if (!_isConnecting)
                    {
                        SocketConnectDontBlock();
                    }

                    SocketConnectPoll();
                }
            }
            finally
            {
                _isConnecting = false;
            }
        }

        private void SocketConnectDontBlock()
        {
            EndPoint = _endPoints.Next();

            try
            {
                _socket.ConnectDontBlock(EndPoint);
            }
            finally
            {
                _isConnecting = true;                
            }
        }

        private void SocketConnectPoll()
        {
            _socket.ConnectPoll(_connectTimeoutMicros);
            _connectTimestamp = SystemTimestamp.Current;
            _isConnected = true;
        }

        private void SocketInit()
        {
            _socket = new HttpSocket();
            _message.Socket = _socket;
            _connectTimestamp = 0;
            _isConnected = false;
            _isConnecting = false;
            _isFaulted = false;
            EndPoint = null;
        }

        private void SocketReInit()
        {
            _socket.Dispose();
            SocketInit();
        }

        private bool IsConnectionExpired
        {
            get 
            {
                if (_connectTimestamp == 0)
                {
                    return false;
                }

                var elapsedMicros = SystemTimestamp.GetElapsedMicros(_connectTimestamp);

                return elapsedMicros > _connectionExpireMicros;
            }
        }
    }
}
