using System;
using System.Runtime.InteropServices;

namespace HttpReactor.Parser
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int HttpDataCallback(IntPtr parser,
        IntPtr data, UIntPtr length);
}