using FreeGrok.Common.Dtos;
using FreeGrok.Server.Persistence;
using FreeGrok.Server.ServerHandlers;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FreeGrok.Server.Hubs
{
    public class RoutingHub : Hub
    {
        private readonly IClientStore store;
        private readonly IHttpServerHandler httpServerHandler;
        private readonly IWebSocketServerHandler webSocketServerHandler;

        public RoutingHub(IClientStore store, IHttpServerHandler httpServerHandler, IWebSocketServerHandler webSocketServerHandler)
        {
            this.store = store;
            this.httpServerHandler = httpServerHandler;
            this.webSocketServerHandler = webSocketServerHandler;
        }

        public bool Register(RegisterDto registerDto)
        {
            return store.TryAddClient(Context.ConnectionId, registerDto.Domain, Clients.Caller);
        }

        public async Task Response(ResponseDto responseDto)
        {
            await httpServerHandler.OnResponseAsync(responseDto, Context.ConnectionAborted);
        }

        public async Task OnResponseData(ResponseContentDto contentDto)
        {
            await httpServerHandler.OnResponseDataAsync(contentDto, Context.ConnectionAborted);
        }

        public async Task OnSendWSData(WebSocketDataDto webSocketDataDto)
        {
            await webSocketServerHandler.OnSendWSDataAsync(webSocketDataDto, Context.ConnectionAborted);
        }

        public Task CloseWS(CloseWebSocketDto closeWebSocketDto)
        {
            return webSocketServerHandler.FinishWebSocketAsync(closeWebSocketDto.WebSocketId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var haveHost = store.TryRemoveClient(Context.ConnectionId, out var host);
            if (haveHost)
            {
                httpServerHandler.FinishRequests(host);
                await webSocketServerHandler.FinishWebSocketsAsync(host);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
