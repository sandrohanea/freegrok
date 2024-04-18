# FreeGrok
Open-Source FreeGrok

Easy tunneling to localhost from a public domain using SignalR.

## Getting started

To use FreeGrok, you'll need to deploy the FreeGrok.Server in a public accessible server (e.g. Azure) there is no server for public use.

## Using the client

Once you have the server deployed (let's assume the location is `example.com`), just install the freegrok client:

```
    dotnet tool install -g freegrok
```

And create a tunnel (example for http on port 8080)
```
    freegrok -p 8080 -d "mycustomsubdomain.example.com" -h "localhost:8080" -r "https://example.com/" -t http
```

Note that the server must have the mycustomsubdomain.example.com resolving to it.

## Available client options:

```

  -p, --port         Required. Set the port which will be used with the localhost.

  -d, --domain       Required. Set the public domain which will be used.

  -r, --remoteUrl    Set the remote URL.

  -t, --Type         (Default: https) Set the type of the forwarding.

  -h, --host         Set a value which will override the host header in all requests

  --help             Display this help screen.

  --version          Display version information

```