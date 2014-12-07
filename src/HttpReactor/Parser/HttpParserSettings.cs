using System.Runtime.InteropServices;

namespace HttpReactor.Parser
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct HttpParserSettings
    {
        public HttpCallback OnMessageBegin;
        public HttpDataCallback OnUrl;
        public HttpDataCallback OnStatus;
        public HttpDataCallback OnHeaderField;
        public HttpDataCallback OnHeaderValue;
        public HttpCallback OnHeadersComplete;
        public HttpDataCallback OnBody;
        public HttpCallback OnMessageComplete;
    }
}