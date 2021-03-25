using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace FreeGrok.Server.Persistence
{
    public interface IClientStore
    {
        bool TryAddClient(string connectionId, string host, IClientProxy caller);

        bool TryRemoveClient(string connectionId, out string host);

        public IClientProxy GetClientProxy(string host);
    }
}
