using System;
using System.IO;
using System.Net;

namespace HttpReactor.Client
{
    public interface IHttpClient : IDisposable
    {
        void WriteMessageStart(string line);
        void WriteHeader(string header, string value);
        string Status { get; }
        EndPoint EndPoint { get; }
        Stream GetBodyStream();
        void Send();
    }
}