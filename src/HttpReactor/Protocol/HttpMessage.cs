using System;
using System.Globalization;
using System.IO;
using System.Text;
using HttpReactor.Parser;
using HttpReactor.Transport;
using HttpReactor.Util;

namespace HttpReactor.Protocol
{
    public sealed class HttpMessage : IDisposable
    {
        private static readonly ArraySegment<byte> EmptyBuffer = 
            new ArraySegment<byte>(new byte[0]);
        private static readonly byte[] CrLf = { 13, 10 };
        private static readonly byte[] HeaderSeparator = { 58, 32 };
        private static readonly Encoding Ascii = Encoding.ASCII;
        private const string ContentLengthHeader = "Content-Length";
        private readonly BufferStream _requestHeaders;
        private readonly BufferStream _requestBody;
        private readonly ArraySegment<byte> _buffer;
        private readonly int _maxHeadersLength;
        private readonly IHttpSocket _socket;
        private readonly HttpParser _parser;
        private bool _messageComplete;
        private int _parsedBodyOffset;
        private int _parsedBodyLength;

        public HttpMessage(ArraySegment<byte> buffer, int maxHeadersLength,
            IHttpSocket socket)
        {
            if (maxHeadersLength > buffer.Count)
                throw new ArgumentOutOfRangeException("maxHeadersLength");

            _maxHeadersLength = maxHeadersLength;

            _buffer = buffer;
            _socket = socket;
            _parser = new HttpParser(HttpParserType.Response,
                new HttpMessageHandler(this));

            _requestHeaders = new BufferStream(buffer.Array,
                buffer.Offset, maxHeadersLength, true);

            _requestBody = new BufferStream(buffer.Array, maxHeadersLength,
                buffer.Count - maxHeadersLength, true);

            Recycle();
        }

        public void Recycle()
        {
            Status = null;
            _parsedBodyOffset = -1;
            _parsedBodyLength = 0;
            _messageComplete = false;
            _requestHeaders.Position = 0;
            _requestBody.Position = 0;
        }

        public void WriteMessageStart(string line)
        {
            var lineBytes = Ascii.GetBytes(line);
            WriteHeaderStream(lineBytes);
            WriteHeaderStream(CrLf);
        }

        public void WriteHeader(string header, string value)
        {
            var headerBytes = Ascii.GetBytes(header);
            var valueBytes = Ascii.GetBytes(value);
            WriteHeaderStream(headerBytes);
            WriteHeaderStream(HeaderSeparator);
            WriteHeaderStream(valueBytes);
            WriteHeaderStream(CrLf);
        }

        public void WriteContentLength()
        {
            var bodyLength = (int)_requestBody.Position;
            WriteHeader(ContentLengthHeader,
                bodyLength.ToString(CultureInfo.InvariantCulture));
        }

        public void Send(int timeoutMicros)
        {
            // append separator between headers and body
            WriteHeaderStream(CrLf);

            var microsLeft = timeoutMicros;

            microsLeft = SendAllHeaders(microsLeft);
            microsLeft = SendAllBody(microsLeft);
            ReceiveAll(microsLeft);
        }

        public string Status { get; private set; }

        public Stream GetBodyStream()
        {
            if (!_messageComplete)
            {
                return new BufferStream(_buffer.Array, _maxHeadersLength,
                    _buffer.Count - _maxHeadersLength, true);
            }

            if (IsParsedBodyEmpty)
            {
                return new BufferStream(EmptyBuffer, false);
            }

            return new BufferStream(_buffer.Array, _parsedBodyOffset,
                _parsedBodyLength, false);
        }

        public void Dispose()
        {
            _requestHeaders.Dispose();
            _requestBody.Dispose();
            _parser.Dispose();
        }

        internal int SendAllHeaders(int microsLeft)
        {
            return SendAll(ToArraySegment(_requestHeaders), microsLeft);
        }

        internal int SendAllBody(int microsLeft)
        {
            return SendAll(ToArraySegment(_requestBody), microsLeft);
        }

        internal void ReceiveAll(int microsLeft)
        {
            ReceiveAll(_buffer, microsLeft);
        }

        private bool IsParsedBodyEmpty
        {
            get { return _parsedBodyOffset < 0; }
        }

        private int SendAll(ArraySegment<byte> buffer, int microsLeft)
        {
            var array = buffer.Array;
            var offset = buffer.Offset;
            var left = buffer.Count; // how many bytes left to send

            while (left > 0)
            {
                var startTimestamp = SystemTimestamp.Current;
                var sent = _socket.Send(array, offset, left, microsLeft);
                var elapsedMicros = SystemTimestamp.GetElapsedMicros(startTimestamp);

                offset += sent;
                left -= sent;
                microsLeft -= elapsedMicros;
            }

            return microsLeft;
        }

        private void ReceiveAll(ArraySegment<byte> buffer, int microsLeft)
        {
            var array = buffer.Array;
            var offset = buffer.Offset;
            var maxSize = buffer.Count;

            var totalReceived = 0;

            while (!_messageComplete)
            {
                var startTimestamp = SystemTimestamp.Current;
                var read = _socket.Receive(array, offset,
                    maxSize - totalReceived, microsLeft);
                var elapsedMicros = SystemTimestamp.GetElapsedMicros(startTimestamp);

                _parser.Execute(new ArraySegment<byte>(array, offset, read));

                offset += read;
                totalReceived += read;
                microsLeft -= elapsedMicros;
            }
        }

        private void WriteHeaderStream(byte[] buffer)
        {
            WriteStream("headers", _requestHeaders, buffer);
        }

        private static void WriteStream(string name, MemoryStream stream,
            byte[] buffer)
        {
            var length = buffer.Length;

            if (length > stream.Capacity - stream.Position)
            {
                throw new ArgumentOutOfRangeException(name,
                    String.Format("max allowed {0} size is {1}",
                        name, stream.Capacity));
            }

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
                if (_message.IsParsedBodyEmpty)
                {
                    _message._parsedBodyOffset = body.Offset;
                }

                _message._parsedBodyLength += body.Count;
            }

            public void OnMessageComplete()
            {
                _message._messageComplete = true;
            }
        }
    }
}
