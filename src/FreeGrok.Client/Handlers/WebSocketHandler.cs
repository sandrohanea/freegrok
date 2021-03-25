using FreeGrok.Common;
using FreeGrok.Common.Dtos;
using FreeGrok.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeGrok.Client.Handlers
{
    public class WebSocketHandler : IDisposable
    {
        private ServerConnection connection;
        private Options options;
        private readonly Dictionary<Guid, ClientWebSocket> clientWebSockets = new();
        private readonly Dictionary<Guid, TaskCompletionSource> taskCompletionSources = new();

        public WebSocketHandler(ServerConnection connection, Options options)
        {
            this.connection = connection;
            this.options = options;
            connection.On<InitializeConnectionDto>("OnInitializeWebSocketConnection", InitializeClientWebSocket);
            connection.On<WebSocketDataDto>("OnReceiveWSData", OnReceiveDataAsync);
        }

        private async Task OnReceiveDataAsync(WebSocketDataDto data)
        {
            var haveWebSocket = clientWebSockets.TryGetValue(data.WebSocketId, out var webSocket);
            if (!haveWebSocket)
            {
                return;
            }

            if (data.Type == WebSocketDataType.Close)
            {
                await webSocket.CloseOutputAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);
                return;
            }
            await webSocket.SendAsync(data.Data, data.Type.AsMessageType(), data.EndOfMessage, CancellationToken.None);
        }

        private async Task InitializeClientWebSocket(InitializeConnectionDto request)
        {
            var ws = new ClientWebSocket();
            var tcs = new TaskCompletionSource();
            var protocol = options.Type.ToUpper() switch
            {
                "HTTP" => "ws",
                _ => "wss",
            };
            var url = new Uri($"{protocol}://localhost:{options.Port}{request.Path}");
            try
            {
                await ws.ConnectAsync(url, CancellationToken.None);
                taskCompletionSources[request.RequestId] = tcs;
                clientWebSockets[request.RequestId] = ws;
                StartReadFromSocket(ws, tcs, request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when trying to connect to websocket: {ex.Message}");
                await connection.InvokeAsync("CloseWS", new CloseWebSocketDto() { WebSocketId = request.RequestId });
            }
        }

        public void Dispose()
        {
            foreach (var ws in clientWebSockets)
            {
                ws.Value.Dispose();
            }
        }

        private void StartReadFromSocket(ClientWebSocket ws, TaskCompletionSource tcs, InitializeConnectionDto request)
        {
            var cts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        if (ws.State != WebSocketState.Open)
                        {
                            break;
                        }
                        var data = new byte[Constants.StreamBufferSize];
                        var resultTask = ws.ReceiveAsync(data, cts.Token);
                        var completedTask = await Task.WhenAny(tcs.Task, resultTask);
                        if (completedTask == tcs.Task)
                        {
                            cts.Cancel();
                            break;
                        }
                        var wsData = new WebSocketDataDto()
                        {
                            WebSocketId = request.RequestId,
                            Data = data[..resultTask.Result.Count],
                            EndOfMessage = resultTask.Result.EndOfMessage,
                            Type = resultTask.Result.MessageType.AsDataType()
                        };

                        await connection.InvokeAsync("OnSendWSData", wsData);
                    }
                    if (ws.State == WebSocketState.Open)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
                    }
                }
                finally
                {
                    clientWebSockets.Remove(request.RequestId);
                    taskCompletionSources.Remove(request.RequestId);
                    ws.Dispose();
                    await connection.InvokeAsync("CloseWS", new CloseWebSocketDto() { WebSocketId = request.RequestId });
                }
            }, cts.Token);
        }
    }
}
