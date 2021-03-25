using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace FreeGrok.Server.Persistence
{
    public class ClientStore : IClientStore
    {
        private readonly ConcurrentDictionary<string, string> connectionIdToHostMap = new();
        private readonly ConcurrentDictionary<string, IClientProxy> hostToClientProxyMap = new();
        public IClientProxy GetClientProxy(string host)
        {
            return hostToClientProxyMap.TryGetValue(host, out var id) ? id : null;
        }

        public bool TryAddClient(string connectionId, string host, IClientProxy caller)
        {
            return hostToClientProxyMap.TryAdd(host, caller) && connectionIdToHostMap.TryAdd(connectionId, host);
        }

        public bool TryRemoveClient(string connectionId, out string host)
        {
            var haveHost = connectionIdToHostMap.TryRemove(connectionId, out host);
            if (!haveHost)
            {
                return false;
            }

            hostToClientProxyMap.TryRemove(host, out _);
            return true;
        }
    }
}
