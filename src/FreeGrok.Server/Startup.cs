using FreeGrok.Common;
using FreeGrok.Server.Hubs;
using FreeGrok.Server.Middlewares;
using FreeGrok.Server.Persistence;
using FreeGrok.Server.ServerHandlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FreeGrok.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR().AddHubOptions<RoutingHub>(h => h.MaximumReceiveMessageSize = long.MaxValue);
            services.AddRouting(options =>
            {
                options.ConstraintMap.Add("notDomainRoute", typeof(NotDomainRouteConstraint));
            });
            services.AddSingleton<IClientStore, ClientStore>();
            services.AddSingleton<IHttpServerHandler, HttpServerHandler>();
            services.AddSingleton<IWebSocketServerHandler, WebSocketServerHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseWebSockets();
            app.UseRouting();
            app.UseMiddleware<HttpForwardMiddleware>();
            app.UseMiddleware<WebSocketForwardMiddleware>();
            app.UseEndpoints(endpoints =>
            {
                endpoints.Map("{**path:notDomainRoute}", HandleRequestAsync);
                endpoints.MapHub<RoutingHub>("routing");
            });
        }

        /// <summary>
        /// This is only needed to map the endpoint to something so <seealso cref="HttpForwardMiddleware"/> will be invoked.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Task HandleRequestAsync(HttpContext context)
        {
            return Task.CompletedTask;
        }
    }
}
