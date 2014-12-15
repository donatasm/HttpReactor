using System;

namespace HttpReactor
{
    public sealed class HttpReactorException : Exception
    {
        public HttpReactorException(string message)
            : base(message)
        {
        }
    }
}
