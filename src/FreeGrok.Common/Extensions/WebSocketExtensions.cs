using FreeGrok.Common.Dtos;
using System.Net.WebSockets;

namespace FreeGrok.Common.Extensions
{
    public static class WebSocketExtensions
    {
        public static WebSocketMessageType AsMessageType(this WebSocketDataType webSocketDataType)
        {
            return webSocketDataType switch
            {
                WebSocketDataType.Text => WebSocketMessageType.Text,
                WebSocketDataType.Binary => WebSocketMessageType.Binary,
                _ => WebSocketMessageType.Close
            };
        }

        public static WebSocketDataType AsDataType(this WebSocketMessageType webSocketMessageType)
        {
            return webSocketMessageType switch
            {
                WebSocketMessageType.Text => WebSocketDataType.Text,
                WebSocketMessageType.Binary => WebSocketDataType.Binary,
                _ => WebSocketDataType.Close
            };
        }
    }
}
