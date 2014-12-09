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

        public void Connect(EndPoint endPoint, TimeSpan timeout)
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

            if (!Poll(timeout, SelectMode.SelectWrite))
            {
                ThrowTimeoutException("connect", timeout);
            }
        }

        public int Send(ArraySegment<byte> buffer, TimeSpan timeout)
        {
            if (!Poll(timeout, SelectMode.SelectWrite))
            {
                ThrowTimeoutException("send", timeout);
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

        private bool Poll(TimeSpan timeout, SelectMode mode)
        {
            var totalMicroseconds = (int)(timeout.TotalMilliseconds * 1000);
            return _socket.Poll(totalMicroseconds, mode);
        }

        private static void ThrowTimeoutException(string socketOperation,
            TimeSpan timeout)
        {
            throw new TimeoutException(String.Format("{0} timeout {1}",
                socketOperation, timeout));
        }
    }
}
