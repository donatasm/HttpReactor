using System;
using System.Runtime.InteropServices;

namespace HttpReactor.Parser
{
    internal sealed class BytePin : IDisposable
    {
        private readonly byte[] _bytes;
        private GCHandle _pin;

        public BytePin(byte[] bytes)
        {
            _bytes = bytes;
            _pin = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        }

        public IntPtr this[int index]
        {
            get { return Marshal.UnsafeAddrOfPinnedArrayElement(_bytes, index); }
        }

        public void Dispose()
        {
            if (_pin.IsAllocated)
            {
                _pin.Free();
            }
        }
    }
}
