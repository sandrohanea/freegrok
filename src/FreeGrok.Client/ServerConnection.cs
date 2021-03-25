using FreeGrok.Client.Config;
using FreeGrok.Common.Dtos;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeGrok.Client
{
    public class ServerConnection : IAsyncDisposable
    {
        private readonly HubConnection connection;
        private readonly ClientConfig clientConfig;
        private readonly Options options;

        public ServerConnection(ClientConfig clientConfig, Options options)
        {

            var builder = new HubConnectionBuilder()
                           .WithUrl($"{clientConfig.RemoteUrl}routing")
                           .WithAutomaticReconnect();

            this.connection = builder.Build();
            this.clientConfig = clientConfig;
            this.options = options;
        }

        public async Task<bool> InitializeAsync()
        {
            await connection.StartAsync();
            var result = await connection.InvokeAsync<bool>("Register", new RegisterDto()
            {
                Domain = options.Domain,
            });
            if (!result)
            {
                Console.WriteLine("Someone already registered for this route");
                await connection.DisposeAsync();
                return false;
            }
            connection.Reconnected += Connection_Reconnected;
            return true;
        }

        public ValueTask DisposeAsync()
        {
            if (connection == null)
            {
                return ValueTask.CompletedTask;
            }
            return connection.DisposeAsync();
        }

        public void On<T>(string method, Func<T, Task> onFunction)
        {
            connection.On(method, onFunction);
        }

        public Task InvokeAsync<T>(string method, T payload)
        {
            return connection.InvokeAsync(method, payload);
        }

        private async Task Connection_Reconnected(string arg)
        {
            var result = await connection.InvokeAsync<bool>("Register", new RegisterDto()
            {
                Domain = options.Domain,
            });
            if (!result)
            {
                Console.WriteLine("Someone already registered for this route");
                await connection.DisposeAsync();
                Environment.Exit(0);
                return;
            }
            Console.WriteLine($"Reconnected and listening on localhost:{options.Port} with domain {options.Domain}");
        }

    }
}
