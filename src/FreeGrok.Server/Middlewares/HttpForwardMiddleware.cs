using FreeGrok.Server.Extensions;
using FreeGrok.Server.Persistence;
using FreeGrok.Server.ServerHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace FreeGrok.Server.Middlewares
{
    public class HttpForwardMiddleware : ForwardMiddleware
    {
        private readonly RequestDelegate next;

        public HttpForwardMiddleware(RequestDelegate next) : base(next)
        {
            this.next = next;
        }
        public async Task InvokeAsync(HttpContext context, IConfiguration configuration, IClientStore clientStore, IHttpServerHandler httpServerHandler)
        {
            var shouldForward = await ShouldForwardAsync(context, configuration, clientStore);
            if (!shouldForward)
            {
                return;
            }

            if (context.WebSockets.IsWebSocketRequest)
            {
                await next(context);
                return;
            }

            await httpServerHandler.OnRequestAsync(context, context.RequestAborted);
        }
    }
}
