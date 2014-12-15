using System;
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
        private const int MaxHeadersSize = 8192;
        private readonly HttpMessage _message;
        private readonly IEndPoints _endPoints;
        private readonly int _connectTimeoutMicros;
        private readonly int _sendTimeoutMicros;
        private HttpSocket _socket;
        private bool _isConnected;
        private bool _isFaulted;

        public HttpClient(IEndPoints endPoints,
            TimeSpan connectTimeout, TimeSpan sendTimeout)
        {
            var buffer = new ArraySegment<byte>(new byte[DefaultBufferSize]);
            _message = new HttpMessage(buffer, MaxHeadersSize);
            _endPoints = endPoints;
            _connectTimeoutMicros = connectTimeout.TotalMicroseconds();
            _sendTimeoutMicros = sendTimeout.TotalMicroseconds();

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

        public void Send()
        {
            if (_isFaulted)
            {
                _socket.Dispose();
                SocketInit();
            }

            try
            {
                if (!_isConnected)
                {
                    _socket.Connect(_endPoints.Next(),
                        _connectTimeoutMicros);
                    _isConnected = true;
                }

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

        private void SocketInit()
        {
            _socket = new HttpSocket();
            _message.Socket = _socket;
            _isConnected = false;
            _isFaulted = false;
        }
    }
}
