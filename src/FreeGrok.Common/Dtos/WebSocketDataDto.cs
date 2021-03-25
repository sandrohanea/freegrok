using System;

namespace FreeGrok.Common.Dtos
{
    public class WebSocketDataDto
    {
        public Guid WebSocketId { get; set; }
        public byte[] Data { get; set; }

        public bool EndOfMessage { get; set; }

        public WebSocketDataType Type { get; set; }
    }
}
