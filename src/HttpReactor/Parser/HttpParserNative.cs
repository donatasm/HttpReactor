﻿using System;
using System.Runtime.InteropServices;

namespace HttpReactor.Parser
{
    internal static class HttpParserNative
    {
        private const string HttpParserLib = "lib/HttpParser_x64.dll";

        [DllImport(HttpParserLib, EntryPoint = "http_parser_version")]
        public static extern ulong Version();

        [DllImport(HttpParserLib, EntryPoint = "http_parser_size")]
        public static extern int Size();

        [DllImport(HttpParserLib, EntryPoint = "http_parser_init")]
        public static extern void Init(IntPtr parser, HttpParserType type);

        [DllImport(HttpParserLib, EntryPoint = "http_parser_execute")]
        public static extern UIntPtr Execute(IntPtr parser,
            ref HttpParserSettings settings, IntPtr data, UIntPtr length);

        [DllImport(HttpParserLib, EntryPoint = "http_parser_err_message")]
        public static extern IntPtr ErrorMessage(IntPtr parser);

        public static string ErrorMessageString(IntPtr parser)
        {
            return AsString(ErrorMessage(parser));
        }

        public static string AsString(IntPtr ptr)
        {
            return Marshal.PtrToStringAnsi(ptr);
        }
    }
}