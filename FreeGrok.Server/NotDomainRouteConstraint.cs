using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace FreeGrok.Server
{
    public class NotDomainRouteConstraint : IRouteConstraint
    {
        private readonly IConfiguration configuration;

        public NotDomainRouteConstraint(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public bool Match(HttpContext httpContext,
                            IRouter router,
                            string parameterName,
                            RouteValueDictionary values,
                            RouteDirection routeDirection)
        {
            var host = httpContext.Request.Host.Host;
            if (host == configuration.GetValue<string>("Domain"))
            {
                return false;
            }
            return true;
        }
    }
}
