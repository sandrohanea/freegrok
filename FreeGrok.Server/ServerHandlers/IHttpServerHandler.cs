using FreeGrok.Common.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FreeGrok.Server.ServerHandlers
{
    public interface IHttpServerHandler
    {
        Task OnRequestAsync(HttpContext httpContext, CancellationToken cancellationToken);

        Task OnResponseDataAsync(ResponseContentDto responseContentDto, CancellationToken cancellationToken);

        Task OnResponseAsync(ResponseDto responseDto, CancellationToken cancellationToken);

        void FinishRequests(string host);
    }
}