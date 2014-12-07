using System;
using System.Runtime.InteropServices;

namespace HttpReactor.Parser
{
    internal sealed class UnmanagedMemory : IDisposable
    {
        private IntPtr _intPtr;

        public UnmanagedMemory(int size)
        {
            _intPtr = Marshal.AllocHGlobal(size);
        }

        public IntPtr IntPtr
        {
            get { return _intPtr; }
        }

        public void Dispose()
        {
            if (_intPtr == IntPtr.Zero) return;
            Marshal.FreeHGlobal(_intPtr);
            _intPtr = IntPtr.Zero;
        }
    }
}
