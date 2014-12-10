using System;
using System.Net;
using HttpReactor.Transport;
using NUnit.Framework;

namespace HttpReactor.Test.Transport
{
    [TestFixture]
    internal sealed class HttpSocketTest
    {
        [Test]
        public void ConnectToNonExistingEndPoint()
        {
            using (var socket = new HttpSocket())
            {
                var endPoint = new IPEndPoint(1, 1);
                Assert.Throws<TimeoutException>(() =>
                    socket.Connect(endPoint, 0));
            }            
        }
    }
}
