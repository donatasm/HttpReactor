using System;
using System.Globalization;
using System.IO;
using System.Text;
using HttpReactor.Parser;
using HttpReactor.Transport;
using HttpReactor.Util;

namespace HttpReactor.Protocol
{
    public sealed class HttpMessage 
    {
        private static readonly byte[] CrLf = { 13, 10 };
        private static readonly byte[] HeaderSeparator = { 58, 32 };
        private static readonly Encoding Ascii = Encoding.ASCII;
        private const string ContentLengthHeader = "Content-Length";
        private readonly MemoryStream _headers;
        private readonly MemoryStream _body;
        private readonly ArraySegment<byte> _buffer;
        private readonly IHttpSocket _socket;
        private readonly HttpParser _parser;
        private bool _messageComplete;

        public HttpMessage(ArraySegment<byte> buffer, int maxHeadersLength,
            IHttpSocket socket)
        {
            _buffer = buffer;
            _socket = socket;
            _parser = new HttpParser(HttpParserType.Response,
                new HttpMessageHandler(this));

            _headers = new MemoryStream(buffer.Array,
                buffer.Offset, maxHeadersLength, true, true);

            _body = new MemoryStream(buffer.Array, maxHeadersLength,
                buffer.Count - maxHeadersLength, true, true);

            Recycle();
        }

        public void Recycle()
        {
            Status = null;
            _messageComplete = false;
            _headers.Position = 0;
            _body.Position = 0;
        }

        public void WriteMessageStart(string line)
        {
            var lineBytes = Ascii.GetBytes(line);
            Write(_headers, lineBytes);
            Write(_headers, CrLf);
        }

        public void WriteHeader(string header, string value)
        {
            var headerBytes = Ascii.GetBytes(header);
            var valueBytes = Ascii.GetBytes(value);
            Write(_headers, headerBytes);
            Write(_headers, HeaderSeparator);
            Write(_headers, valueBytes);
            Write(_headers, CrLf);
        }

        public void WriteContentLength()
        {
            var bodyLength = (int)_body.Position;
            WriteHeader(ContentLengthHeader,
                bodyLength.ToString(CultureInfo.InvariantCulture));
        }

        public void Send(int timeoutMicros)
        {
            // append separator between headers and body
            Write(_headers, CrLf);

            var microsLeft = timeoutMicros;

            microsLeft = SendAll(ToArraySegment(_headers), microsLeft);
            microsLeft = SendAll(ToArraySegment(_body), microsLeft);
            ReceiveAll(_buffer, microsLeft);
        }

        public string Status { get; private set; }

        private int SendAll(ArraySegment<byte> buffer, int microsLeft)
        {
            var array = buffer.Array;
            var offset = buffer.Offset;
            var count = buffer.Count;

            var sent = 0;

            while (sent < count)
            {
                var startTimestamp = SystemTimestamp.Current;
                sent += _socket.Send(array, offset, count, microsLeft);
                var elapsedMicros = SystemTimestamp.GetElapsedMicros(startTimestamp);

                offset += sent;
                count -= sent;
                microsLeft -= elapsedMicros;
            }

            return microsLeft;
        }

        private void ReceiveAll(ArraySegment<byte> buffer, int microsLeft)
        {
            var array = buffer.Array;
            var offset = buffer.Offset;
            var count = buffer.Count;

            var received = 0;

            while (!_messageComplete)
            {
                if (offset > count)
                {
                    throw new HttpMessageTooLargeException("response");
                }

                var startTimestamp = SystemTimestamp.Current;
                var read = _socket.Receive(array, offset,
                    count - received, microsLeft);
                var elapsedMicros = SystemTimestamp.GetElapsedMicros(startTimestamp);

                _parser.Execute(new ArraySegment<byte>(array, offset, read));

                offset += received;
                received += read;
                microsLeft -= elapsedMicros;
            }
        }

        private static void Write(Stream stream, byte[] buffer)
        {
            stream.Write(buffer, 0, buffer.Length);
        }

        private static ArraySegment<byte> ToArraySegment(MemoryStream stream)
        {
            var array = stream.GetBuffer();
            var count = (int)stream.Position;
            return new ArraySegment<byte>(array, 0, count);
        }

        private class HttpMessageHandler : IHttpParserHandler
        {
            private readonly HttpMessage _message;

            public HttpMessageHandler(HttpMessage message)
            {
                _message = message;
            }

            public void OnMessageBegin()
            {
            }

            public void OnStatus(string status)
            {
                _message.Status = status;
            }

            public void OnHeadersComplete()
            {
            }

            public void OnBody(ArraySegment<byte> body)
            {
            }

            public void OnMessageComplete()
            {
                _message._messageComplete = true;
            }
        }
    }
}
