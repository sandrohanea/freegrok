using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace FreeGrok.Server.Persistence
{
    public interface IStore
    {
        bool TryAddClient(string connectionId, string host, IClientProxy caller);

        void RemoveClient(string connectionId);

        public IClientProxy GetClientProxy(string host);
        void RegisterRequest(Guid requestId, string host, TaskCompletionSource taskCompletionSource, HttpContext context);

        HttpContext GetHttpContext(Guid requestId);

        void FinishRequest(Guid requestId);
    }
}
