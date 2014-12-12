using System;
using System.IO;

namespace HttpReactor.Protocol
{
    internal sealed class BufferStream : MemoryStream
    {
        public BufferStream(byte[] buffer, int offset, int count, bool writable)
            : base(buffer, offset, count, writable, true)
        {
        }

        public BufferStream(ArraySegment<byte> buffer, bool writable)
            : this(buffer.Array, buffer.Offset, buffer.Count, writable)
        {
        }
    }
}
