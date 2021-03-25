using System;

namespace FreeGrok.Common.Dtos
{
    public class ResponseContentDto
    {
        public Guid RequestId { get; set; }

        public byte[] Data { get; set; }
        public bool IsFinished { get; set; }
        public int DataSize { get; set; }
    }
}
