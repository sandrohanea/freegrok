using FreeGrok.Common;
using FreeGrok.Server.Persistence;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading;
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

        public void Response(ResponseDto responseDto)
        {
            try
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
                if (!responseDto.HaveContent)
                {
                    store.FinishRequest(responseDto.RequestId);
                }
            }
            catch (Exception ex)
            {
                store.FinishRequest(responseDto.RequestId);
                Console.WriteLine(ex.Message);
            }
        }

        public async Task OnResponseData(ResponseContentDto contentDto)
        {
            var httpContext = store.GetHttpContext(contentDto.RequestId);
            if (contentDto.Data.Length > 0)
            {
                await httpContext.Response.Body.WriteAsync(contentDto.Data, 0, contentDto.DataSize);
            }
            if (contentDto.IsFinished)
            {
                store.FinishRequest(contentDto.RequestId);
            }
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            store.RemoveClient(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
