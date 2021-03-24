﻿using FreeGrok.Common;
using FreeGrok.Server.Persistence;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace FreeGrok.Server.Hubs
{
    public class RoutingHub : Hub
    {
        private readonly IStore store;

        public RoutingHub(IStore store)
        {
            this.store = store;
        }

        public bool Register(RegisterDto registerDto)
        {
            return store.TryAddClient(Context.ConnectionId, registerDto.Domain, Clients.Caller);
        }

        public async Task Finish(ResponseDto responseDto)
        {
            var httpContext = store.GetHttpContext(responseDto.RequestId);
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
            await httpContext.Response.BodyWriter.WriteAsync(responseDto.Content);
            //using var ms = new MemoryStream(responseDto.Content);
            //await ms.CopyToAsync(httpContext.Response.Body);
            store.FinishRequest(responseDto.RequestId);
        }


        public override Task OnDisconnectedAsync(Exception exception)
        {
            store.RemoveClient(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
