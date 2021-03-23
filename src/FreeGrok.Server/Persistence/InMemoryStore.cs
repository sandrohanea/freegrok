using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace FreeGrok.Server.Persistence
{
    public class InMemoryStore : IStore
    {
        private readonly ConcurrentDictionary<string, string> connectionIdToHostMap = new();
        private readonly ConcurrentDictionary<string, IClientProxy> routes = new();
        private readonly ConcurrentDictionary<Guid, (TaskCompletionSource taskCompletionSource, HttpContext httpContext)> requests = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<Guid>> requestsForHost = new();
        public IClientProxy GetClientProxy(string host)
        {
            return routes.TryGetValue(host, out var id) ? id : null;
        }

        public void RegisterRequest(Guid requestId, string host, TaskCompletionSource taskCompletionSource, HttpContext context)
        {
            requests[requestId] = (taskCompletionSource, context);
            var requestsForCurrentHost = requestsForHost.GetOrAdd(host, (_) => new ConcurrentBag<Guid>());
            requestsForCurrentHost.Add(requestId);
        }

        public bool TryAddClient(string connectionId, string host, IClientProxy caller)
        {
            return routes.TryAdd(host, caller) && connectionIdToHostMap.TryAdd(connectionId, host);
        }

        public HttpContext GetHttpContext(Guid requestId)
        {
            return requests[requestId].httpContext;
        }

        public void FinishRequest(Guid requestId)
        {
            requests.TryRemove(requestId, out var requestData);
            requestData.taskCompletionSource.SetResult();
        }

        public void RemoveClient(string connectionId)
        {
            var haveHost = connectionIdToHostMap.TryRemove(connectionId, out var host);
            if (!haveHost)
            {
                return;
            }
            var haveRoutes = routes.TryRemove(host, out _);
            if (!haveRoutes)
            {
                return;
            }

            var haveRequests = requestsForHost.TryRemove(host, out var currentRequests);
            if (!haveRequests)
            {
                return;
            }
            foreach (var request in currentRequests)
            {
                FinishRequest(request);
            }
        }
    }
}
