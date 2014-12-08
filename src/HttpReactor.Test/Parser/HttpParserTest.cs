using System;
using System.Collections.Generic;
using System.Text;
using HttpReactor.Parser;
using NUnit.Framework;

namespace HttpReactor.Test.Parser
{
    [TestFixture]
    internal sealed class HttpParserTest
    {
        [Test]
        public void Constructor()
        {
            using (new HttpParser(HttpParserType.Both,
                new EventHttpParserHandler()))
            {
            }
        }

        [Test]
        public void ExecuteCallbacks()
        {
            const string request = "HTTP/1.1 200 OK\r\n" +
                "Date: Fri, 31 Dec 1999 23:59:59 GMT\r\n" +
                "Content-Type: text/plain\r\n" +
                "Content-Length: 42\r\n" +
                "some-footer: some-value\r\n" +
                "another-footer: another-value\r\n" +
                "\r\n" +
                "abcdefghijklmnopqrstuvwxyz1234567890abcdef";

            var requestBytes = Encoding.UTF8.GetBytes(request);
            var eventHandler = new EventHttpParserHandler();

            using (var parser = new HttpParser(HttpParserType.Both, eventHandler))
            {
                var buffer = new ArraySegment<byte>(requestBytes);
                var parsed = parser.Execute(buffer);

                Assert.AreEqual(requestBytes.Length, parsed);
                CollectionAssert.AreEqual(new[]
                {
                    "OnMessageBegin",
                    "OK",
                    "OnHeadersComplete",
                    "OnBody",
                    "abcdefghijklmnopqrstuvwxyz1234567890abcdef",
                    "OnMessageComplete"
                }, eventHandler.Events);
            }
        }

        private sealed class EventHttpParserHandler : IHttpParserHandler
        {
            private readonly List<string> _events;

            public EventHttpParserHandler()
            {
                _events = new List<string>();
            }

            public IEnumerable<string> Events
            {
                get { return _events; }
            }

            public void OnMessageBegin()
            {
                _events.Add("OnMessageBegin");
            }

            public void OnStatus(string status)
            {
                _events.Add(status);
            }

            public void OnHeadersComplete()
            {
                _events.Add("OnHeadersComplete");
            }

            public void OnBody(ArraySegment<byte> body)
            {
                _events.Add("OnBody");

                var bytes = new byte[body.Count];

                Buffer.BlockCopy(body.Array, body.Offset,
                    bytes, 0, bytes.Length);

                _events.Add(Encoding.UTF8.GetString(bytes));
            }

            public void OnMessageComplete()
            {
                _events.Add("OnMessageComplete");
            }
        }
    }
}
