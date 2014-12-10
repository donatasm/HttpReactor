using System;
using System.Collections.Generic;
using System.Net;

namespace HttpReactor.Transport
{
    public interface IHttpSocket : IDisposable
    {
        void Connect(EndPoint endPoint, int timeoutMillis);
        int Send(ArraySegment<byte> buffer, int timeoutMillis);
        int Send(IList<ArraySegment<byte>> buffers, int timeoutMillis);
        int Receive(ArraySegment<byte> buffer, int timeoutMillis);
        void Close();
    }
}