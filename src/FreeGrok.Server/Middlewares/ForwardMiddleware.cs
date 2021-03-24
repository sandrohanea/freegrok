using FreeGrok.Common;
using FreeGrok.Server.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FreeGrok.Server.Middlewares
{
    public class ForwardMiddleware
    {
        private readonly RequestDelegate next;

        public ForwardMiddleware(RequestDelegate next)
        {
            this.next = next;
        }
        public async Task InvokeAsync(HttpContext context, IConfiguration configuration, IStore store)
        {
            var host = context.Request.Host.Host;
            if (host == configuration.GetValue<string>("Domain"))
            {
                await next(context);
                return;
            }

            var clientProxy = store.GetClientProxy(host);
            if (clientProxy == null)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"Cannot find any route for {host}");
                return;
            }

            var headers = new List<HeaderDto>();
            foreach (var header in context.Request.Headers)
            {
                headers.Add(new HeaderDto() { Key = header.Key, Value = header.Value });
            }
            var requestId = Guid.NewGuid();
            var bodyLength = context.Request.ContentLength;
            var bodyStream = context.Request.Body;
            var data = new byte[Constants.StreamBufferSize];
            var dataSize = await bodyStream.ReadAsync(data);
            var haveContent = dataSize != 0;
            await clientProxy.SendAsync(
                "OnInitializeConnection",
                new InitializeConnectionDto()
                {
                    HaveContent = haveContent,
                    BodyLength = bodyLength,
                    Method = context.Request.Method,
                    Headers = headers,
                    Path = context.Request.Path.Value,
                    RequestId = requestId
                });
            var taskCompletionSource = new TaskCompletionSource();
            store.RegisterRequest(requestId, host, taskCompletionSource, context);
            if (haveContent)
            {
                await clientProxy.SendAsync(
                                            "OnRequestData",
                                            new RequestContentDto()
                                            {
                                                Data = data,
                                                IsFinished = dataSize < Constants.StreamBufferSize,
                                                DataSize = dataSize,
                                                RequestId = requestId
                                            });
            }
            while (dataSize == Constants.StreamBufferSize)
            {
                dataSize = await bodyStream.ReadAsync(data);
                await clientProxy.SendAsync(
                                            "OnRequestData",
                                            new RequestContentDto()
                                            {
                                                Data = data,
                                                IsFinished = dataSize < Constants.StreamBufferSize,
                                                DataSize = dataSize,
                                                RequestId = requestId
                                            });

            }

            await taskCompletionSource.Task;
        }
    }
}
