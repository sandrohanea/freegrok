using FreeGrok.Common;
using FreeGrok.Common.Dtos;
using FreeGrok.Common.Extensions;
using FreeGrok.Server.Extensions;
using FreeGrok.Server.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace FreeGrok.Server.ServerHandlers
{
    public class WebSocketServerHandler : IWebSocketServerHandler
    {
        private readonly ConcurrentDictionary<Guid, (TaskCompletionSource taskCompletionSource, WebSocket webSocket)> webSockets = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<Guid>> webSocketsForHost = new();
        private readonly IClientStore store;

        public WebSocketServerHandler(IClientStore store)
        {
            this.store = store;
        }

        public async Task OnRequestAsync(HttpContext httpContext, CancellationToken cancellationToken)
        {
            var host = httpContext.GetHost();
            using var ws = await httpContext.WebSockets.AcceptWebSocketAsync();
            var webSocketId = Guid.NewGuid();
            var tcs = new TaskCompletionSource();
            var clientProxy = store.GetClientProxy(host);

            RegisterWebSocket(webSocketId, host, tcs, ws);
            await clientProxy.SendAsync(
                "OnInitializeWebSocketConnection",
                new InitializeConnectionDto()
                {
                    HaveContent = false,
                    BodyLength = 0,
                    Method = httpContext.Request.Method,
                    Headers = httpContext.GetHeaders(),
                    Path = httpContext.Request.Path.Value,
                    RequestId = webSocketId
                },
                cancellationToken);

            var cts = new CancellationTokenSource();
            while (true)
            {
                if (ws.State != WebSocketState.Open)
                {
                    break;
                }
                var data = new byte[Constants.StreamBufferSize];
                var resultTask = ws.ReceiveAsync(data, cts.Token);
                var completedTask = await Task.WhenAny(tcs.Task, resultTask);
                if (completedTask == tcs.Task)
                {
                    cts.Cancel();
                    break;
                }
                var wsData = new WebSocketDataDto()
                {
                    WebSocketId = webSocketId,
                    Data = data[..resultTask.Result.Count],
                    EndOfMessage = resultTask.Result.EndOfMessage,
                    Type = resultTask.Result.MessageType.AsDataType()
                };

                await clientProxy.SendAsync("OnReceiveWSData", wsData, cancellationToken);
            }
        }

        public async Task OnSendWSDataAsync(WebSocketDataDto webSocketData, CancellationToken cancellationToken)
        {
            var haveWebSocket = webSockets.TryGetValue(webSocketData.WebSocketId, out var webSocketInfo);
            if (!haveWebSocket)
            {
                return;
            }
            if (webSocketInfo.webSocket.State != WebSocketState.Open)
            {
                return;
            }
            if (webSocketData.Type == WebSocketDataType.Close)
            {
                await webSocketInfo.webSocket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, cancellationToken);
                await FinishWebSocketAsync(webSocketData.WebSocketId);
                return;
            }
            await webSocketInfo.webSocket.SendAsync(webSocketData.Data, webSocketData.Type.AsMessageType(), webSocketData.EndOfMessage, cancellationToken);
        }

        public async Task FinishWebSocketsAsync(string host)
        {

            var haveRequests = webSocketsForHost.TryRemove(host, out var currentRequests);
            if (!haveRequests)
            {
                return;
            }
            foreach (var request in currentRequests)
            {
                await FinishWebSocketAsync(request);
            }
        }

        public async Task FinishWebSocketAsync(Guid webSocketId)
        {
            webSockets.TryRemove(webSocketId, out var webSocketInfo);
            webSocketInfo.taskCompletionSource?.SetResult();
            if (webSocketInfo.webSocket != null && webSocketInfo.webSocket.State == WebSocketState.Open)
            {
                await webSocketInfo.webSocket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);
            }

        }

        private void RegisterWebSocket(Guid webSocketId, string host, TaskCompletionSource taskCompletionSource, WebSocket webSocket)
        {
            webSockets[webSocketId] = (taskCompletionSource, webSocket);
            var webSocketsForCurrentHost = webSocketsForHost.GetOrAdd(host, (_) => new ConcurrentBag<Guid>());
            webSocketsForCurrentHost.Add(webSocketId);
        }

    }
}
