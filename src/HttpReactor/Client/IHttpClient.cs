using System;
using System.IO;

namespace HttpReactor.Client
{
    public interface IHttpClient : IDisposable
    {
        void WriteMessageStart(string line);
        void WriteHeader(string header, string value);
        string Status { get; }
        Stream GetBodyStream();
        void Send();
    }
}