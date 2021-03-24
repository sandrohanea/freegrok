using CommandLine;
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
        private static readonly HttpClient httpClient = new HttpClient(new HttpClientHandler() { UseCookies = false }) { Timeout = TimeSpan.FromDays(2) };
        private static Dictionary<Guid, RequestStreamContent> requestStreams = new();
        private static Options options;
        private static HubConnection connection;


        static async Task Main(string[] args)
        {
            var config = await ClientConfigProvider.GetClientConfigAsync();
            await Parser.Default.ParseArguments<Options>(args)
                      .WithParsedAsync(async o =>
                      {
                          options = o;
                          var builder = new HubConnectionBuilder()
                            .WithUrl($"{o.Remote ?? config.DefaultUrl}routing")
                            .WithAutomaticReconnect();
                          connection = builder.Build();
                          await connection.StartAsync();
                          Console.WriteLine($"Listening on localhost:{o.Port} with domain {o.Domain}\nPress enter to exit.");
                          var result = await connection.InvokeAsync<bool>("Register", new RegisterDto()
                          {
                              Domain = o.Domain,
                          });
                          if (!result)
                          {
                              Console.WriteLine("Someone already registered for this route");
                              await connection.DisposeAsync();
                              return;
                          }
                          connection.On<InitializeConnectionDto>("OnInitializeConnection", (request) => InitializeConnection(request).ConfigureAwait(false));
                          connection.On<RequestContentDto>("OnRequestData", (request) => OnRequestDataAsync(request).ConfigureAwait(false));
                          connection.Reconnected += Connection_Reconnected;
                          ConsoleKeyInfo key;
                          do
                          {
                              key = await Task.Run(() => Console.ReadKey());
                          } while (key.Key != ConsoleKey.Enter);

                          await connection.DisposeAsync();
                      });
        }

        private static async Task Connection_Reconnected(string arg)
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

        private static async Task OnRequestDataAsync(RequestContentDto request)
        {
            var haveRequestStream = requestStreams.TryGetValue(request.RequestId, out var requestStream);
            if (!haveRequestStream)
            {
                return;
            }
            await requestStream.SendDataAsync(request.Data, request.DataSize, request.IsFinished);
            if (request.IsFinished)
            {
                requestStreams.Remove(request.RequestId);
            }
        }

        private static async Task InitializeConnection(InitializeConnectionDto request)
        {
            var requestMessage = new HttpRequestMessage
            {
                Method = request.Method.ToUpper() switch
                {
                    "POST" => HttpMethod.Post,
                    "DELETE" => HttpMethod.Delete,
                    "PATCH" => HttpMethod.Patch,
                    "OPTIONS" => HttpMethod.Options,
                    "HEAD" => HttpMethod.Head,
                    "PUT" => HttpMethod.Put,
                    "TRACE" => HttpMethod.Trace,
                    _ => HttpMethod.Get
                },
                RequestUri = new Uri($"http{(options.UseHttps ? "s" : "")}://localhost:{options.Port}{request.Path}")
            };

            if (request.HaveContent)
            {
                var streamContent = new RequestStreamContent(request.BodyLength);
                requestStreams.Add(request.RequestId, streamContent);
                requestMessage.Content = streamContent;
            }
            foreach (var header in request.Headers)
            {
                if (header.Key.StartsWith(":"))
                {
                    continue;
                }
                if (header.Key.ToUpper() == "HOST" && !string.IsNullOrEmpty(options.Host))
                {
                    requestMessage.Headers.Add(header.Key, options.Host);
                    continue;
                }
                if (header.Key.ToUpper().StartsWith("CONTENT"))
                {
                    if(requestMessage.Content == null)
                    {
                        requestMessage.Content = new StringContent(string.Empty);
                        requestMessage.Content.Headers.Clear();
                    }
                    requestMessage.Content.Headers.Add(header.Key, header.Value);
                    continue;
                }
                requestMessage.Headers.Add(header.Key, header.Value);
            }

            ResponseDto responseDto = null;
            try
            {
                var response = await httpClient.SendAsync(requestMessage);
                var responseHeaders = new List<HeaderDto>();
                foreach (var header in response.Headers)
                {
                    foreach (var value in header.Value)
                    {
                        responseHeaders.Add(new HeaderDto() { Key = header.Key, Value = value });
                    }
                }
                foreach (var header in response.Content.Headers)
                {
                    foreach (var value in header.Value)
                    {
                        responseHeaders.Add(new HeaderDto() { Key = header.Key, Value = value });
                    }
                }

                

                using var responseContentStream = await response.Content.ReadAsStreamAsync();

                var data = new byte[Constants.StreamBufferSize];
                var dataSize = await responseContentStream.ReadAsync(data);
                var haveContent = dataSize != 0;


                responseDto = new ResponseDto()
                {
                    HaveContent = haveContent,
                    Headers = responseHeaders,
                    StatusCode = (int)response.StatusCode,
                    RequestId = request.RequestId
                };
                await connection.InvokeAsync("Response", responseDto);
                Console.WriteLine($" {request.Method} \t{request.Path}\t{responseDto.StatusCode}");

                if (haveContent)
                {
                    await connection.InvokeAsync(
                                                "OnResponseData",
                                                new RequestContentDto()
                                                {
                                                    Data = data,
                                                    IsFinished = dataSize < Constants.StreamBufferSize,
                                                    DataSize = dataSize,
                                                    RequestId = request.RequestId
                                                });
                }
                while (dataSize == Constants.StreamBufferSize)
                {
                    dataSize = await responseContentStream.ReadAsync(data);
                    await connection.InvokeAsync(
                                                "OnResponseData",
                                                new RequestContentDto()
                                                {
                                                    Data = data,
                                                    IsFinished = dataSize < Constants.StreamBufferSize,
                                                    DataSize = dataSize,
                                                    RequestId = request.RequestId
                                                });

                }
            }
            catch (HttpRequestException httpRequestException)
            {
                Console.WriteLine($"Coudn't connect to the specied port: {options.Port}");
                Console.WriteLine($"Exception: {httpRequestException.Message}");
                responseDto = new ResponseDto()
                {
                    StatusCode = (int?)httpRequestException.StatusCode ?? 404,
                    RequestId = request.RequestId,
                    Headers = new List<HeaderDto>()
                };
                Console.WriteLine($" {request.Method} \t{request.Path}\t{responseDto.StatusCode}");
                await connection.InvokeAsync("Response", responseDto);
            }
        }
    }
}
