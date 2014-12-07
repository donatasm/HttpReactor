using System;

namespace HttpReactor.Parser
{
    public sealed class HttpParserException : Exception
    {
        public HttpParserException(string message)
            : base(message)
        {
        }
    }
}
