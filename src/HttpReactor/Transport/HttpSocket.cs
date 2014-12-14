using System;
using System.Net;
using System.Net.Sockets;

namespace HttpReactor.Transport
{
    public sealed class HttpSocket : IHttpSocket
    {
        private readonly Socket _socket;

        public HttpSocket()
        {
            _socket = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp)
            {
                Blocking = false,
                NoDelay = true
            };
        }

        public void Connect(EndPoint endPoint, int timeoutMicros)
        {
            try
            {
                _socket.Connect(endPoint);
            }
            catch (SocketException exception)
            {
                // WSAEWOULDBLOCK 10035
                if (exception.ErrorCode != (int)SocketError.WouldBlock)
                {
                    throw;
                }
            }

            PollOrTimeout("connect", SelectMode.SelectWrite, timeoutMicros);
        }

        public void Reconnect(EndPoint endPoint, int timeoutMicros)
        {
            _socket.Disconnect(true);
            Connect(endPoint, timeoutMicros);
        }

        public int Send(byte[] buffer, int offset, int count, int timeoutMicros)
        {
            PollOrTimeout("send", SelectMode.SelectWrite, timeoutMicros);
            return _socket.Send(buffer, offset, count, SocketFlags.None);
        }

        public int Receive(byte[] buffer, int offset, int count, int timeoutMicros)
        {
            PollOrTimeout("receive", SelectMode.SelectRead, timeoutMicros);
            return _socket.Receive(buffer, offset, count, SocketFlags.None);
        }

        public void Close()
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        public void Dispose()
        {
            _socket.Dispose();
        }

        public int SendBufferSize
        {
            get { return _socket.SendBufferSize; }
        }

        public int ReceiveBufferSize
        {
            get { return _socket.ReceiveBufferSize; }
        }

        private void PollOrTimeout(string socketOperation,
            SelectMode mode, int timeoutMicros)
        {
            if (timeoutMicros < 0 || !_socket.Poll(timeoutMicros, mode))
            {
                ThrowTimeoutException(socketOperation, timeoutMicros);
            }
        }

        private static void ThrowTimeoutException(string socketOperation,
            int timeoutMicros)
        {
            var timeoutMillis = (double)timeoutMicros / 1000;
            throw new TimeoutException(String.Format("{0} timeout {1}",
                socketOperation, TimeSpan.FromMilliseconds(timeoutMillis)));
        }
    }
}
