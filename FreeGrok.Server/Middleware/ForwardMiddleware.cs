using FreeGrok.Server.Extensions;
using FreeGrok.Server.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace FreeGrok.Server.Middleware
{
    public abstract class ForwardMiddleware
    {
        private readonly RequestDelegate next;

        public ForwardMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        protected async Task<bool> ShouldForwardAsync(HttpContext context, IConfiguration configuration, IClientStore clientStore)
        {
            var host = context.GetHost();
            if (host == configuration.GetValue<string>("Domain"))
            {
                await next(context);
                return false;
            }

            var clientProxy = clientStore.GetClientProxy(host);
            if (clientProxy == null)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"Cannot find any route for {host}");
                return false;
            }

            return true;

        }
    }
}
