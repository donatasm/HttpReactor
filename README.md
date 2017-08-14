# HttpReactor

A super primitive and effective http client for c#.
No magic happening behind you can't control.

```
    const int maxClients = 16;
    var endPoints = RoundRobinEndPoints.FromDns("localhost", 8080);
    var connectTimeout = TimeSpan.FromSeconds(1);
    var sendTimeout = TimeSpan.FromMilliseconds(80);
    var connectionExpire = TimeSpan.FromMinutes(10);

    // create a pool of connections
    using (var reactor = new HttpReactor(endPoints,
        maxClients, connectTimeout, sendTimeout, connectionExpire))
    {
        // get a connection from a pool and return it back on Dispose
        using (var client = reactor.GetClient())
        {
            client.WriteMessageStart("GET / HTTP/1.1");
            client.WriteHeader("User-Agent", "curl/7.37.0");
            client.WriteHeader("Host", "localhost");

            client.Send();

            using (var reader = new StreamReader(client.GetBodyStream()))
            {
                var responseBody = reader.ReadToEnd();
            }
        }
    }
```