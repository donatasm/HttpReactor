using System;

namespace HttpReactor.Protocol
{
    public sealed class HttpMessageTooLargeException : Exception
    {
        public HttpMessageTooLargeException(string message)
            : base(message)
        {
        }
    }
}
