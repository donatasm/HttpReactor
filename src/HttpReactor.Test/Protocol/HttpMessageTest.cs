using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using HttpReactor.Parser;
using HttpReactor.Protocol;
using HttpReactor.Transport;
using NUnit.Framework;

namespace HttpReactor.Test.Protocol
{
    [TestFixture]
    internal sealed class HttpMessageTest
    {
        private const int _100ms = 100000;
        private const int _8kb = 8192;
        private const int _64kb = 65536;

        [TestCase(0)]
        [TestCase(34)]
        [TestCase(34816)]
        [TestCase(_64kb - _8kb)]
        public void SendHeadersAndBody(int responseBodyLength)
        {
            var buffer = new ArraySegment<byte>(new byte[_64kb]);
            var response = CreateResponse(responseBodyLength);
            using (var socket = new HttpStreamSocket(response))
            using (var message = new HttpMessage(buffer, _8kb))
            {
                message.Socket = socket;
                message.WriteMessageStart("GET / HTTP/1.1");
                message.WriteHeader("User-Agent", "curl/7.37.0");
                message.WriteHeader("Host", "localhost");

                message.Send(_100ms);

                const string expectedRequest =
                    "GET / HTTP/1.1\r\n" +
                    "User-Agent: curl/7.37.0\r\n" +
                    "Host: localhost\r\n" +
                    "\r\n";

                var expectedRequestBytes =
                    Encoding.ASCII.GetBytes(expectedRequest);
                var expectedResponseBytes =
                    Encoding.ASCII.GetBytes(response);

                CollectionAssert.AreEqual(expectedRequestBytes,
                    socket.InputBytes);
                CollectionAssert.AreEqual(expectedResponseBytes,
                    socket.OutputBytes);
            }
        }

        [Test]
        public void MaxHeadersLengthShouldBeWithinMessageBuffer()
        {
            var buffer = new ArraySegment<byte>(new byte[_8kb]);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HttpMessage(buffer, _8kb + 1));
        }

        [Test]
        public void SendResponseMessageTooLarge()
        {
            var buffer = new ArraySegment<byte>(new byte[_64kb]);
            var response = CreateResponse(_64kb);
            using (var socket = new HttpStreamSocket(response))
            using (var message = new HttpMessage(buffer, _8kb))
            {
                message.Socket = socket;
                message.WriteMessageStart("GET / HTTP/1.1");
                message.WriteHeader("User-Agent", "curl/7.37.0");
                message.WriteHeader("Host", "localhost");

                var exception = Assert.Throws<HttpParserException>(() =>
                    message.Send(_100ms));
                Assert.AreEqual("stream ended at an unexpected time",
                    exception.Message);
            }
        }

        [Test]
        public void SendRequestHeadersTooLarge()
        {
            var buffer = new ArraySegment<byte>(new byte[4]);
            using (var message = new HttpMessage(buffer, 4))
            {
                var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                    message.WriteMessageStart("GET / HTTP/1.1"));
                StringAssert.StartsWith("max allowed headers size is 4",
                    exception.Message);
            }
        }

        [Test]
        public void SendRequestBodyTooLarge()
        {
            var buffer = new ArraySegment<byte>(new byte[4]);
            using (var message = new HttpMessage(buffer, 4))
            using (var body = message.GetBodyStream())
            {
                Assert.Throws<NotSupportedException>(() =>
                    body.WriteByte(1));
            }
        }

        [Test]
        public void MultipleDispose()
        {
            var buffer = new ArraySegment<byte>(new byte[0]);
            using (var message = new HttpMessage(buffer, 0))
            {
                message.Dispose();
            }
        }

        [TestCase(0)]
        [TestCase(16)]
        public void ReadResponseBody(int responseBodyLength)
        {
            var buffer = new ArraySegment<byte>(new byte[_64kb]);
            var response = CreateResponse(responseBodyLength);
            using (var socket = new HttpStreamSocket(response))
            using (var message = new HttpMessage(buffer, _8kb))
            {
                message.Socket = socket;
                message.Send(_100ms);

                using (var reader = new StreamReader(message.GetBodyStream()))
                {
                    var responseBody = reader.ReadToEnd();
                    Assert.AreEqual(responseBodyLength, responseBody.Length);
                }
            }
        }

        private static string CreateResponse(int responseBodyLength)
        {
            return new StringBuilder()
                .Append("HTTP/1.1 200 OK\r\n")
                .AppendFormat("Content-Length: {0}\r\n", responseBodyLength)
                .Append("Connection: keep-alive\r\n")
                .Append("\r\n")
                .Append("".PadLeft(responseBodyLength, '.'))
                .ToString();
        }

        private sealed class HttpStreamSocket : IHttpSocket
        {
            private const int MaxBufferSize = 4;
            private readonly MemoryStream _input;
            private readonly MemoryStream _output;

            public HttpStreamSocket(string response)
                : this(new ArraySegment<byte>(Encoding.ASCII.GetBytes(response)))
            {
            }

            private HttpStreamSocket(ArraySegment<byte> output)
            {
                _input = new MemoryStream();
                _output = new MemoryStream(output.Array,
                    output.Offset, output.Count);
            }

            public IEnumerable<byte> InputBytes
            {
                get { return _input.ToArray(); }
            }

            public IEnumerable<byte> OutputBytes
            {
                get { return _output.ToArray(); }
            }

            public int Send(byte[] buffer, int offset, int count,
                int timeoutMicros)
            {
                var size = MaxBufferSize > count ? count : MaxBufferSize;

                _input.Write(buffer, offset, size);

                return size;
            }

            public int Receive(byte[] buffer, int offset, int count,
                int timeoutMicros)
            {
                var size = MaxBufferSize > count ? count : MaxBufferSize;

                return _output.Read(buffer, offset, size);
            }

            public void Connect(EndPoint endPoint, int timeoutMicros)
            {
            }

            public void Reconnect(EndPoint endPoint, int timeoutMicros)
            {
            }

            public void Close()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
