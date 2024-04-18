using FreeGrok.Common;
using FreeGrok.Common.Dtos;
using FreeGrok.Server.Extensions;
using FreeGrok.Server.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace FreeGrok.Server.ServerHandlers
{
    public class HttpServerHandler : IHttpServerHandler
    {
        private readonly IClientStore store;
        private readonly ConcurrentDictionary<Guid, (TaskCompletionSource taskCompletionSource, HttpContext httpContext)> requests = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<Guid>> requestsForHost = new();

        public HttpServerHandler(IClientStore store)
        {
            this.store = store;
        }

        public async Task OnRequestAsync(HttpContext httpContext, CancellationToken cancellationToken)
        {
            var host = httpContext.GetHost();
            var headers = httpContext.GetHeaders();
            var requestId = Guid.NewGuid();
            var bodyLength = httpContext.Request.ContentLength;
            var bodyStream = httpContext.Request.Body;
            var data = new byte[Constants.StreamBufferSize];
            var dataSize = await bodyStream.ReadAsync(data, cancellationToken);
            var haveContent = dataSize != 0;
            var clientProxy = store.GetClientProxy(host);

            await clientProxy.SendAsync(
                "OnInitializeConnection",
                new InitializeConnectionDto()
                {
                    HaveContent = haveContent,
                    BodyLength = bodyLength,
                    Method = httpContext.Request.Method,
                    Headers = headers,
                    Path = httpContext.Request.Path.Value,
                    RequestId = requestId
                },
                cancellationToken);

            var taskCompletionSource = new TaskCompletionSource();
            RegisterRequest(requestId, host, taskCompletionSource, httpContext);

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
                                            },
                                            cancellationToken);
            }

            while (dataSize == Constants.StreamBufferSize)
            {
                dataSize = await bodyStream.ReadAsync(data, cancellationToken);
                await clientProxy.SendAsync(
                                            "OnRequestData",
                                            new RequestContentDto()
                                            {
                                                Data = data,
                                                IsFinished = dataSize < Constants.StreamBufferSize,
                                                DataSize = dataSize,
                                                RequestId = requestId
                                            },
                                            cancellationToken);

            }

            await taskCompletionSource.Task;
        }

        public Task OnResponseAsync(ResponseDto responseDto, CancellationToken cancellationToken)
        {
            try
            {
                var httpContext = GetHttpContext(responseDto.RequestId);
                httpContext.Response.Headers.Clear();
                foreach (var header in responseDto.Headers)
                {
                    // We need to skip this as the transfer won't be chunked anymore
                    if (header.Key.ToUpper() == "TRANSFER-ENCODING")
                    {
                        continue;
                    }
                    httpContext.Response.Headers.Add(header.Key, header.Value);
                }
                httpContext.Response.StatusCode = responseDto.StatusCode;
                if (!responseDto.HaveContent)
                {
                    FinishRequest(responseDto.RequestId);
                }
            }
            catch (Exception ex)
            {
                FinishRequest(responseDto.RequestId);
                Console.WriteLine(ex.Message);
            }
            return Task.CompletedTask;
        }

        public async Task OnResponseDataAsync(ResponseContentDto responseContentDto, CancellationToken cancellationToken)
        {
            var httpContext = GetHttpContext(responseContentDto.RequestId);
            if (responseContentDto.Data.Length > 0)
            {
                await httpContext.Response.Body.WriteAsync(responseContentDto.Data, 0, responseContentDto.DataSize, cancellationToken);
            }
            if (responseContentDto.IsFinished)
            {
                FinishRequest(responseContentDto.RequestId);
            }
        }

        public void FinishRequests(string host)
        {
            var haveRequests = requestsForHost.TryRemove(host, out var currentRequests);
            if (!haveRequests)
            {
                return;
            }
            foreach (var request in currentRequests)
            {
                FinishRequest(request);
            }
        }

        private void RegisterRequest(Guid requestId, string host, TaskCompletionSource taskCompletionSource, HttpContext context)
        {
            requests[requestId] = (taskCompletionSource, context);
            var requestsForCurrentHost = requestsForHost.GetOrAdd(host, (_) => new ConcurrentBag<Guid>());
            requestsForCurrentHost.Add(requestId);
        }

        private void FinishRequest(Guid requestId)
        {
            requests.TryRemove(requestId, out var requestData);
            requestData.taskCompletionSource?.SetResult();
        }

        private HttpContext GetHttpContext(Guid requestId)
        {
            return requests[requestId].httpContext;
        }
    }
}
