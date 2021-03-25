using FreeGrok.Common.Dtos;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace FreeGrok.Server.Extensions
{
    public static class HttpContextExtensions
    {
        public static string GetHost(this HttpContext httpContext)
        {
            return httpContext.Request.Host.Host;
        }

        public static List<HeaderDto> GetHeaders(this HttpContext httpContext)
        {
            var headers = new List<HeaderDto>();
            foreach (var header in httpContext.Request.Headers)
            {
                headers.Add(new HeaderDto() { Key = header.Key, Value = header.Value });
            }
            return headers;
        }
    }
}
