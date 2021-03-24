using FreeGrok.Common;
using FreeGrok.Server.Hubs;
using FreeGrok.Server.Middlewares;
using FreeGrok.Server.Persistence;
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
        private readonly IStore store = new InMemoryStore();
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
            services.AddSingleton(store);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            app.UseMiddleware<ForwardMiddleware>();
            app.UseEndpoints(endpoints =>
            {
                endpoints.Map("{**path:notDomainRoute}", HandleRequestAsync);
                endpoints.MapHub<RoutingHub>("routing");
            });
        }

        private async Task HandleRequestAsync(HttpContext context)
        {
            var host = context.Request.Host.Host;

        }
    }
}
