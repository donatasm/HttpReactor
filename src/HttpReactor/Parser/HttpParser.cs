using System;
using System.Runtime.InteropServices;
using System.Text;

namespace HttpReactor.Parser
{
    public sealed class HttpParser : IDisposable
    {
        private const string Success = "success";
        private readonly UnmanagedMemory _parser;

        public HttpParser(HttpParserType type)
        {
            _parser = new UnmanagedMemory(HttpParserNative.Size());

            HttpParserNative.Init(_parser.IntPtr, type);
        }

        public int Execute(ArraySegment<byte> buffer,
            IHttpParserHandler handler)
        {
            using (var pin = new BytePin(buffer.Array))
            {
                var offsetPtr = pin[buffer.Offset];
                var lengthPtr = new UIntPtr((uint)buffer.Count);
                var settings = GetParserSettings(offsetPtr, buffer, handler);
                var parsedPtr = HttpParserNative.Execute(_parser.IntPtr,
                    ref settings, offsetPtr, lengthPtr);

                EnsureSuccess();

                return (int)parsedPtr.ToUInt32();
            }
        }

        public void Dispose()
        {
            _parser.Dispose();
        }

        private static HttpParserSettings GetParserSettings(IntPtr offsetPtr,
            ArraySegment<byte> buffer, IHttpParserHandler handler)
        {
            return new HttpParserSettings
            {
                OnMessageBegin = parserPtr =>
                {
                    handler.OnMessageBegin();
                    return 0;
                },
                OnStatus = (parserPtr, statusPtr, lengthPtr) =>
                {
                    handler.OnStatus(GetAsciiString(statusPtr, lengthPtr));
                    return 0;
                },
                OnHeadersComplete = parserPtr =>
                {
                    handler.OnHeadersComplete();
                    return 0;
                },
                OnBody = (parserPtr, bodyPtr, lengthPtr) =>
                {
                    var parsed = (int)(bodyPtr.ToInt64() - offsetPtr.ToInt64());
                    var length = (int)lengthPtr.ToUInt32();
                    var body = new ArraySegment<byte>(buffer.Array,
                        buffer.Offset + parsed, length);
                    handler.OnBody(body);
                    return 0;
                },
                OnMessageComplete = parserPtr =>
                {
                    handler.OnMessageComplete();
                    return 0;
                }
            };
        }

        private void EnsureSuccess()
        {
            var error = HttpParserNative.ErrorMessageString(_parser.IntPtr);

            if (!error.Equals(Success, StringComparison.InvariantCulture))
            {
                throw new HttpParserException(error);
            }
        }

        private static string GetAsciiString(IntPtr ptr, UIntPtr lengthPtr)
        {
            var length = (int)lengthPtr.ToUInt32();
            var bytes = new byte[length];
            Marshal.Copy(ptr, bytes, 0, length);
            return Encoding.ASCII.GetString(bytes);
        }
    }
}
