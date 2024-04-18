using CommandLine;
using FreeGrok.Client.Config;
using FreeGrok.Client.Handlers;
using FreeGrok.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace FreeGrok.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ClientConfigProvider clientConfigProvider = new();
            await clientConfigProvider.InitializeAsync();
            var clientConfig = clientConfigProvider.ClientConfig;
            await Parser.Default.ParseArguments<Options>(args)
                      .WithParsedAsync(async o =>
                      {
                          if (o.Remote != clientConfig.RemoteUrl && !string.IsNullOrEmpty(o.Remote))
                          {
                              await clientConfigProvider.SetRemote(o.Remote);
                          }
                          await using var connection = new ServerConnection(clientConfig, o);
                          await connection.InitializeAsync();
                          using var httpHandler = new HttpHandler(connection, o);
                          using var webSocketHandler = new WebSocketHandler(connection, o);
                          Console.WriteLine($"Listening on {o.Type}://localhost:{o.Port} with domain {o.Domain}\nPress enter to exit.");

                          ConsoleKeyInfo key;
                          do
                          {
                              key = await Task.Run(() => Console.ReadKey());
                          } while (key.Key != ConsoleKey.Enter);
                      });
        }

    }
}
