using System;
using System.Net;
using System.Net.Sockets;

namespace HttpReactor.Transport
{
    public sealed class HttpSocket : IDisposable
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

        public void Connect(EndPoint endPoint, int timeoutMillis)
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

            if (!Poll(timeoutMillis, SelectMode.SelectWrite))
            {
                ThrowTimeoutException("connect", timeoutMillis);
            }
        }

        public int Send(ArraySegment<byte> buffer, int timeoutMillis)
        {
            if (!Poll(timeoutMillis, SelectMode.SelectWrite))
            {
                ThrowTimeoutException("send", timeoutMillis);
            }

            return _socket.Send(buffer.Array, buffer.Offset,
                buffer.Count, SocketFlags.None);
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

        private bool Poll(int timeoutMillis, SelectMode mode)
        {
            var totalMicroseconds = timeoutMillis * 1000;
            return _socket.Poll(totalMicroseconds, mode);
        }

        private static void ThrowTimeoutException(string socketOperation,
            int timeoutMillis)
        {
            throw new TimeoutException(String.Format("{0} timeout {1}",
                socketOperation, TimeSpan.FromMilliseconds(timeoutMillis)));
        }
    }
}
