using System;
using System.Runtime.InteropServices;
using System.Text;

namespace HttpReactor.Parser
{
    public sealed class HttpParser : IDisposable
    {
        private const string Success = "success";
        private readonly UnmanagedMemory _parser;
        private readonly IHttpParserHandler _handler;
        private HttpParserSettings _settings;
        private ArraySegment<byte> _buffer; // current buffer parser executing on
        private IntPtr _bufferPinPtr; // current offset address of pinned byte buffer

        public HttpParser(HttpParserType type, IHttpParserHandler handler)
        {
            _handler = handler;
            _parser = new UnmanagedMemory(HttpParserNative.Size());
            _settings = SetupParserSettings();

            HttpParserNative.Init(_parser.IntPtr, type);
        }

        public int Execute(ArraySegment<byte> buffer)
        {
            _buffer = buffer;

            using (var pin = new BytePin(_buffer.Array))
            {
                _bufferPinPtr = pin[_buffer.Offset];
                var lengthPtr = new UIntPtr((uint)_buffer.Count);
                var parsedPtr = HttpParserNative.Execute(_parser.IntPtr,
                    ref _settings, _bufferPinPtr, lengthPtr);

                EnsureSuccess();

                return (int)parsedPtr.ToUInt32();
            }
        }

        public void Dispose()
        {
            _parser.Dispose();
        }

        private HttpParserSettings SetupParserSettings()
        {
            return new HttpParserSettings
            {
                OnMessageBegin = parserPtr =>
                {
                    _handler.OnMessageBegin();
                    return 0;
                },
                OnStatus = (parserPtr, statusPtr, lengthPtr) =>
                {
                    _handler.OnStatus(GetAsciiString(statusPtr, lengthPtr));
                    return 0;
                },
                OnHeadersComplete = parserPtr =>
                {
                    _handler.OnHeadersComplete();
                    return 0;
                },
                OnBody = (parserPtr, bodyPtr, lengthPtr) =>
                {
                    var bodyOffset = GetBodyOffset(bodyPtr, ref _bufferPinPtr);
                    var bodyParsedLength = (int)lengthPtr.ToUInt32();
                    _handler.OnBody(GetBodyArraySegment(ref _buffer, bodyOffset,
                        bodyParsedLength));
                    return 0;
                },
                OnMessageComplete = parserPtr =>
                {
                    _handler.OnMessageComplete();
                    return 0;
                }
            };
        }

        // Get number of bytes between currently parsed body pointer and pinned
        // buffer pointer (a pointer we started parsing).
        // This is needed for calculating the offset of the body array segment 
        private static int GetBodyOffset(IntPtr bodyPtr, ref IntPtr bufferPinPtr)
        {
            return (int)(bodyPtr.ToInt64() - bufferPinPtr.ToInt64());
        }

        private static ArraySegment<byte> GetBodyArraySegment(ref ArraySegment<byte> buffer,
            int bodyOffset, int bodyParsedLength)
        {
            return new ArraySegment<byte>(buffer.Array,
                buffer.Offset + bodyOffset, bodyParsedLength);
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
