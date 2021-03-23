using CommandLine;
using FreeGrok.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace FreeGrok.Client
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient(new HttpClientHandler() { UseCookies = false });
        static async Task Main(string[] args)
        {
            var config = await ClientConfigProvider.GetClientConfigAsync();
            await Parser.Default.ParseArguments<Options>(args)
                      .WithParsedAsync(async o =>
                      {
                          var builder = new HubConnectionBuilder()
                            .WithUrl($"{o.Remote ?? config.DefaultUrl}routing")
                            .WithAutomaticReconnect();
                          var connection = builder.Build();
                          await connection.StartAsync();
                          Console.WriteLine($"Listening on port {o.Port} with domain {o.Domain}");
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
                          connection.On("Handle", new[] { typeof(RequestDto) }, (args) => HandleRequest(args[0] as RequestDto, connection, o));

                          await Task.Run(() => Console.ReadKey());

                          await connection.DisposeAsync();
                      });
        }

        private static async Task HandleRequest(RequestDto request, HubConnection connection, Options options)
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
                RequestUri = new Uri($"http{(options.UseHttps ? "s" : "")}://localhost:{options.Port}/{request.Path}")
            };

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

                responseDto = new ResponseDto()
                {
                    Content = await response.Content.ReadAsByteArrayAsync(),
                    Headers = responseHeaders,
                    StatusCode = (int)response.StatusCode,
                    RequestId = request.RequestId
                };

            }
            catch (HttpRequestException httpRequestException)
            {
                Console.WriteLine($"Coudn't connect to the specied port: {options.Port}");
                responseDto = new ResponseDto()
                {
                    StatusCode = (int?)httpRequestException.StatusCode ?? 404,
                    RequestId = request.RequestId,
                    Headers = new List<HeaderDto>(),
                    Content = Array.Empty<byte>()
                };
            }
            Console.WriteLine($"{request.Method}\t{request.Path}\t{responseDto.StatusCode}\r{responseDto.Content?.Length ?? 0}");
            await connection.InvokeAsync("Finish", responseDto);
        }
    }
}
