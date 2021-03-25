using FreeGrok.Common.Dtos;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FreeGrok.Server.ServerHandlers
{
    public interface IWebSocketServerHandler
    {
        Task OnRequestAsync(HttpContext httpContext, CancellationToken cancellationToken);
        Task FinishWebSocketAsync(Guid webSocketId);
        Task FinishWebSocketsAsync(string host);

        Task OnSendWSDataAsync(WebSocketDataDto webSocketData, CancellationToken cancellationToken);
    }
}