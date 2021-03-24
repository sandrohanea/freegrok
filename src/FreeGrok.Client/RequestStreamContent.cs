using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FreeGrok.Client
{
    public class RequestStreamContent : HttpContent
    {
        private readonly long? streamLength;
        private Queue<(byte[] data, int size)> pendingData = new();
        private TaskCompletionSource isStreamingTcs = new();
        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        private bool isFinished;

        public RequestStreamContent(long? streamLength)
        {
            this.streamLength = streamLength;
        }

        public async Task SendDataAsync(byte[] data, int dataSize, bool isFinished)
        {
            await semaphoreSlim.WaitAsync();
            pendingData.Enqueue((data, dataSize));
            if (isStreamingTcs.Task.Status != TaskStatus.RanToCompletion)
            {
                isStreamingTcs.SetResult();
            }
            this.isFinished = isFinished;
            semaphoreSlim.Release();
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            while (!isFinished || pendingData.Count > 0)
            {
                await isStreamingTcs.Task;
                await semaphoreSlim.WaitAsync();
                while (pendingData.Count > 0)
                {
                    var (data, dataSize) = pendingData.Dequeue();
                    await stream.WriteAsync(data, 0, dataSize);

                }
                isStreamingTcs = new();
                semaphoreSlim.Release();
            }

        }

        protected override bool TryComputeLength(out long length)
        {
            if (streamLength.HasValue)
            {
                length = streamLength.Value;
            }
            else
            {
                length = 0;
            }
            return streamLength.HasValue;
        }
    }
}
