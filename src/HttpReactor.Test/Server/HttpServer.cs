using System;
using Microsoft.Owin.Hosting;
using Owin;

namespace HttpReactor.Test.Server
{
    internal sealed class HttpServer
    {
        public static IDisposable Start(string url,
            Action<IAppBuilder> startup)
        {
            return WebApp.Start(new StartOptions(url), startup);
        }
    }
}
