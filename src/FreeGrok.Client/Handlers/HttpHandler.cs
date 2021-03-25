using FreeGrok.Common;
using FreeGrok.Common.Dtos;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FreeGrok.Client.Handlers
{
    public class HttpHandler : IDisposable
    {
        private readonly ServerConnection connection;
        private readonly Options options;
        private readonly HttpClient httpClient;
        private readonly Dictionary<Guid, RequestStreamContent> requestStreams;

        public HttpHandler(ServerConnection connection, Options options)
        {
            this.connection = connection;
            this.options = options;
            this.httpClient = new HttpClient(new HttpClientHandler() { UseCookies = false }) { Timeout = TimeSpan.FromDays(2) };
            this.requestStreams = new();
            connection.On<InitializeConnectionDto>("OnInitializeConnection", InitializeConnection);
            connection.On<RequestContentDto>("OnRequestData", OnRequestDataAsync);
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        private async Task OnRequestDataAsync(RequestContentDto request)
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

        private async Task InitializeConnection(InitializeConnectionDto request)
        {
            try
            {
                var protocol = options.Type.ToUpper() switch
                {
                    "HTTP" => "http",
                    _ => "https",
                };
                var url = new Uri($"{protocol}://localhost:{options.Port}{request.Path}");
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
                    RequestUri = url
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
                        if (requestMessage.Content == null)
                        {
                            requestMessage.Content = new StringContent(string.Empty);
                            requestMessage.Content.Headers.Clear();
                        }
                        requestMessage.Content.Headers.Add(header.Key, header.Value);
                        continue;
                    }
                    requestMessage.Headers.Add(header.Key, header.Value);
                }

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


                var responseDto = new ResponseDto()
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
                var responseDto = new ResponseDto()
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
