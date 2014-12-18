using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using HttpReactor.Parser;
using NUnit.Framework;

namespace HttpReactor.Test.Parser
{
    [TestFixture]
    internal sealed class HttpParserNativeTest
    {
        [Test]
        public void Version()
        {
            var version = HttpParserNative.Version();
            var major = (version >> 16) & 255;
            var minor = (version >> 8) & 255;
            var patch = version & 255;
            Assert.AreEqual(2, major);
            Assert.AreEqual(3, minor);
            Assert.AreEqual(0, patch);
        }

        [Test]
        public void Size()
        {
            Assert.That(HttpParserNative.Size() > 0);
        }

        [Test]
        public void Init()
        {
            using (var parser = AllocateForParser())
            {
                HttpParserNative.Init(parser.IntPtr, HttpParserType.Response);
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

            var events = new List<string>();

            var settings = new HttpParserSettings
            {
                OnMessageBegin = parser =>
                {
                    events.Add("OnMessageBegin");
                    return 0;
                },
                OnStatus = (parser, status, len) =>
                {
                    events.Add(GetString(status, len));
                    return 0;
                },
                OnHeaderField = (parser, field, len) =>
                {
                    events.Add(GetString(field, len));
                    return 0;
                },
                OnHeaderValue = (parser, value, len) =>
                {
                    events.Add(GetString(value, len));
                    return 0;
                },
                OnHeadersComplete = parser =>
                {
                    events.Add("OnHeadersComplete");
                    return 0;
                },
                OnBody = (parser, body, len) =>
                {
                    events.Add("OnBody");
                    events.Add(GetString(body, len));
                    return 0;
                },
                OnMessageComplete = parser =>
                {
                    events.Add("OnMessageComplete");
                    return 0;
                }
            };

            var requestBytes = Encoding.UTF8.GetBytes(request);
            var length = new UIntPtr((uint)requestBytes.Length);

            using (var parser = AllocateForParser())
            using (var pin = new BytePin(requestBytes))
            {
                HttpParserNative.Init(parser.IntPtr, HttpParserType.Response);
                var parsed = HttpParserNative.Execute(parser.IntPtr,
                    ref settings, pin[0], length);

                var error = HttpParserNative.ErrorMessageString(parser.IntPtr);

                Assert.AreEqual("success", error);
                Assert.AreEqual(length, parsed);
                CollectionAssert.AreEqual(new []
                {
                    "OnMessageBegin",
                    "OK",
                    "Date", "Fri, 31 Dec 1999 23:59:59 GMT",
                    "Content-Type", "text/plain",
                    "Content-Length", "42",
                    "some-footer", "some-value",
                    "another-footer", "another-value",
                    "OnHeadersComplete",
                    "OnBody",
                    "abcdefghijklmnopqrstuvwxyz1234567890abcdef",
                    "OnMessageComplete"
                }, events);
            }
        }

        [Test]
        public void KeepAliveTrueConnectionHeader()
        {
            const string request = "HTTP/1.1 200 OK\r\n" +
                "Connection: Keep-Alive\r\n" +
                "Content-Length: 0\r\n" +
                "\r\n";

            var keepAlive = GetKeepAliveValueFor(request);

            Assert.AreNotEqual(0, keepAlive);
        }

        [Test]
        public void KeepAliveFalseConnectionHeader()
        {
            const string request = "HTTP/1.1 200 OK\r\n" +
                "Connection: Close\r\n" +
                "Content-Length: 0\r\n" +
                "\r\n";

            var keepAlive = GetKeepAliveValueFor(request);

            Assert.AreEqual(0, keepAlive);
        }

        [Test]
        public void KeepAliveTrue()
        {
            const string request = "HTTP/1.1 200 OK\r\n" +
                "Content-Length: 0\r\n" +
                "\r\n";

            var keepAlive = GetKeepAliveValueFor(request);

            Assert.AreNotEqual(0, keepAlive);
        }

        public int GetKeepAliveValueFor(string request)
        {
            int? keepAlive = null;

            var settings = new HttpParserSettings
            {
                OnMessageBegin = parser => 0,
                OnStatus = (parser, status, len) => 0,
                OnHeaderField = (parser, field, len) => 0,
                OnHeaderValue = (parser, value, len) => 0,
                OnHeadersComplete = parser =>
                {
                    keepAlive = HttpParserNative.ShouldKeepAlive(parser);
                    return 0;
                },
                OnBody = (parser, body, len) => 0,
                OnMessageComplete = parser => 
                {
                    keepAlive = HttpParserNative.ShouldKeepAlive(parser);
                    return 0;
                }
            };

            var requestBytes = Encoding.UTF8.GetBytes(request);
            var length = new UIntPtr((uint)requestBytes.Length);

            using (var parser = AllocateForParser())
            using (var pin = new BytePin(requestBytes))
            {
                HttpParserNative.Init(parser.IntPtr, HttpParserType.Response);
                var parsed = HttpParserNative.Execute(parser.IntPtr,
                    ref settings, pin[0], length);

                var error = HttpParserNative.ErrorMessageString(parser.IntPtr);

                Assert.AreEqual("success", error);
                Assert.AreEqual(length, parsed);
                Assert.IsNotNull(keepAlive);

                return keepAlive.Value;
            }
        }

        [Test]
        public void ErrorMessage()
        {
            using (var parser = AllocateForParser())
            {
                HttpParserNative.Init(parser.IntPtr, HttpParserType.Response);
                var error = HttpParserNative.ErrorMessageString(parser.IntPtr);
                Assert.AreEqual("success", error);
            }
        }

        private static UnmanagedMemory AllocateForParser()
        {
            return new UnmanagedMemory(HttpParserNative.Size());
        }

        private static string GetString(IntPtr ptr, UIntPtr length)
        {
            var size = (int)length.ToUInt32();
            var bytes = new byte[size];
            Marshal.Copy(ptr, bytes, 0, size);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
