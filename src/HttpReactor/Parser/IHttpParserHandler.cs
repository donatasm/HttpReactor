using System;

namespace HttpReactor.Parser
{
    public interface IHttpParserHandler
    {
        void OnMessageBegin();
        void OnStatus(string status);
        void OnHeadersComplete();
        void OnBody(ArraySegment<byte> body);
        void OnMessageComplete();
    }
}