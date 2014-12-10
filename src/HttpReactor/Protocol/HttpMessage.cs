using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using HttpReactor.Transport;

namespace HttpReactor.Protocol
{
    public sealed class HttpMessage 
    {
        private static readonly byte[] CRLF = { 13, 10 };
        private static readonly byte[] HeaderSeparator = { 58, 32 };
        private static readonly Encoding Ascii = Encoding.ASCII;
        private static readonly ArraySegment<byte> EmptyBuffer =
            new ArraySegment<byte>(new byte[0]); 
        private const string ContentLengthHeader = "Content-Length";
        private readonly MemoryStream _headers;
        private readonly MemoryStream _body;
        private readonly List<ArraySegment<byte>> _buffers; 

        private readonly IHttpSocket _socket;

        public HttpMessage(ArraySegment<byte> buffer, int maxHeadersLength,
            IHttpSocket socket)
        {
            _socket = socket;

            _headers = new MemoryStream(buffer.Array,
                buffer.Offset, maxHeadersLength);

            _body = new MemoryStream(buffer.Array,
                maxHeadersLength, buffer.Count - maxHeadersLength);

            _buffers = new List<ArraySegment<byte>>
            {
                EmptyBuffer, EmptyBuffer
            };
        }

        public void WriteMessageStart(string line)
        {
            var lineBytes = Ascii.GetBytes(line);
            Write(_headers, lineBytes);
            Write(_headers, CRLF);
        }

        public void WriteHeader(string header, string value)
        {
            var headerBytes = Ascii.GetBytes(header);
            var valueBytes = Ascii.GetBytes(value);
            Write(_headers, headerBytes);
            Write(_headers, HeaderSeparator);
            Write(_headers, valueBytes);
            Write(_headers, CRLF);
        }

        public void WriteContentLength()
        {
            WriteHeader(ContentLengthHeader,
                BodyLength.ToString(CultureInfo.InvariantCulture));
        }

        public Stream Body
        {
            get { return _body; }
        }

        public long BodyLength
        {
            get { return _body.Position; }
        }

        public void Send(int timeoutMillis)
        {
            Write(_headers, CRLF);

            _socket.Send(_buffers, timeoutMillis);
        }

        private static void Write(Stream stream, byte[] buffer)
        {
            stream.Write(buffer, 0, buffer.Length);
        }
    }
}
